using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Pipeline
{
    public class AcceptedValuesAttribute : ValidationAttribute
    {
        ITypeEnforcer Enforcer;
        string RejectionMessage = "Sorry, that is not an accepted value.";

        public AcceptedValuesAttribute(bool enforceCasing, bool allowNull, params string[] acceptedValues)
        {
            Enforcer = new StringEnforcer(acceptedValues, enforceCasing, allowNull);
        }

        public AcceptedValuesAttribute(string clientErrorMessage, bool enforceCasing, bool allowNull, params string[] acceptedValues)
        {
            Enforcer = new StringEnforcer(acceptedValues, enforceCasing, allowNull);
            RejectionMessage = clientErrorMessage;
        }

        public AcceptedValuesAttribute(DateTime? notBefore, DateTime? notAfter, bool allowNull, bool allowFutureDates, bool allowPastDates)
        {
            Enforcer = new DateEnforcer(notBefore, notAfter, allowNull);
        }


        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (Enforcer.IsValid(value))
            {
                return ValidationResult.Success;
            }
            return new ValidationResult(RejectionMessage);
        }
    }

    class StringEnforcer : ITypeEnforcer
    {
        private bool AllowNull = false;
        private bool EnforceCasing = true;
        private string[] AcceptedValue;
        public StringEnforcer(string[] acceptedValues, bool enforceCasing, bool allowNull)
        {
            this.AcceptedValue = acceptedValues;
            this.EnforceCasing = enforceCasing;
            this.AllowNull = allowNull;
        }

        public bool IsValid(object value)
        {
            if ((value == null && !AllowNull) || AcceptedValue.Length == 0) return false;
            if (value == null && AllowNull) return true;

            string val = value as string;

            if (EnforceCasing != true)
            {
                return AcceptedValue.Any(av => av.Equals(val, StringComparison.OrdinalIgnoreCase));
            }
            return AcceptedValue.Contains(val);
        }
    }

    class DateEnforcer : ITypeEnforcer
    {
        private bool AllowNull = false;
        private DateTime? NotBefore;
        private DateTime? NotAfter;
        public DateEnforcer(DateTime? notBefore, DateTime? notAfter, bool allowNull)
        {
            this.NotBefore = notBefore;
            this.NotAfter = notAfter;
            this.AllowNull = allowNull;
        }

        public bool IsValid(object value)
        {
            if ((value == null && !AllowNull) || (NotBefore == null && NotAfter == null)) return false;
            if (value == null && AllowNull) return true;

            DateTime val = (DateTime)value;

            bool hasFailed = false;
            if (NotBefore != null)
            {
                hasFailed = val.ToUniversalTime() < NotAfter;
            }
            if (NotAfter != null && !hasFailed)
            {
                hasFailed = val.ToUniversalTime() > NotAfter;
            }
            return hasFailed;
        }
    }

    interface ITypeEnforcer
    {
        bool IsValid(object value);
    }
}
