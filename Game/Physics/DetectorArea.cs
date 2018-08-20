using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;
using BEPUCharacterController = BEPUphysics.Character.CharacterController;
using Fusion.Core.IniParser.Model;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.DataStructures;


namespace IronStar.Physics {
	public class DetectorArea {

		Vector3 position;
		Quaternion orientation;

		readonly Space space;
		readonly GameWorld world;
		readonly DetectorVolume detectorVolume;

		readonly float width;
		readonly float height;
		readonly float depth;

		public event EventHandler<EntityEventArgs> Touch;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="world"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="depth"></param>
		/// <param name="mass"></param>
		public DetectorArea ( GameWorld world, float width, float height, float depth )
		{
			this.world	=	world;
			this.space	=	world.Physics.PhysSpace;

			this.width	=	width;
			this.height	=	height;
			this.depth	=	depth;
			var p		=	Position;
			var q		=	Orientation;

			detectorVolume	=	new DetectorVolume( CreateCube( p, q, width, height, depth ) );

			world.Physics.PhysSpace.Add( detectorVolume );

			detectorVolume.EntityBeganTouching +=DetectorVolume_EntityBeganTouching;
		}




		private void DetectorVolume_EntityBeganTouching( DetectorVolume volume, BEPUphysics.Entities.Entity toucher )
		{
			var ent = toucher.Tag as Entity;

			if (ent!=null) {
				Touch?.Invoke( this, new EntityEventArgs(ent) );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="depth"></param>
		/// <returns></returns>
		TriangleMesh CreateCube ( Vector3 position, Quaternion rotation, float width, float height, float depth )
		{
			float x = width/2;
			float y = height/2;
			float z = depth/2;

			var rm	=	Matrix.RotationQuaternion( rotation );
			var tm	=	Matrix.Translation( position );
			var m	=	rm * tm;

			var verts = new[] {
				Vector3.TransformCoordinate( new Vector3( -x, -y,  z ), m ),
				Vector3.TransformCoordinate( new Vector3(  x, -y,  z ), m ),
				Vector3.TransformCoordinate( new Vector3( -x,  y,  z ), m ),
				Vector3.TransformCoordinate( new Vector3(  x,  y,  z ), m ),
				Vector3.TransformCoordinate( new Vector3( -x,  y, -z ), m ),
				Vector3.TransformCoordinate( new Vector3(  x,  y, -z ), m ),
				Vector3.TransformCoordinate( new Vector3( -x, -y, -z ), m ),
				Vector3.TransformCoordinate( new Vector3(  x, -y, -z ), m ),
			};

			var verts2 = verts.Select( v => MathConverter.Convert( v ) ).ToArray();

			var inds = new [] {
				0, 1, 2,	2, 1, 3,
				2, 3, 4,	4, 3, 5,
				4, 5, 6,	6, 5, 7,
				6, 7, 0,	0, 7, 1,
				1, 7, 3,	3, 7, 5,
				6, 0, 4,	4, 0, 2,
			};

			return new TriangleMesh( new StaticMeshData( verts2, inds ) );
		}


		/// <summary>
		/// 
		/// </summary>
		public void Destroy ()
		{
			space.Remove( detectorVolume );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"></param>
		/// <param name="orient"></param>
		public void Teleport ( Vector3 position, Quaternion orient )
		{
			this.position		=	position;
			this.orientation	=	orient;

			detectorVolume.TriangleMesh	=	CreateCube( position, orientation, width, height, depth );
		}



		public Vector3 Position {
			get {
				return position;
			}
		}


		public Quaternion Orientation {
			get {
				return orientation;
			}
		}
	}
}
