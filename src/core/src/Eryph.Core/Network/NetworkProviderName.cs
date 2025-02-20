﻿using Eryph.ConfigModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eryph.Core.Network;

public class NetworkProviderName : EryphName<NetworkProviderName>
{
    public NetworkProviderName(string value) : base(value)
    {
        ValidOrThrow(Validations<CatletName>.ValidateCharacters(
                         value,
                         allowDots: false,
                         allowHyphens: true,
                         allowUnderscores: false,
                         allowSpaces: false)
                     | Validations<CatletName>.ValidateLength(value, 1, 50));
    }
}
