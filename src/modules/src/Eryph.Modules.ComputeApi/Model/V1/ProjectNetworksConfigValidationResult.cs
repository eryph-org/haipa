﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eryph.Modules.ComputeApi.Model.V1;

public class ProjectNetworksConfigValidationResult
{
    /// <summary>
    /// Indicates whether the network configuration is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Contains a list of the issues when the configuration is invalid.
    /// </summary>
    public IReadOnlyList<ValidationIssue>? Errors { get; set; }
}
