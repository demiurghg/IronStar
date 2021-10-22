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
	public class MapOmniLight : MapNode, IEntityFactory 
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
		[AESlider(0, 16, 1, 1f/16f)]
		public float LightIntensity { get; set; } = 5;


		public MapOmniLight ()
		{
		}


		public override void SpawnNodeECS( IGameState gs )
		{
			ecsEntity		=	gs.Spawn( this );
			ecsEntity.Tag	=	this;
		}


		public void Construct( Entity entity, IGameState gs )
		{
			entity.AddComponent( new Transform( Translation, Rotation, 1 ) );
			entity.AddComponent( CreateOmniLight() );
		}


		SFX2.OmniLight CreateOmniLight()
		{
			var light = new SFX2.OmniLight();

			light.TubeLength		=	TubeLength;
			light.TubeRadius		=	TubeRadius;
			light.OuterRadius		=	OuterRadius;
			light.LightColor		=	LightColor;
			light.LightIntensity	=	LightIntensity;

			return light;
		}


		public override BoundingBox GetBoundingBox( IGameState gs )
		{
			float sz = OuterRadius / (float)Math.Sqrt(3);
			return new BoundingBox( sz, sz, sz );
		}
	}
}
