using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using IronStar.Gameplay.Systems;

namespace IronStar.Gameplay
{
	public static class GSExtensions
	{
		public static Entity FindAttachmentRoot(this Entity entity)
		{
			if (entity == null) return null;

			var attachment = entity.GetComponent<AttachmentComponent>();

			if (attachment!=null)
			{
				return FindAttachmentRoot( attachment.Target );
			}
			else
			{
				return entity;
			}
		}

		
		public static void Trigger( this GameState gs, string target, Entity source, Entity activator )
		{
			gs.GetService<TriggerSystem>().Trigger( target, source, activator );
		}
	}
}
