using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Fusion.Core.Extensions;

namespace Fusion.Engine.Graphics {

	public class FogSettings {

		/// <summary>
		/// 
		/// </summary>
		public float DistanceAttenuation {
			get {
				return (float)( Math.Log(VisibilityPercentage) / VisibilityDistance );
			}
		}


		/// <summary>
		/// Distance where visibility is less than VisibilityPercentage
		/// </summary>
		public float VisibilityDistance {
			get; set;
		} = 100.0f;


		/// <summary>
		/// Visibility percentage
		/// </summary>
		public float VisibilityPercentage {
			get; set;
		} = 0.5f;


		/// <summary>
		/// Fog color
		/// </summary>
		public Color4 Color {
			get; set;
		} = new Color4( 0.5f, 0.5f, 0.5f, 1.0f );


		/// <summary>
		/// 
		/// </summary>
		public FogSettings ()
		{
		}
	}
}
