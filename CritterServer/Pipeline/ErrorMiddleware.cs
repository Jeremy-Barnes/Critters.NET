using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
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
                throw;
            }
        }


    }
}
