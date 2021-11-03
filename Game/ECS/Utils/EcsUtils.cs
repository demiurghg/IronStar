using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	public static class EcsUtils
	{
		public static void WriteEntity( this BinaryWriter writer, IGameState gs, Entity entity )
		{
			writer.Write( entity.ID );
		}

		public static Entity ReadEntity( this BinaryReader reader, IGameState gs )
		{
			return gs.GetEntity( reader.ReadUInt32() );
		}
	}
}
