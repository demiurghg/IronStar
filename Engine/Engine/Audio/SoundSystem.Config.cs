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
		public static float MasterVolume { get; set; } = 1.0f;

		/// <summary>
		/// Overall distance scale. Default = 1.
		/// </summary>
		[Config]
		public static float DistanceScale { get; set; } = 1.0f;

		/// <summary>
		/// Overall doppler scale. Default = 1;
		/// </summary>
		[Config]
		public static float DopplerScale { get; set; } = 1;

		/// <summary>
		/// Global speed of sound. Default = 343.5f;
		/// </summary>
		[Config]
		public static float SpeedOfSound { get; set; } = 343.5f;
	}
}
