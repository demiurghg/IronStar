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

namespace IronStar.Mapping {

	public enum LightMapSize {
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


	public class MapModel : MapNode {

		static readonly Scene EmptyScene = Scene.CreateEmptyScene();


		[AECategory( "Appearance" )]
		[Description( "Path to FBX scene" )]
		[AEFileName("scenes", "*.fbx", AEFileNameMode.NoExtension)]
		public string ScenePath { 
			get { return scenePath; }
			set {
				if (scenePath!=value) {
					scenePath = value;
					scenePathDirty = true;
				}
			}
		}

		string scenePath = "";
		bool scenePathDirty = true;

		[AECategory( "Appearance" )]
		[Description( "Entire model scale" )]
		public float Scale { get; set; } = 1;

		[AECategory( "Appearance" )]
		[AEDisplayName("Glow Color")]
		[Description( "Model glow color multiplier" )]
		public Color GlowColor { get; set; } = Color.Red;

		[AECategory( "Appearance" )]
		[AEDisplayName("Glow Intensity")]
		[AEValueRange(0,5000,100,1)]
		[Description( "Model glow color multiplier" )]
		public float GlowIntensity { get; set; } = 100;

		[AECategory( "Light Mapping" )]
		public LightMapSize LightMapSize { get; set; } = LightMapSize.LightMap32;

		[AECategory( "Light Mapping" )]
		public bool UseLightVolume { get; set; } = false;

		[AECategory( "Physics" )]
		public bool UseCollisionMesh { get; set; } = false;

		[AECategory( "Navigation" )]
		public bool Walkable { get; set; } = true;

		[AECategory( "Custom Node" )]
		public bool UseCustomNodes { get; set; } = false;

		[AECategory( "Custom Node" )]
		public string CustomVisibleNode { get; set; } = "";

		[AECategory( "Custom Node" )]
		public string CustomCollisionNode { get; set; } = "";
		


		Scene			scene		= null;
		RenderInstance[]	instances	= null;
		StaticMesh[]	collidables = null;
		Matrix[]		transforms	= null;
		DebugModel[]	debugModels = null;


		/// <summary>
		/// 
		/// </summary>
		public MapModel ()
		{
		}


		public override void SpawnNodeECS( GameState gs )
		{
			ecsEntity = gs.Spawn();

			ecsEntity.AddComponent( new ECS.Transform( TranslateVector, RotateQuaternion, Scale ) );
			ecsEntity.AddComponent( new StaticCollisionComponent() { Walkable = Walkable } );

			var rm		=	new SFX2.RenderModel( ScenePath, Matrix.Identity, Color.White, 1, SFX2.RMFlags.None );
			rm.cmPrefix	=	UseCollisionMesh ? "cm_" : "";
			var lmSize	=	UseLightVolume ? 0 : (int)LightMapSize;
			rm.SetupLightmap( lmSize, lmSize, NodeGuid );
			ecsEntity.AddComponent( rm );
		}


		public override MapNode DuplicateNode()
		{
			var newNode = (MapModel)MemberwiseClone();
			newNode.NodeGuid = Guid.NewGuid();

			return newNode;
		}


		public override BoundingBox GetBoundingBox()
		{
			#warning Need more smart bounding box for map models!
			return new BoundingBox( 8, 8, 8 );
		}
	}
}
