﻿namespace Eryph.StateDb.Model;

// The change tracking in the controller module must be updated when modifying this entity.
public class ProviderSubnet : Subnet
{
    public string ProviderName { get; set; }

}