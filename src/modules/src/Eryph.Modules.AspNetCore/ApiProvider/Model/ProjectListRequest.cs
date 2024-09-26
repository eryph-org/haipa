﻿using System;
using Microsoft.AspNetCore.Mvc;

namespace Eryph.Modules.AspNetCore.ApiProvider.Model;

public class ProjectListRequest : ListRequest, IProjectListRequest
{
    [FromQuery(Name = "projectId")] public string? ProjectId { get; set; }
}
