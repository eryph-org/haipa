﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eryph.ZeroState
{
    public interface IZeroStateConfig
    {
        public string ProjectsConfigPath { get; }

        public string ProjectNetworksConfigPath { get; }

        public string NetworkPortsConfigPath { get; }
    }
}
