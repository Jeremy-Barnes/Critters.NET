using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Utilities.Serialization
{
    public static class Extensions
    {
        public static void AddDataContractResolver(this IMvcBuilder builder)
        {
            builder.AddJsonOptions(opts =>
            {
                 opts.SerializerSettings.ContractResolver = new SensitiveDataContractResolver();
            });
        }

    }
}
