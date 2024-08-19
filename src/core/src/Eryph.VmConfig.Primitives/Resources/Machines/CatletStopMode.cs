﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Eryph.Resources.Machines;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CatletStopMode
{
    /// <summary>
    /// This mode attempts to gracefully shut down the catlet.
    /// </summary>
    Shutdown = 0,

    /// <summary>
    /// This mode immediately stops the catlet comparable to pulling the power plug.
    /// </summary>
    Hard = 1,

    /// <summary>
    /// This mode is not yet implemented.
    /// </summary>
    Kill = 2,
}