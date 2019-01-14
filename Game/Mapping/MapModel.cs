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

namespace IronStar.Mapping {

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
		


		Scene			scene		= null;
		BoundingBox[]	bboxes		= null;
		MeshInstance[]	instances	= null;
		StaticMesh[]	collidables = null;
		Matrix[]		transforms	= null;


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
			instances	=	new MeshInstance[ scene.Nodes.Count ];

			scene.ComputeAbsoluteTransforms( transforms );

			bboxes		=	scene.Meshes.Select( m => m.ComputeBoundingBox() ).ToArray();


			//
			//	add static collision mesh :
			//
			for ( int i=0; i<scene.Nodes.Count; i++ ) {

				var node = scene.Nodes[i];

				collidables[i] = null;

				if (node.MeshIndex<0) {
					continue;
				}

				var mesh		=	scene.Meshes[ node.MeshIndex ];
				var indices     =   mesh.GetIndices();
				var vertices    =   mesh.Vertices
									.Select( v1 => Vector3.TransformCoordinate( v1.Position, transforms[i] ) )
									.Select( v2 => MathConverter.Convert( v2 ) )
									.ToArray();

				var staticMesh = new StaticMesh( vertices, indices );
				staticMesh.Sidedness = BEPUutilities.TriangleSidedness.Clockwise;

				var q = MathConverter.Convert( RotateQuaternion );
				var p = MathConverter.Convert( TranslateVector );
				var s = MathConverter.Convert( new Vector3( Scale, Scale, Scale ) );
				staticMesh.WorldTransform = new BEPUTransform( s, q, p );

				staticMesh.CollisionRules.Group	=	world.Physics.StaticGroup;

				collidables[i] =	staticMesh;
	
				pm.PhysSpace.Add( staticMesh );
			}


			//
			//	add visible mesh instance :
			//
			for ( int i=0; i<scene.Nodes.Count; i++ ) {
				var meshIndex = scene.Nodes[i].MeshIndex;
				
				if (meshIndex>=0) {
					instances[i] = new MeshInstance( rs, scene, scene.Meshes[meshIndex] );
					instances[i].World	= transforms[ i ] * WorldMatrix;
					instances[i].Group	= InstanceGroup.Static;
					rs.RenderWorld.Instances.Add( instances[i] );
				} else {
					instances[i] = null;
				}
			}
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


			if (scene!=null && selected) {
				for ( int i=0; i<scene.Nodes.Count; i++ ) {

					var node = scene.Nodes[i];

					if (node.MeshIndex<0) {
						continue;
					}

					dr.DrawBox( bboxes[node.MeshIndex], transforms[ i ] * WorldMatrix, color, 1 ); 
				}
			}
		}



		public override void ResetNode( GameWorld world )
		{
			if (scene==null) {
				return;
			}

			for (int i=0; i<scene.Nodes.Count; i++) {

				var collidable = collidables[i];

				if (collidable!=null) {
					var q = MathConverter.Convert( RotateQuaternion );
					var p = MathConverter.Convert( TranslateVector );

					collidable.WorldTransform = new BEPUTransform( q, p );
				}

				var instance = instances[i];

				if (instance!=null) {
					instances[i].World = transforms[ i ] * WorldMatrix;
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

			if (collidables!=null) {
				foreach ( var collidable in collidables ) {
					if ( collidable!=null ) {
						pm.PhysSpace.Remove( collidable );
					}
				}
			}

			instances	=	null;
			collidables	=	null;
		}



		public override MapNode DuplicateNode()
		{
			var newNode = (MapModel)MemberwiseClone();

			instances	=	null;
			collidables	=	null;

			return newNode;
		}


		public override BoundingBox GetBoundingBox()
		{
			#warning Need more smart bounding box for map models!
			return new BoundingBox( 8, 8, 8 );
		}
	}
}
