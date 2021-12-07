using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using IronStar.ECS;

namespace IronStar.Environment
{
	public enum DoorControlMode	: byte
	{
		Automatic,
		ExternalToggle,
		//ExternalOpen,
	}

	public class DoorComponent : IComponent
	{
		public string Message = "";
		public int Wait = 3000;
		public DoorControlMode Mode = DoorControlMode.Automatic;

		public TimeSpan Timer = TimeSpan.Zero;

		
		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}


		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}


		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Message	 );
			writer.Write( Wait		 );
			writer.Write( (byte)Mode );
			writer.Write( Timer		 );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Message	=	reader.ReadString();
			Wait	=	reader.ReadInt32();
			Mode	=	(DoorControlMode)reader.ReadByte();
			Timer	=	reader.Read<TimeSpan>();
		}

	}
}
