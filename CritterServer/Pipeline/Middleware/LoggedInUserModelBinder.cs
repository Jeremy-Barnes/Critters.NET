using CritterServer.DataAccess;
using CritterServer.Models;
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

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            User userModel = (await userRepository.RetrieveUsersByUserName(bindingContext.HttpContext.User.Identity.Name)).First();

            bindingContext.Result = ModelBindingResult.Success(userModel);
        }
    }
}
