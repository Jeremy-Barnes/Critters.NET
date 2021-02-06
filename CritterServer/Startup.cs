using System.Data;
using System.Data.Common;
using CritterServer.Domains;
using CritterServer.DataAccess;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using CritterServer.Domains.Components;
using Serilog.Events;
using CritterServer.Pipeline;
using CritterServer.Utilities.Serialization;
using CritterServer.Pipeline.Middleware;
using Microsoft.Extensions.Hosting;
using CritterServer.Game;
using CritterServer.Hubs;
using System.Reflection;
using CritterServer.DataAccess.Caching;
using Microsoft.AspNetCore.HttpOverrides;
using System.Collections.Generic;

namespace CritterServer
{
    public class Startup
    {
        readonly string PermittedOrigins = "XMenOrigins";

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
#region Framework
            //request pipeline
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddDataContractResolver().AddMvcOptions((options) =>
                {
                    options.EnableEndpointRouting = true;
                });

            List<string> permittedOriginUrls = new List<string>
            { "jabarnes.io", "jabarnes.io/", "http://jabarnes.io", "https://jabarnes.io", "http://jabarnes.io/", "https://jabarnes.io/",
            "https://app.jabarnes.io/", "http://app.jabarnes.io/"};

            if (Environment.IsDevelopment())
            {
                permittedOriginUrls.Add("localhost:8080");
                permittedOriginUrls.Add("localhost:8080/");
                permittedOriginUrls.Add("http://localhost:8080");
                permittedOriginUrls.Add("http://localhost:8080/");
            }
            services.AddCors(options =>
            {
                options.AddPolicy(name: PermittedOrigins,
                                  builder =>
                                  {
                                      builder.WithOrigins(permittedOriginUrls.ToArray())
                                      .AllowAnyMethod()
                                      .AllowAnyHeader()
                                      .AllowCredentials();
                                  });
            });

            //db
            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
            services.AddScoped<IDbConnection>(sp =>
            {
                var conn = DbProviderFactories.GetFactory("Npgsql").CreateConnection();
                conn.ConnectionString = Configuration.GetConnectionString("Sql");
                return conn;
            });
            services.AddScoped<ITransactionScopeFactory, TransactionScopeFactory>();

            configureLogging(Environment);

            //auth
            services.AddJwt(Configuration);

            //real time talk, for whatever
            services.AddSignalR()
                .AddNewtonsoftJsonProtocol(options =>
            {
                options.PayloadSerializerSettings.ContractResolver = new SensitiveDataContractResolver();
            });


            #endregion Framework

            //set up DI stuff

            //domains
            services.AddTransient<UserDomain>();
            services.AddTransient<MessageDomain>();
            services.AddTransient<AdminDomain>();
            services.AddTransient<PetDomain>();
            services.AddTransient<GameDomain>();
            services.AddTransient<SearchDomain>();
            services.AddTransient<ErrorMiddleware>();

            services.AddSingleton<MultiplayerGameService>();
            services.AddSingleton<IHostedService>(sp => sp.GetService<MultiplayerGameService>());

            //repositories
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IMessageRepository, MessageRepository>();
            services.AddTransient<IPetRepository, PetRepository>();
            services.AddTransient<IConfigRepository, ConfigRepository>();
            services.AddTransient<IFriendshipRepository, FriendshipRepository>();
            services.AddTransient<IGameRepository, GameRepository>();

            services.AddTransient<IGameCache, GameCache>();
            services.AddMemoryCache();

            //components
            services.AddHttpContextAccessor();
            services.AddScoped<CookieEventHandler>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            app.UseCors(PermittedOrigins);
            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();

            app.UseMiddleware<ErrorMiddleware>();
            
           app.UseEndpoints(endpoints => {
               endpoints.MapControllers();
               endpoints.MapHub<NotificationHub>("/notificationhub");
               endpoints.MapHub<GameHub>(typeof(GameHub).GetCustomAttribute<HubPathAttribute>().HubPath);
               endpoints.MapHub<BattleHub>(typeof(BattleHub).GetCustomAttribute<HubPathAttribute>().HubPath);
           });//last thing
        }

        private void configureLogging(IWebHostEnvironment env)
        {
            var stringLevel = Configuration.GetSection("Logging__LogLevel__Default").Value;

            LogEventLevel logLevel;
            switch (stringLevel)
            {
                case "Verbose": logLevel = LogEventLevel.Verbose; break;
                case "Debug": logLevel = LogEventLevel.Debug; break;
                case "Information": logLevel = LogEventLevel.Information; break;
                case "Warning": logLevel = LogEventLevel.Warning; break;
                case "Error": logLevel = LogEventLevel.Error; break;
                case "Fatal": logLevel = LogEventLevel.Fatal; break;
                default: logLevel = LogEventLevel.Warning; break;
            }

            var logCfg = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File(path: "bin/logs/Critter.log", rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: 1000 * 1000 * 100, //100mb
            rollOnFileSizeLimit: true)
            .WriteTo.Debug()
            .MinimumLevel.Is(logLevel);

            if (env.IsDevelopment())
            {
                logCfg.WriteTo.EventLog("Critters.NET", "Critters.NET");
            }

            Log.Logger = logCfg.CreateLogger();

            Log.Warning("Logger configured to {debugLevel}", stringLevel);
        }
    }
}
