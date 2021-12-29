using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Shell;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using System.Net;
using System.Net.Sockets;
using Lidgren.Network;
using System.IO.Compression;
using System.IO;

namespace Fusion.Engine.Common 
{
	[ConfigClass]
	public class Network
	{
		[Config]
		static public int Port { get; set; } = 28101;

		[Config]
		static public int MaxClients { get; set; }

		[Config]
		static public bool ShowPackets { get; set; }

		[Config]
		static public float SimulatePacketsLoss { get; set; }

		[Config]
		static public float SimulateMinLatency { get; set; }
		
		[Config]
		static public float SimulateRandomLatency { get; set; }

		[Config]
		static public bool ShowSnapshots { get; set; }

		[Config]
		static public bool ShowUserCommands { get; set; }

		[Config]
		static public bool ShowLatency { get; set; }
	}

}
