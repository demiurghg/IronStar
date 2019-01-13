using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.Core;
using Fusion.Engine.Graphics;
using Fusion.Core.Shell;
using Newtonsoft.Json;

namespace IronStar.Mapping {
	public class MapEnvironment {

		[AECategory( "Physics" )]
		public float Gravity { get; set; } = 48;

		[AECategory( "Sky" )]
		[AEValueRange(2,8,0.25f,0.125f)]
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
		[AEValueRange(0,90,15,1)]
		public float SunAltitude { get; set; } = 45;

		[AECategory( "Sky" )]
		[AEValueRange(-180,180,15,1)]
		public float SunAzimuth { get; set; } = 45;

		[AECategory( "Sky" )]
		[AEValueRange(0,500,10,1)]
		public float SunIntensity { get; set; } = 100;

		[AECategory( "Fog" )]
		public float FogDistance { get; set; } = 0.001f;

		[AECategory( "Fog" )]
		public float FogHeight { get; set; } = 0.05f;

		[AECategory( "Fog" )]
		public Color4 FogColor { get; set; } = new Color4(10,10,10,0);


		[AECategory("AI")]
		[Description("Character height")]
		public float CharacterHeight { get; set; } = 2;

		[AECategory("AI")]
		[Description("Character size")]
		public float CharacterSize { get; set; } = 1.2f;

		[AECategory("AI")]
		[Description("Walkable slope angle")]
		public float WalkableSlope { get; set; } = 45f;

		[AECategory("AI")]
		[Description("Stair step or climbable height")]
		public float StepHeight { get; set; } = 0.5f;

		[AECategory("AI")]
		[Description("Cell height for Recast voxelization")]
		public float RecastCellHeight { get; set; } = 0.25f;

		[AECategory("AI")]
		[Description("Cell width for Recast voxelization")]
		public float RecastCellSize { get; set; } = 0.25f;

		[AECategory("GI")]
		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public BoundingBox IrradianceVolume { get; set; } = new BoundingBox(128,64,128);

		[AECategory("GI")]
		public Color4 AmbientLevel { get; set; } = Color4.Zero;

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
