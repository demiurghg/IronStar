﻿using System;
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
	public class MapLightVolume : MapNode 
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


		public override void SpawnNodeECS( GameState gs )
		{
			ecsEntity		=	gs.Spawn();
			ecsEntity.Tag	=	this;

			ecsEntity.AddComponent( new KinematicState( Translation, Rotation, 1 ) );
			ecsEntity.AddComponent( CreateOmniLight() );
		}


		SFX2.LightVolume CreateOmniLight()
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


		public override BoundingBox GetBoundingBox( GameState gs )
		{
			return new BoundingBox( Width, Height, Depth );
		}
	}
}
