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
using BEPUphysics.BroadPhaseEntries;
using BEPUTransform = BEPUutilities.AffineTransform;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics.Scenes;
using IronStar.ECS;
using IronStar.ECSPhysics;
using Fusion.Widgets.Advanced;
using IronStar.Animation;
using IronStar.SFX2;

namespace IronStar.Mapping 
{
	public enum LightMapSize 
	{
		LightMap8	= 8,
		LightMap16	= 16,
		LightMap32	= 32,
		LightMap64	= 64,
		LightMap128	= 128,
		LightMap256	= 256,
		LightMap512	= 512,
		LightMap1K	= 1024,
		LightMap2K	= 2048,
	}


	public class MapModel : MapNode, IFactory 
	{
		static readonly Scene EmptyScene = Scene.CreateEmptyScene();

		[AECategory( "Appearance" )]
		[Description( "Path to FBX scene" )]
		[AEFileName("scenes", "*.fbx", AEFileNameMode.NoExtension)]
		public string ScenePath 
		{ 
			get { return scenePath; }
			set 
			{
				if (scenePath!=value) 
				{
					scenePath = value;
					bboxDirty = true;
				}
			}
		}

		string scenePath = "";
		bool bboxDirty = true;
		BoundingBox bbox;

		[AECategory( "Appearance" )]
		[Description( "Entire model scale" )]
		[AESlider(0.1f, 100.0f, 0.5f, 0.01f)]
		public float Scale { get; set; } = 1;

		[AECategory( "Appearance" )]
		[AEDisplayName("Glow Color")]
		[Description( "Model glow color multiplier" )]
		public Color GlowColor { get; set; } = Color.Red;

		[AECategory( "Appearance" )]
		[AEDisplayName("Skip Shadow")]
		public bool SkipShadow { get; set; }

		[AECategory( "Appearance" )]
		[AEDisplayName("Glow Intensity")]
		[AESlider(0,12,1,0.01f)]
		[Description( "Model glow color multiplier" )]
		public float GlowIntensity { get; set; } = 100;


		[AECategory( "Light Mapping" )]
		public bool UseLightMap { get; set; } = false;

		[AECategory( "Light Mapping" )]
		public bool UseLightmapProxy { get; set; } = false;

		[AECategory( "Light Mapping" )]
		public LightMapSize LightMapSize { get; set; } = LightMapSize.LightMap32;

		[AECategory( "Physics" )]
		public bool UseCollisionProxy { get; set; } = false;

		[AECategory( "Physics" )]
		public bool Collidable { get; set; } = true;

		[AECategory( "Navigation" )]
		public bool Walkable { get; set; } = true;


		//[AECategory( "Obsolete!" )]	[Obsolete]
		//public bool UseCustomNodes { get; set; } = false;

		//[AECategory( "Obsolete!" )]	[Obsolete]
		//public bool UseLightVolume 
		//{ 
		//	get { return !UseLightMap; } 
		//	set { UseLightMap=!value; } 
		//}

		//[AECategory( "Obsolete!" )]	[Obsolete]
		//public string CustomVisibleNode { get; set; } = "";

		//[AECategory( "Obsolete!" )]	[Obsolete]
		//public bool Animated { get; set; } = false;

		//[AECategory( "Obsolete!" )]	[Obsolete]
		//public string CustomCollisionNode { get; set; } = "";

		//[AECategory( "Physics" )]
		//public bool UseCollisionMesh { 
		//	get { return UseCollisionProxy; }
		//	set { UseCollisionProxy = value; }
		//}



		public MapModel ()
		{
		}


		public void Construct( Entity entity, IGameState gs )
		{
			var t		=	new Transform( Translation, Rotation, Scale );
			var rm		=	new RenderModel( ScenePath, Matrix.Identity, GlowColor, GlowIntensity, SFX2.RMFlags.None );

			var flags	=	RMFlags.Static;

			if (SkipShadow)			flags	|=	RMFlags.NoShadow;
			if (UseLightMap)		flags	|=	RMFlags.UseLightmap;
			if (UseCollisionProxy)	flags	|=	RMFlags.UseCollisionProxy;
			if (UseLightmapProxy)	flags	|=	RMFlags.UseLightmapProxy;
			
			var lmSize	=	UseLightMap ? (int)LightMapSize : 0;
			rm.LightmapSize	=	new Size2( lmSize, lmSize );
			rm.LightmapName	=	Name;
			rm.rmFlags		=	flags;

			entity.AddComponent( t );
			entity.AddComponent( rm );			

			entity.AddComponent( new StaticCollisionComponent { Walkable = Walkable, Collidable = Collidable } );
		}


		public override void SpawnNodeECS( IGameState gs )
		{
			ecsEntity		=	gs.Spawn( this );
			ecsEntity.Tag	=	this;
		}


		public override BoundingBox GetBoundingBox( IGameState gs )
		{
			if (bboxDirty)
			{
				var scene = gs.Content.Load( ScenePath, (Scene)null );
				bboxDirty = false;

				if (scene==null)
				{
					bbox = new BoundingBox( 8, 8, 8 );
				}
				else
				{
					bbox = scene.ComputeBoundingBoxApprox( Matrix.Scaling(Scale) );
				}
			}

			// #TODO #MAP -- Need more smart bounding box for map models!
			return bbox;
		}
	}
}
