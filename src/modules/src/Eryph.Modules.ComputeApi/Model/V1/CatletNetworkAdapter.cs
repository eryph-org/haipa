﻿using System.ComponentModel.DataAnnotations;

namespace Eryph.Modules.ComputeApi.Model.V1;

public class CatletNetworkAdapter
{
    public required string Name { get; set; }
    
    public required string MacAddress { get; set; }
}