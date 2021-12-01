using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Environment
{
	public enum DoorControlMode	: byte
	{
		Automatic,
		//ExternalToggle,		//	toggle open/close on external signal
		//ExternalLockUnlock,	//	toggle lock/unlock on external signal
		//ExternalOpen,			//	open door on external signal
	}

	public class DoorComponent : IComponent
	{
		public string Message = "";
		public int Wait = 3000;
		public string Sound = "";
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
			writer.Write( Sound		 );
			writer.Write( (byte)Mode );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Message	=	reader.ReadString();
			Wait	=	reader.ReadInt32();
			Sound	=	reader.ReadString();
			Mode	=	(DoorControlMode)reader.ReadByte();
		}

	}
}
