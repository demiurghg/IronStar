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
	public class Entity2 {

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
		public readonly string TargetName = null;

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


		/// <summary>
		/// Used to replicate entity on client side.
		/// </summary>
		/// <param name="id"></param>
		public Entity2 ( uint id )
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
		public Entity2 ( uint id, short classId, uint parentId, Vector3 position, Quaternion rotation, string targetName )
		{
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
		/// Indicates whether entity 
		/// should be removed from the game world
		/// </summary>
		public virtual bool IsDead {
			get;
		}

 
		/// <summary>
		/// Writes entity state to stream using binary writer.
		/// </summary>
		/// <param name="writer"></param>
		public virtual void Write ( BinaryWriter writer )
		{
			writer.Write( ParentID );
			writer.Write( UserGuid.ToByteArray() );

			writer.Write( TeleportCount );

			writer.Write( Position );
			writer.Write( Rotation );
			writer.Write( LinearVelocity );
			writer.Write( AngularVelocity );
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

			TeleportCount	=	reader.ReadByte();

			Position		=	reader.Read<Vector3>();	
			Rotation		=	reader.Read<Quaternion>();	
			LinearVelocity	=	reader.Read<Vector3>();
			AngularVelocity	=	reader.Read<Vector3>();	

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
		/// Draw entity.
		/// </summary>
		/// <param name="gameTime"></param>
		public virtual void Draw ( GameTime	gameTime, EntityFX entityFx )
		{
		}


		/// <summary>
		/// Inflicts damage to current entity.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="damageType"></param>
		/// <param name="damage"></param>
		/// <param name="kickImpulse"></param>
		/// <param name="kickPoint"></param>
		public virtual void Damage ( Entity2 attacker, DamageType damageType, short damage, Vector3 kickImpulse, Vector3 kickPoint )
		{
		}


		/// <summary>
		/// Called when one entity starts to touch another
		/// </summary>
		/// <param name="other"></param>
		/// <param name="touchPoint"></param>
		public virtual void Touch ( Entity2 other, Vector3 touchPoint )
		{
		}


		/// <summary>
		/// Activate entity in trigger chain.
		/// </summary>
		/// <param name="activator"></param>
		public virtual void Activate ( Entity2 activator )
		{
		}


		/// <summary>
		/// Attempt to use entity.
		/// </summary>
		/// <param name="user"></param>
		public virtual void Use ( Entity2 user )
		{
		}


		/// <summary>
		/// Handle user control.
		/// </summary>
		/// <param name="userCommand"></param>
		public virtual void UserControl ( UserCommand userCommand )
		{
		}


		/// <summary>
		/// Gets hint for player.
		/// </summary>
		public virtual string UserHint { 
			get;
		}


  		/// <summary>
		/// Immediatly put entity in given position without interpolation.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="orient"></param>
		public void Teleport ( Vector3 position, Quaternion orient )
		{
			TeleportCount++;
			TeleportCount &= 0x7F;

			Position		=	position;
			Rotation		=	orient;
			PositionOld		=	position;
			RotationOld		=	orient;
		}

  
		/// <summary>
		/// Moves entity to given position with interpolation.
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
	}
}
