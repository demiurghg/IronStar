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
	public class MapLightProbeBox : MapNode, IEntityFactory 
	{
		[Category("Light probe")]
		[AESlider(0,256,8,0.25f)]
		public float Width  { get; set; } = 16;

		[Category("Light probe")]
		[AESlider(0,256,8,0.25f)]
		public float Height { get; set; } = 16;

		[Category("Light probe")]
		[AESlider(0,256,8,0.25f)]
		public float Depth  { get; set; } = 16;

		[Category("Light probe")]
		[AEDisplayName("Transition Width")]
		[AESlider(0.25f,32,1,0.25f)]
		public float ShellWidth  { get; set; } = 8f;

		[Category("Light probe")]
		[AEDisplayName("Transition Height")]
		[AESlider(0.25f,32,1,0.25f)]
		public float ShellHeight  { get; set; } = 8f;

		[Category("Light probe")]
		[AEDisplayName("Transition Depth")]
		[AESlider(0.25f,32,1,0.25f)]
		public float ShellDepth  { get; set; } = 8f;


		[AECommand]
		public void MakeGlobal()
		{
			Translation	=	Vector3.Up * 512;
			Rotation	=	Quaternion.Identity;
			Width		=	2048;
			Height		=	2048;
			Depth		=	2048;
		}

		LightProbe	light;


		/// <summary>
		/// 
		/// </summary>
		public MapLightProbeBox ()
		{
		}


		public void Construct( Entity entity, IGameState gs )
		{
			var transform		=	new Transform( Translation, Rotation );

			var light			=	new SFX2.LightProbeBox(Name);
			light.Width			=	Width;
			light.Height		=	Height;
			light.Depth			=	Depth;
			light.ShellWidth	=	ShellWidth;
			light.ShellHeight	=	ShellHeight;
			light.ShellDepth	=	ShellDepth;

			entity.AddComponent( transform );
			entity.AddComponent( light );
		}


		public override void SpawnNodeECS( IGameState gs )
		{
			ecsEntity			=	gs.Spawn( this );
			ecsEntity.Tag		=	this;
		}


		private Matrix ComputeProbeMatrix ()
		{
			return Matrix.Scaling( Width/2.0f, Height/2.0f, Depth/2.0f ) * Transform;
		}



		public override BoundingBox GetBoundingBox( IGameState gs )
		{
			return new BoundingBox( Width, Height, Depth );
		}
	}
}
