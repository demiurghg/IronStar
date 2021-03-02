using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Core.Shell;
using Newtonsoft.Json;
using Fusion.Widgets.Advanced;

namespace IronStar.Mapping 
{
	public class MapEnvironment 
	{
		[AECategory( "Physics" )]
		public float Gravity { get; set; } = 48;

		[AECategory( "Sky" )]
		[AESlider(2,8,0.25f,0.01f)]
		public float SkyTrubidity {
			get {
				return turbidity;
			}
			set {
				turbidity = MathUtil.Clamp( value, 2, 8 );
			}
		}
		float turbidity = 2;

		[AECategory( "Sky" )]
		[AESlider(0,90,15,0.01f)]
		public float SunAltitude { get; set; } = 45;

		[AECategory( "Sky" )]
		[AESlider(0,10,1,0.01f)]
		public float SkyIntensity { get; set; } = 1;

		[AECategory( "Sky" )]
		[AESlider(-180,180,15,0.1f)]
		public float SunAzimuth { get; set; } = 45;

		[AECategory( "Sky" )]
		[AESlider(0,500,10,1)]
		public float SunIntensity { get; set; } = 100;

		[AECategory( "Fog" )]
		public float FogDistance { get; set; } = 0.001f;

		[AECategory( "Fog" )]
		public float FogHeight { get; set; } = 0.05f;

		[AECategory( "Fog" )]
		public Color4 FogColor { get; set; } = new Color4(10,10,10,0);

		[AEIgnore]
		[JsonIgnore]
		public Vector3 SunPosition {
			get {
				var m = Matrix.RotationYawPitchRoll( MathUtil.DegreesToRadians(SunAzimuth), MathUtil.DegreesToRadians(SunAltitude), 0 );
				return m.Forward;
			}
		}
	}
}
