﻿using System;
using Eryph.VmManagement;

namespace Eryph.Modules.VmHostAgent;

public class TraceRecord
{
    public TraceData Data { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public string Message { get; set; }
}