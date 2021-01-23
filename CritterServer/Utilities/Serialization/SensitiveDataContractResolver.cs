using CritterServer.DataAccess;
using CritterServer.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CritterServer.Utilities.Serialization
{
    public class SensitiveDataContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if(member.CustomAttributes?.Any(a => a.AttributeType == typeof(InternalOnly)) ?? false)
                property.ShouldSerialize = (instance) =>
                {
                    return false;
                };

            if (member.CustomAttributes?.Any(a => a.AttributeType == typeof(OwnerOnly)) ?? false)
                property.ShouldSerialize = (instance) =>
                {
                    if (instance is IDataOwner ido)
                    {
                        return ido.ShowPrivateData;
                    };
                    return true;
                };

            return property;
        }
    }
}
