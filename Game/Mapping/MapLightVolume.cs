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
	public class MapLightVolume : MapNode, IEntityFactory 
	{
		[AECategory("Light Volume")]
		[AESlider(4, 256, 4, 1)]
		public int ResolutionX 
		{ 
			get; set; 
		} = 4;
		
		[AECategory("Light Volume")]
		[AESlider(4, 256, 4, 1)]
		public int ResolutionY 
		{ 
			get; set; 
		} = 4;
		
		[AECategory("Light Volume")]
		[AESlider(4, 256, 4, 1)]
		public int ResolutionZ 
		{ 
			get; set; 
		} = 4;


		[AECategory("Light Volume")]
		[AESlider(32, 2048, 64, 4)]
		public float Width 
		{ 
			get; set; 
		} = 32;
		
		[AECategory("Light Volume")]
		[AESlider(32, 2048, 64, 4)]
		public float Height 
		{ 
			get; set; 
		} = 32;
		
		[AECategory("Light Volume")]
		[AESlider(32, 2048, 64, 4)]
		public float Depth 
		{ 
			get; set; 
		} = 32;


		public MapLightVolume ()
		{
		}


		SFX2.LightVolume CreateLightVolumeComponent()
		{
			var light = new SFX2.LightVolume();

			light.ResolutionX	=	ResolutionX;
			light.ResolutionY	=	ResolutionY;
			light.ResolutionZ	=	ResolutionZ;

			light.Width			=	Width;
			light.Height		=	Height;
			light.Depth			=	Depth;

			return light;
		}


		public void Construct( Entity entity, IGameState gs )
		{
			entity.AddComponent( new Transform( Translation, Rotation, 1 ) );
			entity.AddComponent( CreateLightVolumeComponent() );
		}


		public override void SpawnNodeECS( IGameState gs )
		{
			ecsEntity		=	gs.Spawn( this );
			ecsEntity.Tag	=	this;
		}


		public override BoundingBox GetBoundingBox( IGameState gs )
		{
			return new BoundingBox( Width, Height, Depth );
		}
	}
}
