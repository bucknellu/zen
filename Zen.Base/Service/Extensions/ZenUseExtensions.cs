﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Zen.Base.Module.Service;

namespace Zen.Base.Service.Extensions
{
    public static class Use
    {
        public static IApplicationBuilder UseZen(this IApplicationBuilder app, Action<IZenBuilder> configuration = null, IHostingEnvironment env = null)
        {
            configuration = configuration ?? (x => { });

            Instances.ApplicationBuilder = app;

            var optionsProvider = app.ApplicationServices.GetService<IOptions<ZenOptions>>();

            var options = new ZenOptions(optionsProvider.Value);

            AutoService.UseAll(app, env);

            var builder = new ZenBuilder(app, options);

            configuration.Invoke(builder);

            Current.Log.Add(Current.State.ToString());

            return app;
        }
    }
}