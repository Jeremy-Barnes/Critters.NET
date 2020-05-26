using CritterServer.Models;
using CritterServer.Pipeline;
using CritterServer.Pipeline.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CritterServer.Domains.Components
{
    ///<inheritdoc/>
    public class JwtProvider : IJwtProvider
    {
        private string SecretKey;
        private SymmetricSecurityKey SigningKey;
        private TokenValidationParameters tokenValidationOptions;

        public JwtProvider(string secretKey, TokenValidationParameters tokenValidationOptions)
        {
            this.SecretKey = secretKey;
            this.SigningKey = new SymmetricSecurityKey(Convert.FromBase64String(SecretKey));
            this.tokenValidationOptions = tokenValidationOptions;
        }

        public SymmetricSecurityKey GetSigningKey()
        {
            return SigningKey;
        }


        public string GenerateToken(User user)
        {
            UserFilter.UserNameIsValidForm(user);
            UserFilter.EmailIsValidForm(user);
            return GenerateToken(user.UserName, user.EmailAddress);
        }

        public string GenerateToken(string userName, string email)
        {
            SecurityTokenDescriptor std = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Email, email)

                }),
                Expires = DateTime.UtcNow.AddDays(14), //todo configurable, same value as the cookie
                SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha384Signature),
                IssuedAt = DateTime.UtcNow,
                Issuer = "critters!",
            };
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken jwt = tokenHandler.CreateToken(std);
            return tokenHandler.WriteToken(jwt);
        }

        public bool ValidateToken(string jwtString)
        {
            if (!string.IsNullOrEmpty(jwtString))
            {
                if (CrackJwt(jwtString) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public ClaimsPrincipal CrackJwt(string jwtString)
        {
            SecurityToken jwt;
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                return tokenHandler.ValidateToken(jwtString, tokenValidationOptions, out jwt);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, $"Invalid JWT {jwtString} was passed in and rejected.");
                return null;
            }
        }
    }

    /// <summary>
    /// Used to create and crack JWTs for cookies. 
    /// Pipeline handles authorization heaaders magically (using TokenValidationParameters created in JwtExtensions.AddJwt below
    /// </summary>
    public interface IJwtProvider
    {
        string GenerateToken(User user);

        string GenerateToken(string userName, string email);

        bool ValidateToken(string jwtString);
        ClaimsPrincipal CrackJwt(string jwtString);
        SymmetricSecurityKey GetSigningKey();
    }

    public static class JwtExtensions
    {
        /// <summary>
        /// Extension method
        /// Adds pipeline support for JWT Auth Headers (Bearer tokens)
        /// Adds JwtProvider as a service so arbitrary code can create and decode JWTs (useful for putting JWTs into cookies)
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration config)
        {
            string secretKey = config.GetValue<string>("JwtSigningKey");
            var jwtProvider = new JwtProvider(secretKey, getTokenValidationParameters(secretKey));
            //services.AddSingleton<JwtBearerOptions>(new JwtBearerOptions()
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jbo => {
                jbo.TokenValidationParameters = getTokenValidationParameters(secretKey);
                jbo.Events = new JwtBearerEvents
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

            services.AddSingleton<IJwtProvider, JwtProvider>(sp => jwtProvider);

            services.AddAuthentication()
                .AddCookie("Cookie", opts => {
                    opts.Cookie.Name = "critterlogin";
                    opts.ExpireTimeSpan = new TimeSpan(14);//todo configurable
                    opts.EventsType = typeof(CookieEventHandler);
                    opts.TicketDataFormat = new CookieTicketDataFormat(jwtProvider);
                });


            return services;
        }

        private static TokenValidationParameters getTokenValidationParameters(string jwtSigningKey)
        { 
            return new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSigningKey)),
                ValidIssuer = "critters!",
                ValidateAudience = false,
                ValidateActor = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true
            };
        }
    }
}
