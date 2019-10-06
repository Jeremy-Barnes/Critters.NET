using CritterServer.Domains.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CritterServer.Pipeline
{
    public class CookieTicketDataFormat : ISecureDataFormat<AuthenticationTicket>
    {
        IJwtProvider jwt;

        public CookieTicketDataFormat(IJwtProvider jwt)
        {
            this.jwt = jwt;
        }

        public string Protect(AuthenticationTicket data)
        {
            return Protect(data, null);
        }

        public string Protect(AuthenticationTicket data, string purpose)
        {
            return jwt.GenerateToken(data.Principal.Identity.Name);
        }

        public AuthenticationTicket Unprotect(string protectedText)
        {
            return Unprotect(protectedText, null);
        }

        public AuthenticationTicket Unprotect(string protectedText, string purpose)
        {
            var authenticatedUser = jwt.CrackJwt(protectedText);
            if (authenticatedUser == null)
                throw new InvalidCredentialException("Sorry, you've been logged out, please log in again!");

            return new AuthenticationTicket(authenticatedUser, "Cookie");
        }
    }
}
