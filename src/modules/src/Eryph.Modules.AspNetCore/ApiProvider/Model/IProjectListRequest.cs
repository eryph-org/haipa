﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eryph.Modules.AspNetCore.ApiProvider.Model;

public interface IProjectListRequest : IListRequest
{
    public string? ProjectId { get; }
}