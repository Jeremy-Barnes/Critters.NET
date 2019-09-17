using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }

        public int Cash { get; set; }

        public string Sex { get; set; }
        public string Birthdate { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Postcode { get; set; }
         
        public string Password { get; set; }
        public string Salt { get; set; }
        public string TokenSelector { get; set; }
        public string TokenValidator { get; set; }
         
        public bool IsActive { get; set; }

    }
}
