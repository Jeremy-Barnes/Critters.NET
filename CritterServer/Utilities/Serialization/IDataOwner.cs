﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Utilities.Serialization
{
    public interface IDataOwner
    {
        public bool ShowPrivateData { get; set; }
    }
}
