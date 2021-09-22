using System;
using System.Management;

namespace Fusion.Development
{
    public class GpuInfo
    {
        public GpuInfo(ManagementBaseObject query, int index)
        {
            Name = query["Caption"].ToString();
            DeviceId = query["DeviceID"].ToString();
            Family = query["VideoProcessor"].ToString();
            long memory = 0;
            Int64.TryParse(query["AdapterRAM"].ToString(), out memory);
            Memory = memory;
            DACType = query["AdapterDACType"].ToString();
            Monochrome = query["Monochrome"].ToString();
            InstalledDisplayDriver = query["InstalledDisplayDrivers"].ToString();
            DriverVersion = query["DriverVersion"].ToString();
            Architecture = query["VideoArchitecture"].ToString();
            VideoMemoryType = query["VideoMemoryType"].ToString();
            Index = index;
        }

        public string Name { get; }
        public string DeviceId { get; }
        public string Family { get; }
        public long Memory { get; }
        public string DACType { get; }
        public string Monochrome { get; }
        public string InstalledDisplayDriver { get; }
        public string DriverVersion { get; }
        public string Architecture { get; }
        public string VideoMemoryType { get; }
        public int Index { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
