﻿using System;

namespace Eryph.Modules.AspNetCore.ApiProvider.Model;

public interface IListRequest
{
    public Guid? Project { get; }

}