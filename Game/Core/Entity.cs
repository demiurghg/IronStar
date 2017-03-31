using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Utils;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using System.IO;
using IronStar.SFX;
using Fusion.Engine.Graphics;
using Fusion.Core.Content;
using Fusion.Engine.Common;
using IronStar.Views;

namespace IronStar.Core {
	public class Entity {


		/// <summary>
		/// Entity ID
		/// </summary>
		public readonly uint ID;

		/// <summary>
		/// Entity's target name.
		/// </summary>
		public readonly string TargetName = null;

		/// <summary>
		/// Players guid. Zero if no player.
		/// </summary>
		public Guid UserGuid;// { get; private set; }

		/// <summary>
		/// Gets entity state
		/// </summary>
		public EntityState State;

		/// <summary>
		///	Gets parent's ID. 
		///	Zero value means no parent.
		/// </summary>
		public uint ParentID { get; private set; }

		/// <summary>
		/// Classname atoms.
		/// </summary>
		public short ClassID;

		/// <summary>
		/// Teleportation counter.
		/// Used to prevent interpolation in discreete movement.
		/// </summary>
		public byte TeleportCount;

		/// <summary>
		/// Point-of-view vertical offset
		/// </summary>
		public float PovHeight = 0;

		/// <summary>
		/// Entity position
		/// </summary>
		public Vector3 Position;

		/// <summary>
		/// Entity position
		/// </summary>
		public Vector3 PositionOld;

		/// <summary>
		/// Entity's angle
		/// </summary>
		public Quaternion Rotation;

		/// <summary>
		/// Entity's angle
		/// </summary>
		public Quaternion RotationOld;

		/// <summary>
		/// Linear object velocity.
		/// Linear velocity XYZ also means decal bounds.
		/// </summary>
		public Vector3 LinearVelocity;

		/// <summary>
		/// Angular object velocity
		/// </summary>
		public Vector3 AngularVelocity;

		/// <summary>
		/// Animation frame
		/// </summary>
		public float AnimFrame;

		/// <summary>
		/// Animation frame
		/// </summary>
		public float AnimFrame2;


		/// <summary>
		/// Visible model
		/// </summary>
		public short Model {
			get { return model; }
			set { 
				modelDirty |= (model != value); 
				model = value; 
			}
		}
		private short model = -1;
		private bool modelDirty = true;

		/// <summary>
		/// Visible model
		/// </summary>
		public short Model2 {
			get { return model2; }
			set { 
				model2Dirty |= (model2 != value); 
				model2 = value; 
			}
		}
		private short model2 = -1;
		private bool model2Dirty = true;

		/// <summary>
		/// Visible special effect
		/// </summary>
		public short Sfx {
			get { return sfx; }
			set { 
				sfxDirty |= (sfx != value); 
				sfx = value; 
			}
		}
		private short sfx = -1;
		private bool sfxDirty = true;



		public FXInstance FXInstance { get; private set; }
		public ModelInstance ModelInstance { get; private set; }
		public ModelInstance ModelInstance2 { get; private set; }


		/// <summary>
		/// Used to replicate entity on client side.
		/// </summary>
		/// <param name="id"></param>
		public Entity ( uint id )
		{
			ID	=	id;
			RotationOld		=	Quaternion.Identity;
			Rotation		=	Quaternion.Identity;
			TeleportCount	=	0xFF;
		}


