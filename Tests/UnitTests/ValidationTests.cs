using CritterServer.Contract;
using CritterServer.Domains.Components;
using CritterServer.Models;
using CritterServer.Pipeline;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Xunit;

namespace Tests.UnitTests
{
    public class ValidationTests
    {
        public TestUtilities utils = new TestUtilities();

        [Fact]
        public void AcceptedValuesEnforceCasing()
        {
            var attribute = new AcceptedValuesAttribute(true, false, "a", "B", "c");
            Assert.Throws<ValidationException>(() => attribute.Validate("A", new ValidationContext(new Pet())));
            attribute.Validate("a", new ValidationContext(new Pet()));
            attribute.Validate("c", new ValidationContext(new Pet()));
            Assert.Throws<ValidationException>(() => attribute.Validate("d", new ValidationContext(new Pet())));
        }

        [Fact]
        public void AcceptedValuesEnforceNullability()
        {
            var attribute = new AcceptedValuesAttribute(true, false, "a", "B", "c");
            Assert.Throws<ValidationException>(() => attribute.Validate(null, new ValidationContext(new Pet())));
            attribute.Validate("a", new ValidationContext(new Pet()));
            attribute.Validate("c", new ValidationContext(new Pet()));
            Assert.Throws<ValidationException>(() => attribute.Validate("d", new ValidationContext(new Pet())));

            attribute = new AcceptedValuesAttribute(true, true, "a", "B", "c");
            Assert.Throws<ValidationException>(() => attribute.Validate("A", new ValidationContext(new Pet())));
            attribute.Validate(null, new ValidationContext(new Pet()));
            attribute.Validate("c", new ValidationContext(new Pet()));
            Assert.Throws<ValidationException>(() => attribute.Validate("d", new ValidationContext(new Pet())));
        }

        [Fact]
        public void AcceptedValuesPermitsCasing()
        {
            var attribute = new AcceptedValuesAttribute(false, false, "a", "B", "c");
            Assert.Throws<ValidationException>(() => attribute.Validate("d", new ValidationContext(new Pet())));
            attribute.Validate("A", new ValidationContext(new Pet()));
            attribute.Validate("b", new ValidationContext(new Pet()));
            attribute.Validate("c", new ValidationContext(new Pet()));
        }
    }
}
