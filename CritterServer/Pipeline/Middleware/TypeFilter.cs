using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CritterServer.Pipeline.Middleware
{
    /// <summary>
    /// A Middleware Filter meant to allow pre-controller validation
    /// Many APIs will require identical validation, no need for that to clutter up the domain
    /// </summary>
    /// <typeparam name="T">Type that will be analyzed and validated</typeparam>
    /// <typeparam name="V">Concrete ITypedValidateAttribute </typeparam>
    public abstract class TypeFilter<T, V> : IAsyncActionFilter where V : ITypedValidateAttribute
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var validateAttributesOnMethod = (context.ActionDescriptor as ControllerActionDescriptor)
                .MethodInfo
                .GetCustomAttributes<V>();

            if (validateAttributesOnMethod.Any()) //only execute validation if we have any attributes. 
            {
                await OnActionExecuting(context, validateAttributesOnMethod); //OnActionExecuting is implemented in subclasses and who knows what expensive logic is down in there
            }
            await next.Invoke();
        }

        /// <summary>
        /// Executes before control is passed to the controller, but after auth handlers
        /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1
        /// </summary>
        /// <param name="context">Request Pipeline construct, includes HTTP Request information</param>
        /// <param name="Validations"></param>
        /// <returns>awaitable Task</returns>
        public abstract Task OnActionExecuting(ActionExecutingContext context, IEnumerable<V> Validations);
    }

    public abstract class ITypedValidateAttribute : Attribute
    {
    }

}