		/// <summary>
		/// Used to spawn entity on server side.
		/// </summary>
		/// <param name="id"></param>
		public Entity ( uint id, short classId, uint parentId, Vector3 position, Quaternion rotation, string targetName )
		{
			ClassID		=	classId;
			this.ID		=	id;

			this.TargetName	=	targetName;

			TeleportCount	=	0;

			RotationOld		=	Quaternion.Identity;
			PositionOld		=	Vector3.Zero;

			UserGuid		=	new Guid();
			ParentID		=	parentId;

			Rotation		=	rotation;
			Position		=	position;
			PositionOld		=	position;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxPlayback"></param>
		public void UpdateRenderState ( FXPlayback fxPlayback, ModelManager modelManager, GameCamera gameCamera )
		{
			if (sfxDirty) {
				sfxDirty = false;

				FXInstance?.Kill();
				FXInstance = null;

				if (sfx>0) {
					var fxe = new FXEvent( sfx, ID, Position, LinearVelocity, Rotation );
					FXInstance = fxPlayback.RunFX( fxe, true );
				}
			}

			if (modelDirty) {
				modelDirty = false;

				ModelInstance?.Kill();
				ModelInstance	=	null;

				if (model>0) {
					ModelInstance	=	modelManager.AddModel( model, this );
				}
			}

			if (model2Dirty) {
				model2Dirty = false;

				ModelInstance2?.Kill();
				ModelInstance2	=	null;

				if (model2>0) {
					ModelInstance2	=	modelManager.AddModel( model2, this );
				}
			}
		}



		public void MakeRenderStateDirty ()
		{
			sfxDirty	=	true;
			modelDirty	=	true;
			model2Dirty	=	true;
		}



		public void DestroyRenderState ( FXPlayback fxPlayback )
		{
			FXInstance?.Kill();
			FXInstance = null;

			ModelInstance?.Kill();
			ModelInstance	=	null;

			ModelInstance2?.Kill();
			ModelInstance2	=	null;
		}




		/// <summary>
		/// Immediatly put entity in given position without interpolation :
		/// </summary>
		/// <param name="position"></param>
		/// <param name="orient"></param>
		void SetPose ( Vector3 position, Quaternion orient )
		{
			TeleportCount++;
			TeleportCount &= 0x7F;

			Position		=	position;
			Rotation		=	orient;
			PositionOld		=	position;
			RotationOld		=	orient;
		}



		/// <summary>
		/// Moves entity to given position with interpolation :
		/// </summary>
		/// <param name="position"></param>
		/// <param name="orient"></param>
		public void Move ( Vector3 position, Quaternion orient, Vector3 velocity )
		{
			Position		=	position;
			Rotation		=	orient;
			LinearVelocity	=	velocity;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public void Write ( BinaryWriter writer )
		{
			writer.Write( UserGuid.ToByteArray() );

			writer.Write( ParentID );
			writer.Write( (int)State );
			writer.Write( ClassID );

			writer.Write( TeleportCount );

			writer.Write( Position );
			writer.Write( Rotation );
			writer.Write( LinearVelocity );
			writer.Write( AngularVelocity );
			writer.Write( PovHeight );

			writer.Write( AnimFrame );
			writer.Write( Model );
			writer.Write( Model2 );
			writer.Write( Sfx );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public void Read ( BinaryReader reader, float lerpFactor )
		{
			//	keep old teleport counter :
			var oldTeleport	=	TeleportCount;

			//	set old values :
			PositionOld		=	LerpPosition( lerpFactor );
			RotationOld		=	LerpRotation( lerpFactor );

			//	read state :
			UserGuid		=	new Guid( reader.ReadBytes(16) );
								
			ParentID		=	reader.ReadUInt32();
			State			=	(EntityState)reader.ReadInt32();
			ClassID			=	reader.ReadInt16();

			TeleportCount	=	reader.ReadByte();

			Position		=	reader.Read<Vector3>();	
			Rotation		=	reader.Read<Quaternion>();	
			LinearVelocity	=	reader.Read<Vector3>();
			AngularVelocity	=	reader.Read<Vector3>();	
			PovHeight		=	reader.Read<float>();

			AnimFrame		=	reader.ReadSingle();
			Model			=	reader.ReadInt16();
			Model2			=	reader.ReadInt16();
			Sfx				=	reader.ReadInt16();

			//	entity teleported - reset position and rotation :
			if (oldTeleport!=TeleportCount) {
				PositionOld	=	Position;
				RotationOld	=	Rotation;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Matrix GetWorldMatrix (float lerpFactor)
		{
			return Matrix.RotationQuaternion( LerpRotation(lerpFactor) ) 
					* Matrix.Translation( LerpPosition(lerpFactor) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="lerpFactor"></param>
		/// <returns></returns>
		public Quaternion LerpRotation ( float lerpFactor )
		{
			//return Position;
			return Quaternion.Slerp( RotationOld, Rotation, MathUtil.Clamp(lerpFactor,0,1f) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="lerpFactor"></param>
		/// <returns></returns>
		public Vector3 LerpPosition ( float lerpFactor )
		{
			//return Position;
			return Vector3.Lerp( PositionOld, Position, MathUtil.Clamp(lerpFactor,0,2f) );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="lerpFactor"></param>
		/// <returns></returns>
		public Vector3 GetPOV ( float lerpFactor )
		{
			return	LerpPosition( lerpFactor ) + Vector3.Up * PovHeight;
		}


		public Vector3 PointOfView {
			get {
				return	Position + Vector3.Up * PovHeight;
			}
		}


		/*-----------------------------------------------------------------------------------------------
		 * 
		 *	Entity controllers :
		 * 
		-----------------------------------------------------------------------------------------------*/
		EntityController controller;

		public EntityController	Controller {
			get {
				return controller;
			}
			set {
				controller = value;
			}
		}



		/// <summary>
		/// Iterates all controllers
		/// </summary>
		/// <param name="action"></param>
		public void ForeachController ( Action<EntityController> action )
		{
			if (controller!=null) {
				action(controller);
			}
		}
	}
}
