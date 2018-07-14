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
		/// Entity ID is written to stream by GameWorld during replication.
		/// </summary>
		public readonly uint ID;

		/// <summary>
		/// Class ID.
		/// Class ID is written to stream by GameWorld during replication.
		/// </summary>
		public readonly short ClassID;

		/// <summary>
		/// Entity's target name.
		/// Server-side only
		/// </summary>
		public string TargetName = null;

		/// <summary>
		/// Entity misc state flags
		/// </summary>
		public EntityState EntityState;

		/// <summary>
		/// Players guid. Zero if no player.
		/// Server-side only
		/// </summary>
		public Guid UserGuid;

		/// <summary>
		///	Gets parent's ID. 
		///	Zero value means no parent.
		/// </summary>
		public uint ParentID;

		/// <summary>
		/// Teleportation counter.
		/// Used to prevent interpolation in discreete movement.
		/// </summary>
		public byte TeleportCount;

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
		/// </summary>
		public Vector3 LinearVelocity;

		/// <summary>
		/// Angular object velocity
		/// </summary>
		public Vector3 AngularVelocity;


		protected readonly GameWorld World;

		/// <summary>
		/// Used to replicate entity on client side.
		/// </summary>
		/// <param name="id"></param>
		public Entity ( uint id, short clsid, GameWorld world, EntityFactory factory )
		{
			this.World		=	world;
			this.ID			=	id;
			this.ClassID	=	clsid;
			RotationOld		=	Quaternion.Identity;
			Rotation		=	Quaternion.Identity;
			TeleportCount	=	0xFF;
		}

 
		/// <summary>
		/// Writes entity state to stream using binary writer.
		/// </summary>
		/// <param name="writer"></param>
		public virtual void Write ( BinaryWriter writer )
		{
			writer.Write( UserGuid.ToByteArray() );
			writer.Write( ParentID );
			writer.Write( (int)EntityState );

			writer.Write( TeleportCount );

			writer.Write( Position );
			writer.Write( Rotation );
			writer.Write( LinearVelocity );
			writer.Write( AngularVelocity );

			writer.Write( Sfx );
			writer.Write( Model );
			writer.Write( Model2 );
			writer.Write( ModelFpv );
		}


		/// <summary>
		/// Reads entity states from stream using binary reader.
		/// </summary>
		/// <param name="writer"></param>
		public virtual void Read ( BinaryReader reader, float lerpFactor )
		{
			//	keep old teleport counter :
			var oldTeleport	=	TeleportCount;

			//	set old values :
			PositionOld		=	LerpPosition( lerpFactor );
			RotationOld		=	LerpRotation( lerpFactor );

			//	read state :
			UserGuid		=	new Guid( reader.ReadBytes(16) );
			ParentID		=	reader.ReadUInt32();
			EntityState		=	(EntityState)reader.ReadInt32();

			TeleportCount	=	reader.ReadByte();

			Position		=	reader.Read<Vector3>();	
			Rotation		=	reader.Read<Quaternion>();	
			LinearVelocity	=	reader.Read<Vector3>();
			AngularVelocity	=	reader.Read<Vector3>();	

			Sfx				=	reader.ReadInt16();
			Model			=	reader.ReadInt16();
			Model2			=	reader.ReadInt16();
			ModelFpv		=	reader.ReadInt16();

			//	entity teleported - reset position and rotation :
			if (oldTeleport!=TeleportCount) {
				PositionOld	=	Position;
				RotationOld	=	Rotation;
			}
		}


		/// <summary>
		/// Update entity state.
		/// </summary>
		/// <param name="gameTime"></param>
		public virtual void Update ( GameTime gameTime )
		{
		}


		/// <summary>
		/// Called when game decided to reload entire content.
		/// </summary>
		public virtual void Reload ()
		{
			MakePresentationDirty();
		}

  
		/// <summary>
		/// Draw entity.
		/// </summary>
		/// <param name="gameTime"></param>
		public virtual void Draw ( GameTime	gameTime )
		{
		}


		/// <summary>
		/// Called when entity has been removed from the game world.
		/// </summary>
		public virtual void Kill ()
		{
			KillPresentation();
		}


		/// <summary>
		/// Inflicts damage to current entity.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="damageType"></param>
		/// <param name="damage"></param>
		/// <param name="kickImpulse"></param>
		/// <param name="kickPoint"></param>
		public virtual void Damage ( Entity attacker, int damage, DamageType damageType, Vector3 kickImpulse, Vector3 kickPoint )
		{
		}


		/// <summary>
		/// Called when one entity starts to touch another
		/// </summary>
		/// <param name="other"></param>
		/// <param name="touchPoint"></param>
		public virtual void Touch ( Entity other, Vector3 touchPoint )
		{
			
		}


		/// <summary>
		/// Activate entity in trigger chain.
		/// </summary>
		/// <param name="activator"></param>
		public virtual void Activate ( Entity activator )
		{
		}


		/// <summary>
		/// Attempt to use entity.
		/// </summary>
		/// <param name="user"></param>
		public virtual void Use ( Entity user )
		{
		}


		/// <summary>
		/// Indicated, that given entity could be used by player.
		/// Default FALSE.
		/// </summary>
		public virtual bool AllowUse { get { return false; } }


		/// <summary>
		/// Handle user control.
		/// </summary>
		/// <param name="userCommand"></param>
		public virtual void UserControl ( UserCommand userCommand )
		{
		}


		/// <summary>
		/// Gets hint for player.
		/// Could be null;
		/// </summary>
		public virtual string UserHint { get { return null; } }
		

  		/// <summary>
		/// Immediatly put entity in given position without interpolation.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="orient"></param>
		public virtual void Teleport ( Vector3 position, Quaternion orient )
		{
			TeleportCount++;
			TeleportCount &= 0x7F;

			Position		=	position;
			Rotation		=	orient;
			PositionOld		=	position;
			RotationOld		=	orient;
		}

  

		[Obsolete]
		public virtual void Move ( Vector3 position, Quaternion orient, Vector3 velocity )
		{
			throw new NotImplementedException("Entity.Move is obsolete method");
		}
  

		/// <summary>
		/// Compute entity world matrix
		/// </summary>
		/// <returns></returns>
		public Matrix GetWorldMatrix (float lerpFactor)
		{
			return Matrix.RotationQuaternion( LerpRotation(lerpFactor) ) 
					* Matrix.Translation( LerpPosition(lerpFactor) );
		}

  
		/// <summary>
		/// Lerps entity rotation.
		/// </summary>
		/// <param name="lerpFactor"></param>
		/// <returns></returns>
		public Quaternion LerpRotation ( float lerpFactor )
		{
			return Quaternion.Slerp( RotationOld, Rotation, MathUtil.Clamp(lerpFactor,0,1f) );
		}

   
		/// <summary>
		/// Lerps entity position 
		/// </summary>
		/// <param name="lerpFactor"></param>
		/// <returns></returns>
		public Vector3 LerpPosition ( float lerpFactor )
		{
			return Vector3.Lerp( PositionOld, Position, MathUtil.Clamp(lerpFactor,0,2f) );
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Presentation stuff
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Visible model #1
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
		/// Visible model #2
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
		/// First person view model #2
		/// </summary>
		public short ModelFpv {
			get { return modelFpv; }
			set { 
				modelFpvDirty |= (modelFpv != value); 
				modelFpv = value; 
			}
		}
		private short modelFpv = -1;
		private bool modelFpvDirty = true;

		/// <summary>
		/// Visible persistent special effect
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
		public ModelInstance ModelFpvInstance { get; private set; }

	
		public void UpdatePresentation ( FXPlayback fxPlayback, ModelManager modelManager, GameCamera gameCamera )
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
					ModelInstance	=	modelManager.AddModel( this, model, false );
				}
			}

			if (model2Dirty) {
				model2Dirty = false;

				ModelInstance2?.Kill();
				ModelInstance2	=	null;

				if (model2>0) {
					ModelInstance2	=	modelManager.AddModel( this, model2, false );
				}
			}

			if (modelFpvDirty) {
				modelFpvDirty = false;

				ModelFpvInstance?.Kill();
				ModelFpvInstance	=	null;

				if (modelFpv>0) {
					ModelFpvInstance	=	modelManager.AddModel( this, modelFpv, true );
				}
			}
		}



		public void MakePresentationDirty ()
		{
			sfxDirty		=	true;
			modelDirty		=	true;
			model2Dirty		=	true;
			modelFpvDirty	=	true;
		}



		public void KillPresentation ()
		{
			FXInstance?.Kill();
			FXInstance	= null;

			ModelInstance?.Kill();
			ModelInstance	=	null;

			ModelInstance2?.Kill();
			ModelInstance2	=	null;

			ModelFpvInstance?.Kill();
			ModelFpvInstance	=	null;
		}
	}
}
