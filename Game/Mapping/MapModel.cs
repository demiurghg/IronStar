using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using IronStar.Core;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Fusion.Development;
using System.Drawing.Design;
using Fusion;
using IronStar.Physics;
using BEPUphysics.BroadPhaseEntries;
using BEPUTransform = BEPUutilities.AffineTransform;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics.Scenes;
using IronStar.ECS;
using IronStar.Physics2;

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



		public override void SpawnNode( GameWorld world )
		{
			if (string.IsNullOrWhiteSpace(ScenePath)) {
				return;
			}

			var rs		=	world.Game.RenderSystem;
			var pm		=	world.Physics;

			scene		=	world.Content.Load<Scene>( ScenePath, EmptyScene );

			transforms	=	new Matrix[ scene.Nodes.Count ];
			collidables	=	new StaticMesh[ scene.Nodes.Count ];
			instances	=	new RenderInstance[ scene.Nodes.Count ];
			debugModels	=	new DebugModel[ scene.Nodes.Count ];

			scene.ComputeAbsoluteTransforms( transforms );

			
			//
			//	add static collision mesh :
			//
			for ( int i=0; i<scene.Nodes.Count; i++ ) {

				var node = scene.Nodes[i];

				collidables[i] = null;

				if (node.MeshIndex<0) {
					continue;
				}

				if (UseCollisionMesh && !node.Name.StartsWith("cm_")) {
					continue;
				}

				if (UseCustomNodes && node.Name!=CustomCollisionNode) {
					continue;
				}
				
				var mesh		=	scene.Meshes[ node.MeshIndex ];
				var indices     =   mesh.GetIndices();
				var vertices    =   mesh.Vertices
									.Select( v1 => Vector3.TransformCoordinate( v1.Position, transforms[i] ) )
									.Select( v2 => MathConverter.Convert( v2 ) )
									.ToArray();

				var dvertices    =   mesh.Vertices
									.Select( v1 => v1.Position )
									.ToArray();

				if (world.EditorMode) {				
					var debugModel		=	new DebugModel( rs.RenderWorld.Debug, dvertices, indices );
					debugModel.World	=	transforms[ i ] * Matrix.Scaling( Scale ) * WorldMatrix;
					debugModels[i]		=	debugModel;
					rs.RenderWorld.Debug.DebugModels.Add( debugModel );
				}
				
				var staticMesh = new StaticMesh( vertices, indices );
				staticMesh.Sidedness = BEPUutilities.TriangleSidedness.Clockwise;

				var q = MathConverter.Convert( RotateQuaternion );
				var p = MathConverter.Convert( TranslateVector );
				var s = MathConverter.Convert( new Vector3( Scale, Scale, Scale ) );
				staticMesh.WorldTransform = new BEPUTransform( s, q, p );

				staticMesh.CollisionRules.Group	=	world.Physics.StaticGroup;
				staticMesh.Tag	=	this;

				collidables[i] =	staticMesh;
	
				pm.PhysSpace.Add( staticMesh );
			}


			//
			//	add visible mesh instance :
			//
			for ( int i=0; i<scene.Nodes.Count; i++ ) {

				var meshIndex = scene.Nodes[i].MeshIndex;

				var node = scene.Nodes[i];

				if (UseCollisionMesh && node.Name.StartsWith("cm_")) {
					continue;
				}

				if (UseCustomNodes && node.Name!=CustomVisibleNode) {
					continue;
				}
				
				if (meshIndex>=0) {
					instances[i] = new RenderInstance( rs, scene, scene.Meshes[meshIndex] );
					instances[i].World			=	transforms[ i ] * Matrix.Scaling( Scale ) * WorldMatrix;
					instances[i].Group			=	UseLightVolume ? InstanceGroup.Kinematic : InstanceGroup.Static;
					instances[i].LightMapSize	=	new Size2( (int)LightMapSize, (int)LightMapSize );
					instances[i].LightMapGuid	=	this.NodeGuid;
					rs.RenderWorld.Instances.Add( instances[i] );
				} else {
					instances[i] = null;
				}
			}
		}



		public override void SpawnNodeECS( GameState gs )
		{
			var e = gs.Spawn();
			e.AddComponent( new ECS.Static() );
			e.AddComponent( new ECS.Transform( TranslateVector, RotateQuaternion, Scale ) );
			e.AddComponent( new SFX2.RenderModel( ScenePath, Matrix.Identity, Color.White, 1, SFX2.RMFlags.None ) );
			e.AddComponent( new StaticCollisionModel( ScenePath, UseCollisionMesh ? "cm_" : null, WorldMatrix ) );
		}


		public override void ActivateNode()
		{
		}



		public override void UseNode()
		{
		}



		public override void DrawNode( GameWorld world, DebugRender dr, Color color, bool selected )
		{
			if (scenePathDirty) {
				KillNode(world);
				SpawnNode(world);
				scenePathDirty = false;
			}

			dr.DrawBasis( WorldMatrix, 1, 2 );
			
			if (scene==EmptyScene) {
				dr.DrawBox( TranslateVector, 2,2,2, Color.Red );
			}

			
			if (debugModels!=null) {
				foreach ( var debugModel in debugModels ) {
					if (debugModel!=null) {
						debugModel.Color	=	color;
					}
				}
			}
		}



		public override void KillNode( GameWorld world )
		{
			var rs = world.Game.RenderSystem;
			var pm = world.Physics;

			if (instances!=null) {
				foreach ( var instance in instances ) {
					if ( instance!=null ) {
						rs.RenderWorld.Instances.Remove( instance );
					}
				}
			}

			if (debugModels!=null) {
				foreach ( var debugModel in debugModels ) {
					if ( debugModel!=null ) {
						rs.RenderWorld.Debug.DebugModels.Remove( debugModel );
					}
				}
			}

			if (collidables!=null) {
				foreach ( var collidable in collidables ) {
					if ( collidable!=null ) {
						pm.PhysSpace.Remove( collidable );
					}
				}
			}

			instances	=	null;
			collidables	=	null;
			debugModels	=	null;
		}



		public override MapNode DuplicateNode( GameWorld world )
		{
			KillNode( world );

			var newNode = (MapModel)MemberwiseClone();
			newNode.NodeGuid = Guid.NewGuid();

			SpawnNode( world );

			return newNode;
		}


		public override BoundingBox GetBoundingBox()
		{
			#warning Need more smart bounding box for map models!
			return new BoundingBox( 8, 8, 8 );
		}
	}
}
