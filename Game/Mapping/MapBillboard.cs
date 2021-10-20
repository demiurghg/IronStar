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
using IronStar.ECSGraphics;

namespace IronStar.Mapping 
{
	public class MapBillboard : MapNode 
	{
		public ParticleFX Effect { get; set; } = ParticleFX.SoftLit;

		public Color Color { get; set; } = Color.White;

		[AESlider(0, 1, 1f/8f, 1f/255f)]
		public float Alpha { get; set; } = 1.0f;

		[AESlider(0, 1, 1f/8f, 1f/255f)]
		public float Exposure { get; set; } = 0.0f;

		[AESlider(0, 1, 1f/8f, 1f/255f)]
		public float Roughness { get; set; } = 0.5f;

		[AESlider(0, 1, 1f/8f, 1f/255f)]
		public float Metallic { get; set; } = 0.0f;

		[AESlider(0, 1, 1f/8f, 1f/255f)]
		public float Scattering { get; set; } = 0.0f;

		[AESlider(-4, 8, 1f, 1f/16f)]
		public float IntensityEV  { get; set; } = 0.0f;

		[AESlider(0, 128, 1f, 0.01f)]
		public float Size { get; set; } = 8.0f;

		[AESlider(-360, 360, 15f, 1f)]
		public float Angle { get; set; } = 0.0f;

		[Editor( typeof( SpriteFileLocationEditor ), typeof( UITypeEditor ) )]
		[AEAtlasImage("sprites\\particles")]
		public string ImageName { get; set; } = "";

		public MapBillboard ()
		{
		}

		public override void SpawnNodeECS( IGameState gs )
		{
			ecsEntity		=	gs.Spawn();
			ecsEntity.Tag	=	this;

			ecsEntity.AddComponent( new Transform( Translation, Rotation, 1 ) );
			ecsEntity.AddComponent( CreateBillboard() );
		}


		BillboardComponent CreateBillboard()
		{
			var b = new BillboardComponent();

			b.Effect		=	Effect			;
			b.Color			=	Color			;
			b.Alpha			=	Alpha			;
			b.Exposure		=	Exposure		;
			b.Roughness		=	Roughness		;
			b.Metallic		=	Metallic		;
			b.Scattering	=	Scattering		;
			b.IntensityEV	=	IntensityEV		;
			b.Size			=	Size			;
			b.Rotation		=	Angle			;
			b.ImageName		=	ImageName		;

			return b;
		}


		public override BoundingBox GetBoundingBox( IGameState gs )
		{
			float sz = Size;
			return new BoundingBox( sz, sz, sz );
		}
	}
}
