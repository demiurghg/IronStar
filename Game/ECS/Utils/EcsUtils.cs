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
			if (entity!=null)
			{
				writer.Write( entity.ID );
			}
			else
			{
				writer.Write( 0 );
			}
		}

		public static Entity ReadEntity( this BinaryReader reader, IGameState gs )
		{
			uint id = reader.ReadUInt32();

			if (id==0)
			{
				return null;
			}
			else
			{
				return gs.GetEntity( id );
			}
		}
	}
}
