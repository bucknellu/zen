﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zen.Base.Module.Service;
using Zen.Web.Service.Extensions;

namespace Zen.Web.Service
{
    public class ZenWebAutoService : IZenAutoAddService, IZenAutoUseService
    {
        public void Add(IServiceCollection services) { services.AddZenWeb(); }

        public void Use(IApplicationBuilder app, IHostingEnvironment env = null) { app.UseZenWeb(null, env); }
    }
}