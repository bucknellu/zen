﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Zen.Base;
using Zen.Base.Module.Encryption;

namespace Zen.Module.Encryption.AES
{

    public class AesEncryptionProvider : IEncryptionProvider
    {
        private readonly object _lock = new object();

        private RijndaelManaged _aesAlg;

        private bool _isInitialized;
        private string _rjiv = "";
        private string _rjkey = "";

        #region Instanced methods

        public AesEncryptionProvider()
        {
            _rjkey = Strings.default_aes_key;
            // I know. Default key and vector for encryption, right? This is just a demo, though.

            _rjiv = Strings.default_aes_vector;
            //This class should be properly instanced via the constructor below:
        }

        public AesEncryptionProvider(string key, string iv)
        {
            if (key.Length != 32) throw new Exception(Strings.encryption_aes_key_length_err);
            if (iv.Length != 16) throw new Exception(Strings.encryption_aes_vector_length_err);

            _rjkey = key;
            _rjiv = iv;
        }

        // ReSharper disable once InconsistentNaming
        public string Decrypt(string pContent)
        {
            InitSettings();

            if (pContent == null) return null;

            string plaintext;

            var _base = Convert.FromBase64String(pContent);

            using (var msDecrypt = new MemoryStream(_base))
            {
                var deCryptT = _aesAlg.CreateDecryptor(_aesAlg.Key, _aesAlg.IV);

                using (var csDecrypt = new CryptoStream(msDecrypt, deCryptT, CryptoStreamMode.Read))
                {
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
            return plaintext;
        }

        // ReSharper disable once InconsistentNaming
        public string Encrypt(string pContent)
        {
            InitSettings();

            using (var msEncrypt = new MemoryStream())
            {
                string ret;

                var enCryptT = _aesAlg.CreateEncryptor(_aesAlg.Key, _aesAlg.IV);

                using (var csEncrypt = new CryptoStream(msEncrypt, enCryptT, CryptoStreamMode.Write))
                {
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(pContent);
                        swEncrypt.Flush();
                        csEncrypt.FlushFinalBlock();
                    }

                    ret = Convert.ToBase64String(msEncrypt.ToArray());
                }

                return ret;
            }
        }

        public void Shutdown() { }

        private void InitSettings()
        {
            if (_isInitialized) return;

            lock (_lock)
            {
                _aesAlg = new RijndaelManaged
                {
                    Key = Encoding.ASCII.GetBytes(_rjkey),
                    IV = Encoding.ASCII.GetBytes(_rjiv)
                };

                _isInitialized = true;
            }
        }

        #endregion

        public void Configure(params string[] oParms)
        {
            if (oParms.Length >= 1)
                _rjkey = oParms[0];

            if (oParms.Length >= 2)
                _rjiv = oParms[1];
        }

        public void Initialize()
        {
            Events.Bootup.Actions.Add(InitSettings);
            Events.Shutdown.Actions.Add(Shutdown);
        }
    }
}
