using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Fusion.Development;
using System.Drawing.Design;
using Fusion;
using Fusion.Core.Shell;
using IronStar.ECS;
using Fusion.Widgets.Advanced;

namespace IronStar.Mapping 
{
	public class MapOmniLight : MapNode 
	{
		[AECategory("Omni-light")]
		[AESlider(0, 100, 1, 0.125f)]
		public float OuterRadius { get; set; } = 5;
		
		[AECategory("Omni-light")]
		[AESlider(0, 8, 1, 0.125f)]
		public float TubeRadius { get; set; } = 0.125f;

		[AECategory("Omni-light")]
		[AESlider(0, 32, 1, 0.125f)]
		public float TubeLength { get; set; } = 0.0f;

		[AECategory("Omni-light")]
		public LightStyle LightStyle { get; set; } = LightStyle.Default;

		[AECategory("Omni-light")]
		public bool Ambient { get; set; } = false;

		[AECategory("Light Color")]
		[AEDisplayName("Light Color")]
		public Color LightColor { get; set; } = Color.White;

		[AECategory("Light Color")]
		[AEDisplayName("Intensity")]
		[AESlider(0, 5000, 10, 1)]
		public float LightIntensity { get; set; } = 100;


		public MapOmniLight ()
		{
		}


		public override void SpawnNodeECS( GameState gs )
		{
			ecsEntity = gs.Spawn();

			ecsEntity.AddComponent( new Transform( TranslateVector, RotateQuaternion, 1 ) );
			ecsEntity.AddComponent( CreateOmniLight() );
		}


		SFX2.OmniLight CreateOmniLight()
		{
			var light = new SFX2.OmniLight();

			light.TubeLength		=	TubeLength;
			light.TubeRadius		=	TubeRadius;
			light.OuterRadius		=	OuterRadius;
			light.LightColor		=	LightColor;
			light.LightIntensity	=	MathUtil.Log2( MathUtil.Clamp( LightIntensity, 1/64.0f, 1024 ) );

			return light;
		}


		public override MapNode DuplicateNode()
		{
			var newNode = (MapOmniLight)MemberwiseClone();
			newNode.NodeGuid = Guid.NewGuid();
			return newNode;
		}


		public override BoundingBox GetBoundingBox()
		{
			float sz = OuterRadius / (float)Math.Sqrt(3);
			return new BoundingBox( sz, sz, sz );
		}
	}
}
