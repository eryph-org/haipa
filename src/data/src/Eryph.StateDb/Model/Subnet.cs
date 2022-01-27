﻿using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eryph.StateDb.Model
{
    public class Subnet
    {
        public Guid Id { get; set; }

        //public Network Network { get; set; }
        //public Guid NetworkId { get; set; }

        public bool IsPublic { get; set; }

        public bool DhcpEnabled { get; set; }
        public byte IpVersion { get; set; }
        public string GatewayAddress { get; set; }
        public string Address { get; set; }


    }
}