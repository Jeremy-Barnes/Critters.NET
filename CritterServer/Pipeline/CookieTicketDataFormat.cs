﻿using CritterServer.Domains.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
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
        IHttpContextAccessor contextAccessor;

        public CookieTicketDataFormat(IJwtProvider jwt, IHttpContextAccessor contextAccessor)
        {
            this.jwt = jwt;
            this.contextAccessor = contextAccessor;
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
            if (authenticatedUser == null) {
                return null;
            }
            return new AuthenticationTicket(authenticatedUser, "Cookie");
        }
    }

    public class CookieEventHandler : CookieAuthenticationEvents
    {
        public CookieEventHandler() { }

        public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) 
        {
            return context.HttpContext.SignOutAsync("Cookie");
        }

        public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
        {
            return context.HttpContext.SignOutAsync("Cookie");
        }

        public override Task RedirectToLogout(RedirectContext<CookieAuthenticationOptions> context)
        {
            return context.HttpContext.SignOutAsync("Cookie");
        }
    }
}
