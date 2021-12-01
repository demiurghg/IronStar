using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;
using IronStar.Gameplay.Systems;

namespace IronStar.Gameplay
{
	public static class GSExtensions
	{
		public static void Trigger( this GameState gs, string target, Entity source, Entity activator )
		{
			gs.GetService<TriggerSystem>().Trigger( target, source, activator );
		}
	}
}
