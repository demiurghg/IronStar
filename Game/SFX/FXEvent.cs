﻿using System;
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

namespace IronStar.SFX {


	public class FXEvent {

		/// <summary>
		/// FX Event type.
		/// </summary>
		public short FXAtom;

		/// <summary>
		/// Reliability counter
		/// </summary>
		public byte SendCount;
		
		/// <summary>
		/// Parent entity ID
		/// </summary>
		public uint EntityID;

		/// <summary>
		/// FX Event source position.
		/// </summary>
		public Vector3 Origin;

		/// <summary>
		/// FX rotation
		/// </summary>
		public Quaternion Rotation;

		/// <summary>
		/// FX velocity
		/// </summary>
		public Vector3 Velocity;

		/// <summary>
		/// 
		/// </summary>
		public float Scale;


		public FXEvent ()
		{
		}


		public Matrix TransformMatrix
		{
			get 
			{
				return Matrix.Scaling( Scale ) * Matrix.RotationQuaternion( Rotation ) * Matrix.Translation( Origin );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxType"></param>
		/// <param name="position"></param>
		/// <param name="target"></param>
		/// <param name="orient"></param>
		public FXEvent ( short fxAtomID, uint parentID, Vector3 origin, Vector3 velocity, Quaternion rotation )
		{
			this.FXAtom		=	fxAtomID;
			this.EntityID	=	parentID;
			this.Origin		=	origin;
			this.Velocity	=	velocity;
			this.Rotation	=	rotation;

			this.Scale		=	1;

			SendCount		=	0;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public void Write ( BinaryWriter writer )
		{
			writer.Write( FXAtom );
			writer.Write( SendCount );
			writer.Write( EntityID );
			writer.Write( Origin );
			writer.Write( Velocity );
			writer.Write( Rotation );
			writer.Write( Scale );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		public void Read ( BinaryReader reader )
		{
			FXAtom		=	reader.ReadInt16();
			SendCount	=	reader.ReadByte();
			EntityID	=	reader.ReadUInt32();
			Origin		=	reader.Read<Vector3>();
			Velocity	=	reader.Read<Vector3>();
			Rotation	=	reader.Read<Quaternion>();
			Scale		=	reader.ReadSingle();
		}
	}
}
