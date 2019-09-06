﻿using System.IO;
using Newtonsoft.Json;
using Zen.Base;
using Zen.Base.Extension;

namespace Zen.Storage.Provider.Configuration.FileSystem
{
    public class FileSystemConfiguratonStorageProvider<T> : IZenConfigurationStorageProvider where T : class
    {
        private FileSystemConfigurationStorageAttribute _config;
        private FileInfo _file;
        private DirectoryInfo _path;

        private object _sourceModel;

        #region Implementation of IConfigurationStorageProvider

        public void Initialize(ZenConfigurationStorageAttribute config)
        {
            _config = config as FileSystemConfigurationStorageAttribute;

            if (_config == null) return;

            _path = new DirectoryInfo(_config.Location ?? Host.DataDirectory);
            _file = new FileInfo(Path.Combine(_path.FullName, _config.FileName));
        }

        public bool IsValid(object sourceModel)
        {
            _sourceModel = sourceModel;
            if (!_path.Exists) return false;
            if (_file.Exists) return true;

            if (_config.ReadOnly) return _config.DefaultIfMissing;

            System.IO.File.WriteAllText(_file.FullName, sourceModel.ToJson(format: Formatting.Indented));
            return true;
        }

        public object Load() { return _file.Exists ? System.IO.File.ReadAllText(_file.FullName).FromJson<object>() : _sourceModel; }
        public void Save(object sourceModel) { }
        public string Descriptor => _config.Descriptor;

        #endregion
    }
}