﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Eryph.StateDb.Model;
using LanguageExt.Pipes;

namespace Eryph.StateDb.Specifications
{
    public sealed class FloatingNetworkPortSpecs
    {
        public sealed class GetForConfig : Specification<FloatingNetworkPort>
        {
            public GetForConfig()
            {
                Query.Include(x => x.IpAssignments)
                    .ThenInclude(x => ((IpPoolAssignment)x).Pool);
            }
        }

        public sealed class GetByName : Specification<FloatingNetworkPort>, ISingleResultSpecification
        {
            public GetByName(string providerName, string subnetName, string name)
            {
                Query.Where(x => x.ProviderName == providerName
                                 && x.SubnetName == subnetName
                                 && x.Name == name);
            }
        }
    }
}