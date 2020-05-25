using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Utilities.Serialization
{
    public static class Extensions
    {
        public static IMvcBuilder AddDataContractResolver(this IMvcBuilder builder)
        {
            builder.AddNewtonsoftJson(opts =>
            {
                 opts.SerializerSettings.ContractResolver = new SensitiveDataContractResolver();
            });
            return builder;
        }

    }
}
