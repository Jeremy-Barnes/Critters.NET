using CritterServer.Domains.Components;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Tests.UnitTests
{
    public class JwtTests
    {
        private static string jwtSecretKey1 = "T25lIEV4Y2VwdGlvbmFsbHkgTG9uZyBTZWNyZXQgS2V5IFBsZWFzZSEgRm9yIFJlYWwhIEV2ZW4gTG9uZ2VyIFRoYW4gWW91J2QgUmVhc29uYWJseSBBbnRpY2lwYXRl";
        private static string jwtSecretKey2 = "z25lIEV5Y2VwdGlvbmFsbHkgTG9uZyBTZWNyZXQgS2V5IFBsZWFzZSEgRm9yIFJlYWwhIEV2ZW4gTG9uZ2VyIFRoYW4gWW91J2QgUmVhc29uYWJseSBBbnRpY2lwYXRl";

        private TokenValidationParameters tokenParams1 = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSecretKey1)),
            ValidIssuer = "critters!",
            ValidateAudience = false,
            ValidateActor = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true
        };

        private TokenValidationParameters tokenParams2 = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSecretKey2)),
            ValidIssuer = "critters!",
            ValidateAudience = false,
            ValidateActor = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true
        };


        [Fact]
        public void JwtValidates()
        {
            JwtProvider jwtProvider = new JwtProvider(jwtSecretKey1, tokenParams1);

            string jwt = jwtProvider.GenerateToken(new CritterServer.Models.User() { UserName = "A.TEST.USERNAME", EmailAddress = "a@a.com" });
            Assert.True(jwtProvider.ValidateToken(jwt));
        }


        [Fact]
        public void JwtRejectsBadKey()
        {
            //providers with different secret keys
            JwtProvider jwtProvider1 = new JwtProvider(jwtSecretKey1, tokenParams1);
            JwtProvider jwtProvider2 = new JwtProvider(jwtSecretKey2, tokenParams2);

            string jwt1 = jwtProvider1.GenerateToken(new CritterServer.Models.User() { UserName = "A.TEST.USERNAME", EmailAddress = "a@a.com" });
            string jwt2 = jwtProvider2.GenerateToken(new CritterServer.Models.User() { UserName = "A.TEST.USERNAME", EmailAddress = "a@a.com" });

            Assert.False(jwtProvider2.ValidateToken(jwt1));
            Assert.True(jwtProvider1.ValidateToken(jwt1));
            Assert.False(jwtProvider1.ValidateToken(jwt2));
            Assert.True(jwtProvider2.ValidateToken(jwt2));
        }
    }
}
