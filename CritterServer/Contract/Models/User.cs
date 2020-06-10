using CritterServer.Pipeline;
using CritterServer.Utilities.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CritterServer.Models
{
    public class User
    {
        [InternalOnly]
        [BindNever]
        public int UserId { get; set; }
        [Required]
        [MaxLength(24)]
        public string UserName { get; set; }
        [MaxLength(24)]
        public string FirstName { get; set; }
        [MaxLength(24)]
        public string LastName { get; set; }
        [EmailAddress]
        [MaxLength(100)]
        public string EmailAddress { get; set; }

        [BindNever]
        public int Cash { get; set; }
        [AcceptedValues(false, true, "male","female","other")]
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
