using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public enum Team : byte
	{
		Player,
		Monsters,
	}

	public class TeamComponent : IComponent
	{
		public Team Team;

		public TeamComponent()
		{
			Team = Team.Monsters;
		}

		public TeamComponent( Team team )
		{
			Team = team;
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( (byte)Team );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Team	=	(Team)reader.ReadByte();
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}
	}
}
