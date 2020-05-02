using CritterServer.Contract;
using CritterServer.DataAccess;
using CritterServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CritterServer.Pipeline.Middleware
{
    /// <summary>
    /// Attribute that indicates that a User parameter to the API call should be evaluated
    /// </summary>
    public class UserValidate: ITypedValidateAttribute
    {
        public enum ValidationType
        {
            EmailIsValidForm,
            UserNameIsValidForm,
            GenderOptionIsValidForm,
            NewEmailOrUserNameIsNotTaken,
            BirthdateIsValidForm,
            All
        }

        public ValidationType[] validations;
        public string parameterName;

        /// <summary>
        /// Constructor. Requires that you share the name of the parameter you want evaluated, in case there are multiple users, and only one needs validation
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="validations"></param>
        public UserValidate(string parameterName, params ValidationType[] validations)
        {
            this.validations = validations;
            this.parameterName = parameterName;
        }
    }

    /// <summary>
    /// Validates user as specified by the UserValidate attribute
    /// </summary>
    public class UserFilter : TypeFilter<User, UserValidate>
    {
        private IUserRepository userRepo;

        public UserFilter(IUserRepository userRepo)
        {
            this.userRepo = userRepo;
        }

        /// <summary>
        /// Executes before control is passed to the controller, but after auth handlers
        /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1
        /// </summary>
        /// <param name="context">Request Pipeline construct, includes HTTP Request information</param>
        /// <param name="Validations"></param>
        /// <returns></returns>
        public override async Task OnActionExecuting(ActionExecutingContext context, IEnumerable<UserValidate> Validations)
        {
            if (!context.ModelState.IsValid)
                throw new CritterException("The user you provided was busted, son!", "Invalid model state", HttpStatusCode.BadRequest);
            
            await Task.Run(async () => //run this validation in the background, let another thread come in in the meantime
            {
                foreach (var vAttribute in Validations)
                {
                    User validationParam = context.ActionArguments?.Where(x => x.Key == vAttribute.parameterName && x.Value is User).FirstOrDefault().Value as User;

                    if (validationParam != null)
                    {
                        foreach (var validation in vAttribute.validations)
                        {
                            switch (validation)
                            {
                                case UserValidate.ValidationType.EmailIsValidForm: EmailIsValidForm(validationParam); break;
                                case UserValidate.ValidationType.UserNameIsValidForm: UserNameIsValidForm(validationParam); break;
                                case UserValidate.ValidationType.GenderOptionIsValidForm: GenderOptionIsValidForm(validationParam); break;
                                case UserValidate.ValidationType.NewEmailOrUserNameIsNotTaken: await UserNameOrEmailIsNotDuplicated(validationParam, context.HttpContext.User); break;
                                case UserValidate.ValidationType.BirthdateIsValidForm: BirthdateIsValidForm(validationParam); break;

                                case UserValidate.ValidationType.All: //blech, good enough
                                    EmailIsValidForm(validationParam); UserNameIsValidForm(validationParam); GenderOptionIsValidForm(validationParam); 
                                    await UserNameOrEmailIsNotDuplicated(validationParam, context.HttpContext.User);
                                    BirthdateIsValidForm(validationParam);
                                    break;
                            }
                        }
                    }
                }
            });
        }

        public async Task<bool> UserNameOrEmailIsNotDuplicated(User user, ClaimsPrincipal loggedInUserJwtInfo, bool throwOnInvalid = true)
        {
            bool conflictFound = false;
            if(loggedInUserJwtInfo?.Claims?.Any() != true) //this is a new user with no login, not an account update
            {
                conflictFound = await userRepo.UserExistsByUserNameOrEmail(user.UserName, user.EmailAddress);
            } 
            else //logged in users will likely have a name that already exists in the db: their own
            {
                string searchEmail = null;
                string searchUserName = null;
                if(user.EmailAddress != loggedInUserJwtInfo.FindFirst(ClaimTypes.Email).Value)
                {
                    searchEmail = user.EmailAddress;
                }

                if (user.UserName != loggedInUserJwtInfo.FindFirst(ClaimTypes.Name).Value)
                {
                    searchUserName = user.UserName;
                }

                if(searchEmail != null || searchUserName != null)
                    conflictFound = await userRepo.UserExistsByUserNameOrEmail(searchEmail, searchUserName);
            }

            if (conflictFound)
            {
                if (throwOnInvalid)
                    throw new CritterException($"Sorry, someone already exists with that name or email!", $"Duplicate account creation attempt on {user.UserName} or {user.EmailAddress}", System.Net.HttpStatusCode.Conflict);
            }
            return !conflictFound;
        }

        public static bool BirthdateIsValidForm(User user, bool throwOnInvalid = true)
        {
            DateTime birthday;
            if (!DateTime.TryParse(user.Birthdate, out birthday))
            {
                if (throwOnInvalid)
                    throw new CritterException($"No one was born on {birthday}, we checked.", "Invalid birthday", System.Net.HttpStatusCode.BadRequest);
                return false;
            }
            return true;
        }

        public static bool EmailIsValidForm(User user, bool throwOnInvalid = true)
        {
            bool valid = false;
            CritterException critterException = null;
            try
            {
                valid = new MailAddress(user.EmailAddress).Address == user.EmailAddress;
            }
            catch (Exception ex)
            {
                critterException = new CritterException("Sorry friend, that email address is invalid.", $"Garbage email {user.EmailAddress}", HttpStatusCode.BadRequest, ex);
            }
            if(throwOnInvalid && !valid)
            {
                if(critterException == null)
                {
                    critterException = new CritterException("Sorry friend, that email address is invalid.", $"Garbage email {user.EmailAddress}", HttpStatusCode.BadRequest);
                }
                throw critterException;
            }
            return valid;
        }

        public static bool UserNameIsValidForm(User user, bool throwOnInvalid = true)
        {          
                if (!string.IsNullOrEmpty(user.UserName))
                {
                    if(user.UserName.Length > 0)
                    {
                        return true;
                    }
                }
            if (throwOnInvalid)
            {
                throw new CritterException("That username is invalid.", $"Garbage username {user.UserName}", HttpStatusCode.BadRequest);
            }
            return false;
        }

        public static bool GenderOptionIsValidForm(User user, bool throwOnInvalid = true)
        {
            if (!string.IsNullOrEmpty(user.Gender))
            {
                user.Gender = user.Gender.ToLower();
                if (new List<string>{ "male", "female", "other" }.Contains(user.Gender))
                {
                    return true;
                }
            }
            if (throwOnInvalid)
            {
                throw new CritterException("Sorry! Please choose from: male, female, or other.", null, HttpStatusCode.BadRequest);
            }
            return false;
        }
    }
}
