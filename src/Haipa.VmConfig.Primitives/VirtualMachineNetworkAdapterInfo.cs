﻿namespace Haipa.Messages.Events
{
    public class VirtualMachineNetworkAdapterInfo
    {
        public string Id { get; set; }
        public string AdapterName { get; set; }
        public string VirtualSwitchName { get; set; }
        public ushort VLanId { get; set; }
        public string MACAddress { get; set; }        
    }


    public class VMHostSwitchInfo
    {
        public string Id { get; set; }
        public string VirtualSwitchName { get; set; }
    }
}