using CritterServer.DataAccess;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Pipeline.Middleware
{
    public class LoggedInUserModelBinder : IModelBinder
    {

        IUserRepository userRepository;

        public LoggedInUserModelBinder(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var userModel = userRepository.RetrieveUserByUserName(bindingContext.HttpContext.User.Identity.Name);

            bindingContext.Result = ModelBindingResult.Success(userModel);
            return Task.CompletedTask;
        }
    }
}
