﻿using System;
using Zen.App.Model.Audience;
using Zen.App.Model.Tag;
using Zen.Base.Common;
using Zen.Storage.Provider.File;

namespace Zen.Storage.Model
{
    [Priority(Level = -99)]
    public class ZenFile : ZenFileBaseDescriptor<ZenFile>, IZenFileDescriptor {
        #region Implementation of IDataId

        public string Id { get; set; }

        #endregion

        #region Implementation of IDataLocator

        public string Locator { get; set; }

        #endregion

        #region Implementation of IZenFileDescriptor

        public string StorageName { get; set; }
        public string OriginalName { get; set; }
        public string StoragePath { get; set; }
        public string MimeType { get; set; }
        public long FileSize { get; set; }
        public DateTime Creation { get; set; }
        public TagCollection Tags { get; set; }
        public AudienceDefinition Audience { get; set; }
        public IZenFileDescriptor GetNewInstance() => new ZenFile();

        #endregion
    }
}