using CritterServer.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace CritterServer.Pipeline.Middleware
{
    public class ErrorMiddleware
    {
        private readonly RequestDelegate next;


        public ErrorMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string responseBody = null;
            int responseCode = 200;
            try
            {
                await next.Invoke(context);
            }
            catch(CritterException cex) 
            {
                switch(cex.LogLevelOverride)
                {
                    case LogLevel.Debug: Log.Debug(cex, cex.InternalMessage); break;
                    case LogLevel.Information: Log.Information(cex, cex.InternalMessage); break;
                    case LogLevel.Warning: Log.Warning(cex, cex.InternalMessage); break;
                    case LogLevel.None: break;
                    default: Log.Error(cex, cex.InternalMessage); break;
                }
                responseCode = (int)cex.HttpStatus;
                responseBody = cex.ClientMessage;
            }
            catch (InvalidCredentialException icex)
            {
                Log.Information(icex, "Failed login attempt");
                responseCode = 401;
                responseBody = icex.Message;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handled in Middleware");
                responseCode = 500;
                responseBody = ex.Message;
            }
            if (!string.IsNullOrEmpty(responseBody))
            {
                context.Response.StatusCode = responseCode;
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(responseBody));
            }
        }
    }
}
