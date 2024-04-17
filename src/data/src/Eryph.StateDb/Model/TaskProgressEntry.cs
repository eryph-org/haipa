﻿using System;

namespace Eryph.StateDb.Model;

public class TaskProgressEntry
{
    public Guid Id { get; set; }
    public Guid OperationId { get; set; }
    public Guid TaskId { get; set; }

    public virtual OperationTaskModel Task { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public int Progress { get; set; }
}