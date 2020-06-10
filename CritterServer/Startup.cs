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
using Microsoft.IdentityModel.Tokens;
using System;
using CritterServer.Utilities.Serialization;
using Microsoft.AspNetCore.Http;
using CritterServer.Pipeline.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using CritterServer.Game;
using CritterServer.Hubs;

namespace CritterServer
{
    public class Startup
    {
        readonly string PermittedOrigins = "XMenOrigins";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

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

            services.AddCors(options =>
            {
                options.AddPolicy(name: PermittedOrigins,
                                  builder =>
                                  {
                                      builder.WithOrigins("localhost:10202/", "http://localhost:10202", "http://localhost:10202/", "localhost:10202")
                                      .AllowAnyMethod()
                                      .AllowAnyHeader()
                                      .AllowCredentials();
                                  });
            });

            //db
            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
            services.AddTransient<IDbConnection>((sp) =>
            {
                var conn = DbProviderFactories.GetFactory("Npgsql").CreateConnection();
                conn.ConnectionString = Configuration.GetConnectionString("Sql");
                return conn;
            });

            configureLogging();

            //auth
            services.AddJwt(Configuration);

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
            services.AddTransient<ErrorMiddleware>();

            services.AddSingleton<GameManagerService>();
            services.AddSingleton<IHostedService>(sp => sp.GetService<GameManagerService>());

            //repositories
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IMessageRepository, MessageRepository>();
            services.AddTransient<IPetRepository, PetRepository>();
            services.AddTransient<IConfigRepository, ConfigRepository>();

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
            app.UseCors(PermittedOrigins);
            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();

            app.UseMiddleware<ErrorMiddleware>();
            
           app.UseEndpoints(endpoints => {
               endpoints.MapControllers();
               endpoints.MapHub<NotificationHub>("/notificationhub");
               endpoints.MapHub<GameHub>("/gamehub");

           });//last thing
        }

        private void configureLogging()
        {
            var stringLevel = Configuration.GetSection("Logging:LogLevel:Default").Value;

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

            Log.Logger = new LoggerConfiguration()
               .Enrich.FromLogContext()
               .WriteTo.EventLog("Critters.NET", "Critters.NET", "343GuiltySpark")
               .WriteTo.File(path: "bin/logs/Critter.log", rollingInterval: RollingInterval.Day,
               fileSizeLimitBytes: 1000 * 1000 * 100, //100mb
               rollOnFileSizeLimit: true)
               .WriteTo.Debug()
               .MinimumLevel.Is(logLevel)
                   .CreateLogger();

            Log.Warning("Logger configured to {debugLevel}", stringLevel);
        }
    }
}
