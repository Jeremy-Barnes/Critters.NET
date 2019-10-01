using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CritterServer.Pipeline
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
            try
            {
                await next.Invoke(context);
            } catch(Exception ex)
            {
                Log.Error(ex, "Error handled in Middleware");
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(ex.Message));
                context.Response.StatusCode = 401; //todo this needs some actual logic. This is a dumb hack that doesn't even work.
              //  throw;
            }
        }


    }
}
