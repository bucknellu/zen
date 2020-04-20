﻿using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;
using Zen.Base;
using Zen.Base.Common;
using Zen.Base.Extension;
using Zen.Base.Module.Cache;
using Zen.Base.Module.Encryption;
using Zen.Base.Module.Environment;
using Zen.Base.Module.Log;

namespace Zen.Module.Cache.Redis
{
    [Priority(Level = -1)]
    public class RedisCacheProvider : CacheProviderPrimitive
    {
        public RedisCacheProvider(IEnvironmentProvider environmentProvider, IEncryptionProvider encryptionProvider)
        {
            _environmentProvider = environmentProvider;
            _encryptionProvider = encryptionProvider;
        }

        public override IEnumerable<string> GetKeys(string oNamespace = null)
        {
            if (OperationalStatus != EOperationalStatus.Operational) return null;

            try
            {
                var db = _redis.GetDatabase(DatabaseIndex);
                var conn = _redis.GetEndPoints()[0];
                var svr = _redis.GetServer(conn);
                var keys = svr.Keys(pattern: "*").ToList();

                var ret = keys.Select(a => db.StringGet(a).ToString()).ToList();
                return ret;
            }
            catch (Exception)
            {
                OperationalStatus = EOperationalStatus.Error;
                throw;
            }

        }

        public override bool Contains(string key)
        {
            if (OperationalStatus != EOperationalStatus.Operational) return false;

            try
            {
                var db = _redis.GetDatabase(DatabaseIndex);
                var res = db.KeyExists(key);
                return res;
            }
            catch (Exception)
            {
                OperationalStatus = EOperationalStatus.Error;
                throw;
            }
        }

        public override void SetNative(string key, string serializedModel, CacheOptions options = null)
        {
            if (OperationalStatus != EOperationalStatus.Operational) return;

            try
            {
                var db = _redis.GetDatabase(DatabaseIndex);

                db.StringSet(key, serializedModel, options?.LifeTimeSpan );
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
            }
        }

        public override string GetNative(string key)
        {
            if (OperationalStatus != EOperationalStatus.Operational) return null;

            try
            {
                var db = _redis.GetDatabase(DatabaseIndex);
                var res = db.StringGet(key);
                return res;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                return null;
            }
        }

        public override void Remove(string fullKey)
        {
            if (fullKey == null) throw new ArgumentNullException(nameof(fullKey));
            if (OperationalStatus != EOperationalStatus.Operational) return;

            try
            {
                _redis.GetDatabase(DatabaseIndex).KeyDelete(fullKey);
            }
            catch
            {
                // ignored
            }
        }

        public override void RemoveAll()
        {
            if (OperationalStatus != EOperationalStatus.Operational) return;

            try
            {
                foreach (var endPoint in _redis.GetEndPoints())
                {
                    var server = _redis.GetServer(endPoint);

                    var db = _redis.GetDatabase(DatabaseIndex);

                    var keys = server.Keys(pattern: "*", database: DatabaseIndex).ToList();

                    Current.Log.Add("REDIS: Removing {0} keys from database {1}".format(keys.Count, DatabaseIndex), Message.EContentType.Maintenance);

                    foreach (var key in keys) db.KeyDelete(key);
                }
            }
            catch (Exception e)
            {
                Current.Log.Add($"REDIS: {e.Message}", Message.EContentType.Exception);
            }
        }

        public override string Name { get; } = "Redis";

        #region driver-specific implementation

        public override void Initialize()
        {
            //In the case nothing is defined, a standard environment setup is provided.
            if (EnvironmentConfiguration == null)
                EnvironmentConfiguration = new Dictionary<string, ICacheConfiguration>
                {
                    {"STA", new RedisCacheConfiguration {DatabaseIndex = 5, ConnectionString = "localhost"}}
                };

            var probe = (RedisCacheConfiguration) EnvironmentConfiguration[_environmentProvider.CurrentCode];
            DatabaseIndex = probe.DatabaseIndex;
            _currentServer = probe.ConnectionString;

            Connect();
        }

        private static ConnectionMultiplexer _redis;

        private static string _currentServer = "none";

        private readonly IEnvironmentProvider _environmentProvider;
        private readonly IEncryptionProvider _encryptionProvider;

        private static int DatabaseIndex { get; set; } = -1;
        internal string ServerName { get; private set; }

        public void Connect()
        {
            try
            {
                ServerName = _currentServer.Split(',')[0];

                //The connection string may be encrypted. Try to decrypt it, but ignore if it fails.
                try
                {
                    _currentServer = _encryptionProvider.Decrypt(_currentServer);
                }
                catch { }

                Events.AddLog("Redis server", _currentServer.SafeArray("password"));

                _redis = ConnectionMultiplexer.Connect(_currentServer);
                OperationalStatus = EOperationalStatus.Operational;
            }
            catch (Exception e)
            {
                OperationalStatus = EOperationalStatus.NonOperational;
                Events.AddLog("REDIS server", "Unavailable - running on direct database mode");
                Current.Log.KeyValuePair("REDIS server", "Unavailable - running on direct database mode", Message.EContentType.Warning);
                Current.Log.Add(e);
            }
        }

        #endregion
    }
}