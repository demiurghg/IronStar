using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.ECS
{
	public abstract class ProcessingSystem<TResource,T1> : ISystem
	{
		public Aspect GetAspect()
		{
			return new Aspect().Include<T1>();
		}

		public void Add( GameState gs, Entity e )
		{
			throw new NotImplementedException();
		}

		public void Remove( GameState gs, Entity e )
		{
			throw new NotImplementedException();
		}

		public void Update( GameState gs, GameTime gameTime )
		{
			throw new NotImplementedException();
		}

		public abstract TResource Create ( T1 component1 );
		public abstract TResource Destroy ( T1 component1 );
		public abstract void Process( TResource resource, T1 component1 );
	}
}
