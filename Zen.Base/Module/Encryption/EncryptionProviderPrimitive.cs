﻿namespace Zen.Base.Module.Encryption {
    public abstract class EncryptionProviderPrimitive : IEncryptionProvider
    {
        public abstract void Initialize();
        public abstract void Configure(params string[] oParms);
        public abstract string Decrypt(string pContent);
        public abstract string Encrypt(string pContent);

        public string TryDecrypt(string pContent)
        {
            // If it fails to decrypt, no biggie; It may be plain-text. ignore and continue.
            try { return Decrypt(pContent); } catch { }

            return pContent;
        }

        public string TryEncrypt(string pContent)
        {
            try { return Encrypt(pContent); } catch { }

            return pContent;
        }
    }
}