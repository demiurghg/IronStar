using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Audio;
using Fusion.Engine.Common;

namespace Fusion.Engine.Audio 
{
	[ConfigClass]
	public sealed partial class SoundSystem : GameComponent 
	{
		/// <summary>
		/// Mastering voice value.
		/// </summary>
		[Config]
        public float MasterVolume { get; set; }

		/// <summary>
		/// Overall distance scale. Default = 1.
		/// </summary>
		[Config]
        public float DistanceScale { get; set; }

		/// <summary>
		/// Overall doppler scale. Default = 1;
		/// </summary>
		[Config]
        public float DopplerScale { get; set; }


		/// <summary>
		/// Global speed of sound. Default = 343.5f;
		/// </summary>
		[Config]
        public float SpeedOfSound { get; set; }
	}
}
