﻿using System.Collections.Generic;
using System.Security.Principal;
using Zen.Base.Module.Identity;

namespace Zen.Base.Module.Default
{
    public class NullAuthorizationProvider : IAuthorizationProvider
    {
        public IPrincipal Principal => null;
        public IIdentity Identity => null;
        public string Id => null;
        public string Locator => null;
        public bool CheckPermission(string pCode) => true;
        public bool CheckPermission(IEnumerable<string> pCode) => true;
        public void Shutdown() { }
        public void Initialize() { Events.ShutdownSequence.Actions.Add(Shutdown); }
    }
}