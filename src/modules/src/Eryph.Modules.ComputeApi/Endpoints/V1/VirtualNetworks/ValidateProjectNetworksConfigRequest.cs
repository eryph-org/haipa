﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Eryph.Modules.ComputeApi.Endpoints.V1.VirtualNetworks;

public class ValidateProjectNetworksConfigRequest
{
    public JsonElement Configuration { get; set; }
}
