﻿using Eryph.Core.Sys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Eryph.VmManagement.Inventory;

public static class HardwareIdQueries<RT> where RT : struct, HasRegistry<RT>
{
    public static Eff<RT, Guid> ReadSmBiosUuid() =>
        from wmiValue in Eff(() =>
        {
            // TODO use WMI runtime
            using var uuidSearcher = new ManagementObjectSearcher("SELECT UUId FROM Win32_ComputerSystemProduct");
            var result = uuidSearcher.Get().Cast<ManagementBaseObject>().HeadOrNone();
            return result.Bind(r => Optional(r["UUId"] as string));
        })
        from guid in wmiValue.Bind(parseGuid)
            // According to SMBIOS specification, both all 0s and all 1s (0xFF)
            // indicate that the UUID is not set.
            .Filter(g => g != Guid.Empty && g != Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"))
            .ToEff(Error.New("The found SMBIOS UUID is not a valid GUID."))
        select guid;

    public static Eff<RT, Guid> ReadCryptographyGuid() =>
        from value in Registry<RT>.getRegistryValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid")
        from validValue in value.ToEff(Error.New("Could not read the Machine GUID from the registry."))
        let guid = from s in Optional(validValue as string)
            from g in parseGuid(s)
            select g
        from validGuid in guid.ToEff(Error.New("The Machine GUID is not a valid GUID."))
        select validGuid;

    public static Eff<RT, Guid> ReadFallbackGuid() =>
        from value in Registry<RT>.getRegistryValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\dbosoft\Eryph", "HardwareId")
        from validValue in value.ToEff(Error.New("Could not read the Eryph Hardware ID from the registry."))
        let guid = from s in Optional(validValue as string)
            from g in parseGuid(s)
            select g
        from validGuid in guid.ToEff(Error.New("The Eryph Hardware ID is not a valid GUID."))
        select validGuid;

    public static Eff<RT, Guid> EnsureFallbackGuid() =>
        from _ in SuccessEff(unit)
        select Guid.Empty;

    public static string HashHardwareId(Guid hardwareId)
    {
        var hashBytes = SHA256.HashData(hardwareId.ToByteArray());
        return Convert.ToHexString(hashBytes[..16]);
    }
}

public static class HardwareIdHasher
{
    public static string HashHardwareId(Guid hardwareId)
    {
        var hashBytes = SHA256.HashData(hardwareId.ToByteArray());
        return Convert.ToHexString(hashBytes[..16]);
    }
}
