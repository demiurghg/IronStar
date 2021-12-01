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
	public enum ElevatorMode	: byte
	{
		OneWay,
		Toggle,
	}

	public class ElevatorComponent : IComponent
	{
		public int Wait = 3000;
		public ElevatorMode Mode = ElevatorMode.OneWay;
		public TimeSpan Timer = TimeSpan.Zero;
		public bool Engaged = false;

		
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
			writer.Write( Wait		 );
			writer.Write( (byte)Mode );
			writer.Write( Timer		 );
			writer.Write( Engaged	 );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Wait	=	reader.ReadInt32();
			Mode	=	(ElevatorMode)reader.ReadByte();
			Timer	=	reader.Read<TimeSpan>();
			Engaged	=	reader.ReadBoolean();
		}
	}
}
