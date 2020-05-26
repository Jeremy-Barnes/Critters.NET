using CritterServer.Utilities.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Models
{
    public class User
    {
        [InternalOnly]
        [BindNever]
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }

        [BindNever]
        public int Cash { get; set; }

        public string Gender { get; set; }
        public DateTime Birthdate { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Postcode { get; set; }

        [InternalOnly]
        public string Password { get; set; }
        [JsonIgnore]
        [BindNever]
        public string Salt { get; set; }

        public bool IsActive { get; set; }

    }
}
