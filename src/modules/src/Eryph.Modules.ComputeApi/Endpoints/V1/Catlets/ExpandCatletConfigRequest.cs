﻿using Eryph.Modules.AspNetCore.ApiProvider.Model;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Eryph.Modules.ComputeApi.Endpoints.V1.Catlets;

public class ExpandCatletConfigRequest : SingleEntityRequest
{
    [FromBody] public required ExpandCatletConfigRequestBody Body { get; set; }
}

public class ExpandCatletConfigRequestBody
{
    public Guid? CorrelationId { get; set; }

    public required JsonElement Configuration { get; set; }
}
