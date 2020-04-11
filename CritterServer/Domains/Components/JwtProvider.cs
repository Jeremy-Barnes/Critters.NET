using CritterServer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
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
            return GenerateToken(user.UserName);
        }

        public string GenerateToken(string userName)
        {
            SecurityTokenDescriptor std = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userName)
                }),
                Expires = DateTime.UtcNow.AddDays(14), //todo configurable, same value as the cookie in Startup
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

        string GenerateToken(string userName);

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
            services.AddSingleton<TokenValidationParameters>(sp => {
                return new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(config.GetValue<string>("JwtSigningKey"))),
                    ValidIssuer = "critters!",
                    ValidateAudience = false,
                    ValidateActor = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireExpirationTime = true
                };
            });
            services.AddSingleton<IJwtProvider, JwtProvider>(sp =>
                new JwtProvider(config.GetValue<string>("JwtSigningKey"), services.BuildServiceProvider().GetService<TokenValidationParameters>()));

            return services;
        }
    }
}
