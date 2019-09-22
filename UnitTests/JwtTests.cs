using CritterServer.Domains.Components;
using System;
using Xunit;

namespace UnitTests
{
    public class JwtTests
    {
        [Fact]
        public void JwtValidates()
        {
            JwtProvider jwtProvider = new JwtProvider("T25lIEV4Y2VwdGlvbmFsbHkgTG9uZyBTZWNyZXQgS2V5IFBsZWFzZSEgRm9yIFJlYWwhIEV2ZW4gTG9uZ2VyIFRoYW4gWW91J2QgUmVhc29uYWJseSBBbnRpY2lwYXRl");

            string jwt = jwtProvider.GenerateToken(new CritterServer.Models.User() { UserName = "A.TEST.USERNAME" });
            Assert.True(jwtProvider.ValidateToken(jwt));
        }


        [Fact]
        public void JwtRejectsBadKey()
        {
            //providers with different secret keys
            JwtProvider jwtProvider1 = new JwtProvider("T25lIEV4Y2VwdGlvbmFsbHkgTG9uZyBTZWNyZXQgS2V5IFBsZWFzZSEgRm9yIFJlYWwhIEV2ZW4gTG9uZ2VyIFRoYW4gWW91J2QgUmVhc29uYWJseSBBbnRpY2lwYXRl");
            JwtProvider jwtProvider2 = new JwtProvider("z25lIEV5Y2VwdGlvbmFsbHkgTG9uZyBTZWNyZXQgS2V5IFBsZWFzZSEgRm9yIFJlYWwhIEV2ZW4gTG9uZ2VyIFRoYW4gWW91J2QgUmVhc29uYWJseSBBbnRpY2lwYXRl");

            string jwt1 = jwtProvider1.GenerateToken(new CritterServer.Models.User() { UserName = "A.TEST.USERNAME" });
            string jwt2 = jwtProvider2.GenerateToken(new CritterServer.Models.User() { UserName = "A.TEST.USERNAME" });

            Assert.False(jwtProvider2.ValidateToken(jwt1));
            Assert.True(jwtProvider1.ValidateToken(jwt1));
            Assert.False(jwtProvider1.ValidateToken(jwt2));
            Assert.True(jwtProvider2.ValidateToken(jwt2));
        }
    }
}
