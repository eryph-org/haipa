﻿using System;
using System.Management.Automation;

public static class PsObjectDisposeExtensions
{
    public static void DisposeObject(this PSObject psObject)
    {
        if (psObject?.BaseObject is not IDisposable disposable) return;

        try
        {
            disposable.Dispose();
        }
        catch
        {
            // ignored
        }
    }
}