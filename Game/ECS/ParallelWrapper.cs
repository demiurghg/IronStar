using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;

namespace IronStar.ECS
{
	class ParallelWrapper : ISystem
	{
		readonly GameState gs;
		readonly ISystem[] systems;
		readonly string debugName;

		public ParallelWrapper( GameState gs, ISystem[] systems )
		{
			this.gs			=	gs;
			this.systems	=	systems;
			debugName		=	string.Join("|", systems.Select( s => s.GetType().Name ));
		}

		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}

		public void Add( GameState gs, Entity e )
		{
		}

		public void Remove( GameState gs, Entity e )
		{
		}

		public void Update( GameState gs, GameTime gameTime )
		{
			using ( new CVEvent( debugName ) )
			{
				gs.Game.ParallelLooper.ForLoop( 0, systems.Length, idx => systems[idx].Update( gs, gameTime ) );
			}
		}
	}
}
