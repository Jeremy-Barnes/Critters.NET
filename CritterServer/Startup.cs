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
using Microsoft.Extensions.Hosting;

namespace CritterServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //framework
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddDataContractResolver().AddMvcOptions((options) => 
                {
                    options.Filters.Add<UserFilter>();
                    options.EnableEndpointRouting = true;
                });

            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
            services.AddScoped<IDbConnection>((sp) =>
            {
                var conn = DbProviderFactories.GetFactory("Npgsql").CreateConnection();
                conn.ConnectionString = Configuration.GetConnectionString("Sql");
                return conn;
            });

            configureLogging();

            //auth
            services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(c => c.TokenValidationParameters = services.BuildServiceProvider().GetService<TokenValidationParameters>());

            services.AddAuthentication()
                .AddCookie("Cookie", opts => {
                opts.Cookie.Name = "critterlogin";
                opts.ExpireTimeSpan = new TimeSpan(14);//todo configurable
                opts.EventsType = typeof(CookieEventHandler);
                opts.TicketDataFormat = new CookieTicketDataFormat(services.BuildServiceProvider().GetService<IJwtProvider>(), services.BuildServiceProvider().GetService<IHttpContextAccessor>());
            });

            //domains
            services.AddTransient<UserAuthenticationDomain>();
            services.AddTransient<ErrorMiddleware>();

            //repositories
            services.AddTransient<IUserRepository, UserRepository>();

            //components
            services.AddJwt(Configuration);
            services.AddHttpContextAccessor();
            services.AddScoped<CookieEventHandler>();
            services.AddTransient<UserFilter>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();
            
            app.UseMiddleware<ErrorMiddleware>();
            
           app.UseEndpoints(endpoints => {
               endpoints.MapControllers();
           }); ;//last thing
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
