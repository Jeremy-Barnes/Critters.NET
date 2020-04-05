using System.Data;
using System.Data.Common;
using CritterServer.Domains;
using CritterServer.DataAccess;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using CritterServer.Domains.Components;
using Serilog.Events;
using CritterServer.Pipeline;
using Microsoft.IdentityModel.Tokens;
using System;
using CritterServer.Utilities.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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
            services.AddMvc()
                .AddDataContractResolver();

            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
            services.AddScoped<IDbConnection>((sp) =>
            {
                var conn = DbProviderFactories.GetFactory("Npgsql").CreateConnection();
                conn.ConnectionString = Configuration.GetConnectionString("Sql");
                return conn;
            });

            ConfigureLogging();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(c => c.TokenValidationParameters = services.BuildServiceProvider().GetService<TokenValidationParameters>());

            services.AddAuthentication("Cookie").AddCookie("Cookie", opts => {
                opts.Cookie.Name = "critterlogin";
                opts.ExpireTimeSpan = new TimeSpan(14);//todo configurable

                opts.TicketDataFormat = new CookieTicketDataFormat(services.BuildServiceProvider().GetService<IJwtProvider>());
            });

            //domains
            services.AddTransient<UserAuthenticationDomain>();
            services.AddTransient<ErrorMiddleware>();

            //repositories
            services.AddTransient<IUserRepository, UserRepository>();

            //components
            services.AddJwt(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseMiddleware<ErrorMiddleware>();
        }

        private void ConfigureLogging()
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
