using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Utilities.Serialization
{
    public class InternalOnly: Attribute
    {
    }

    public class OwnerOnly : Attribute
    {
    }
}
