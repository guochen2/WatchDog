using FreeRedis;
using Furion;
using Furion.EventBus;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using WatchDog.src;
using WatchDog.src.Data;
using WatchDog.src.Events;
using WatchDog.src.Exceptions;
using WatchDog.src.Helpers;
using WatchDog.src.Hubs;
using WatchDog.src.Interfaces;
using WatchDog.src.Models;
using WatchDog.src.Options;
using WatchDog.src.Services;

namespace WatchDog
{
    public static class WatchDogExtension
    {
        public static readonly IFileProvider Provider = new EmbeddedFileProvider(
        typeof(WatchDogExtension).GetTypeInfo().Assembly,
        "WatchDog"
        );

        public static IServiceCollection AddWatchDogServices(this IServiceCollection services, [Optional] Action<WatchDogSettings> configureOptions)
        {
            services.AddConfigurableOptions<WatchDogOptions>();
            services.AddOutputCache();
            var options = new WatchDogSettings();
            if (configureOptions != null)
                configureOptions(options);

            AutoClearModel.IsAutoClear = options.IsAutoClear;
            AutoClearModel.ClearTimeSchedule = options.ClearTimeSchedule;
            WatchDogExternalDbConfig.ConnectionString = options.SetExternalDbConnString;
            WatchDogDatabaseDriverOption.DatabaseDriverOption = options.DbDriverOption;
            WatchDogExternalDbConfig.MongoDbName = Assembly.GetCallingAssembly().GetName().Name?.Replace('.', '_') + "_WatchDogDB";

            if (!string.IsNullOrEmpty(WatchDogExternalDbConfig.ConnectionString) && WatchDogDatabaseDriverOption.DatabaseDriverOption == 0)
                throw new WatchDogDBDriverException("Missing DB Driver Option: DbDriverOption is required at .AddWatchDogServices()");
            if (WatchDogDatabaseDriverOption.DatabaseDriverOption != 0 && string.IsNullOrEmpty(WatchDogExternalDbConfig.ConnectionString))
                throw new WatchDogDatabaseException("Missing connection string.");

            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(5);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddSignalR();
            services.AddMvcCore(x =>
            {
                x.EnableEndpointRouting = false;
            }).AddApplicationPart(typeof(WatchDogExtension).Assembly);

            services.AddSingleton<IBroadcastHelper, BroadcastHelper>();
            services.AddSingleton<IMemoryCache, MemoryCache>();

            if (!string.IsNullOrEmpty(WatchDogExternalDbConfig.ConnectionString))
            {
                if (WatchDogDatabaseDriverOption.DatabaseDriverOption == src.Enums.WatchDogDbDriverEnum.Mongo)
                {
                    ExternalDbContext.MigrateNoSql();
                }
                else
                {
                    ExternalDbContext.Migrate();
                }
            }

            if (AutoClearModel.IsAutoClear)
                services.AddHostedService<AutoLogClearerBackgroundService>();
            services.AddSingleton<WatchdogSubscribe>();
            services.AddSingleton<IEventSubscriber, WatchDotMainEvents>();
            return services;
        }

        public static IApplicationBuilder UseWatchDogExceptionLogger(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<src.WatchDogExceptionLogger>();
        }

        public static IEndpointRouteBuilder MapWatchRoute(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<LoggerHub>("/wtchdlogger");
            endpoints.MapControllerRoute(
                name: "WTCHDwatchpage",
                pattern: "WTCHDwatchpage/{action}",
                defaults: new { controller = "WatchPage", action = "Index" });
            endpoints.MapGet("watchdog", async context =>
            {
                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync(WatchDogExtension.GetFile());
            });
            return endpoints;
        }

        public static IApplicationBuilder UseWatchDogSimple(this IApplicationBuilder app, Action<WatchDogOptionsModel> configureOptions)
        {
            ServiceProviderFactory.BroadcastHelper = app.ApplicationServices.GetService<IBroadcastHelper>();
            var options = new WatchDogOptionsModel();
            configureOptions(options);
            options.Blacklist = App.GetOptions<WatchDogOptions>().PathBlacklist;
            if (string.IsNullOrEmpty(options.WatchPageUsername))
            {
                throw new WatchDogAuthenticationException("Parameter Username is required on .UseWatchDog()");
            }
            else if (string.IsNullOrEmpty(options.WatchPagePassword))
            {
                throw new WatchDogAuthenticationException("Parameter Password is required on .UseWatchDog()");
            }

            app.UseMiddleware<src.WatchDog>(options);


            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new EmbeddedFileProvider(
                    typeof(WatchDogExtension).GetTypeInfo().Assembly,
                  "WatchDog.src.WatchPage"),

                RequestPath = new PathString("/WTCHDGstatics")
            });

            app.UseSession();


            if (options.UseOutputCache)
                app.UseOutputCache();

            return app;
        }


        public static IFileInfo GetFile()
        {
            return Provider.GetFileInfo("src.WatchPage.index.html");
        }

        public static string GetFolder()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }
        public static void Init()
        {
            var options = App.GetOptions<WatchDogOptions>();

            WatchDog.src.Models.WatchDogConfigModel.Blacklist = String.IsNullOrEmpty(options.PathBlacklist) ? new string[] { } : options.PathBlacklist.Replace(" ", string.Empty).Split(',');
            WatchDog.src.Models.WatchDogConfigModel.ResHeaderBlacklist = String.IsNullOrEmpty(options.ResHeaderBlacklist) ? new string[] { } : options.ResHeaderBlacklist.Replace(" ", string.Empty).Split(',');
            WatchDog.src.Models.WatchDogConfigModel.ReqHeaderBlacklist = String.IsNullOrEmpty(options.ReqHeaderBlacklist) ? new string[] { } : options.ReqHeaderBlacklist.Replace(" ", string.Empty).Split(',');
            WatchDog.src.Models.WatchDogConfigModel.ExternalWhitelists = String.IsNullOrEmpty(options.ExternalWhitelists) ? new string[] { } : options.ExternalWhitelists.Replace(" ", string.Empty).Split(',');
            Console.WriteLine("初始化WatchDog默认配置");
            App.GetRequiredService<WatchdogSubscribe>().InitSubscribe();
        }
    }
}
