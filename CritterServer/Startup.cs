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
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddDataContractResolver()
                .AddMvcOptions((options) => //get your Filters here! Request Pipeline Filters, right here!
                {
                    options.Filters.Add<UserFilter>();
                });

            services.AddCors(options =>
            {
                options.AddPolicy(name: PermittedOrigins,
                                  builder =>
                                  {
                                      builder.WithOrigins("localhost:10202/", "http://localhost:10202")
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
            services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = services.BuildServiceProvider().GetService<TokenValidationParameters>();
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            // If the request is for our SignalR hubs
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/notificationhub")))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };

                });

            services.AddAuthentication()
                .AddCookie("Cookie", opts => {
                opts.Cookie.Name = "critterlogin";
                opts.Cookie.Expiration = new TimeSpan(14);//todo configurable
                opts.EventsType = typeof(CookieEventHandler);
                opts.TicketDataFormat = new CookieTicketDataFormat(services.BuildServiceProvider().GetService<IJwtProvider>(), services.BuildServiceProvider().GetService<IHttpContextAccessor>());
            });

            services.AddSignalR()
                .AddJsonProtocol(options =>
            {
                options.PayloadSerializerSettings.ContractResolver = new SensitiveDataContractResolver();
            });


            #endregion Framework

            //set up DI stuff

            //domains
            services.AddTransient<UserDomain>();
            services.AddTransient<NotificationDomain>();

            services.AddTransient<ErrorMiddleware>();

            //repositories
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IMessageRepository, MessageRepository>();

            //components
            services.AddJwt(Configuration);
            services.AddHttpContextAccessor();
            services.AddScoped<CookieEventHandler>();
            services.AddTransient<UserFilter>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseAuthentication();
            app.UseCors(PermittedOrigins);

            app.UseMiddleware<ErrorMiddleware>();
            app.UseSignalR(route =>
            {
                route.MapHub<NotificationHub>("/notificationhub");
            });

            app.UseMvc();//always last thing
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
