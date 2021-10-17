using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUutilities.Threading;
using Fusion.Core;

namespace IronStar.ECS
{
	class DefaultLooper : IParallelLooper
	{
		public int ThreadCount
		{
			get { return 1; }
		}

		public void ForLoop( int startIndex, int endIndex, Action<int> loopBody )
		{
			for (int index = startIndex; index < endIndex; index++)
			{
				loopBody(index);
			}
		}
	}



	public abstract class ProcessingSystem<TResource,T1> : DisposableBase, ISystem 
	where T1: IComponent
	{
		private readonly Dictionary<uint,TResource> resources = new Dictionary<uint, TResource>();
		private readonly Aspect aspect;
		private readonly object lockObj = new object();
		private readonly IParallelLooper looper;

		public ProcessingSystem(IParallelLooper looper)
		{
			this.looper	=	looper ?? new DefaultLooper();
			aspect		=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1>();
		}

		public void Add( GameState gs, Entity e )
		{
			lock (lockObj)
			{
				var c1	=	e.GetComponent<T1>();
				var rc	=	Create( e, c1 );
				resources.Add( e.ID, rc );
			}
		}

		public void Remove( GameState gs, Entity e )
		{
			lock (lockObj)
			{
				TResource rc;
				if (resources.TryGetValue( e.ID, out rc))
				{
					resources.Remove( e.ID );
					Destroy( e, rc );
				}
			}
		}

		protected virtual IEnumerable<Entity> OrderEntities( IEnumerable<Entity> entities )
		{
			return entities;
		}

		protected void ForEach( GameState gs, GameTime gameTime, Action<Entity,GameTime,TResource,T1> action )
		{
			lock (lockObj)
			{
				var entities = OrderEntities( gs.QueryEntities(aspect) ).ToArray();

				looper.ForLoop( 0, entities.Length, idx => 
				{
					var e	=	entities[idx];

					var c1	=	e.GetComponent<T1>();
					var rc	=	default(TResource);

					if (resources.TryGetValue( e.ID, out rc ))
					{
						action( e, gameTime, rc, c1 );
					}
				});
			}
		}

		public virtual void Update( GameState gs, GameTime gameTime )
		{
			ForEach( gs, gameTime, Process );
		}

		protected abstract TResource Create ( Entity entity, T1 component1 );
		protected abstract void Destroy ( Entity entity, TResource resource );
		protected abstract void Process( Entity entity, GameTime gameTime, TResource resource, T1 component1 );
	}


	public abstract class ProcessingSystem<TResource,T1,T2> : DisposableBase, ISystem 
	where T1: IComponent
	where T2: IComponent
	{
		private readonly Dictionary<uint,TResource> resources = new Dictionary<uint, TResource>();
		private readonly Aspect aspect;
		private readonly object lockObj = new object();
		private readonly IParallelLooper looper;

		public ProcessingSystem(IParallelLooper looper)
		{
			this.looper	=	looper ?? new DefaultLooper();
			aspect		=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2>();
		}

		public void Add( GameState gs, Entity e )
		{
			lock (lockObj)
			{
				var c1	=	e.GetComponent<T1>();
				var c2	=	e.GetComponent<T2>();
				var rc	=	Create( e, c1, c2 );
				resources.Add( e.ID, rc );
			}
		}

		public void Remove( GameState gs, Entity e )
		{
			lock (lockObj)
			{
				TResource rc;
				if (resources.TryGetValue( e.ID, out rc))
				{
					resources.Remove( e.ID );
					Destroy( e, rc );
				}
			}
		}

		protected virtual IEnumerable<Entity> OrderEntities( IEnumerable<Entity> entities )
		{
			return entities;
		}

		protected void ForEach( GameState gs, GameTime gameTime, Action<Entity,GameTime,TResource,T1,T2> action )
		{
			lock (lockObj)
			{
				var entities = OrderEntities( gs.QueryEntities(aspect) ).ToArray();

				looper.ForLoop( 0, entities.Length, idx => 
				{
					var e	=	entities[idx];

					var c1	=	e.GetComponent<T1>();
					var c2	=	e.GetComponent<T2>();
					var rc	=	default(TResource);

					if (resources.TryGetValue( e.ID, out rc ))
					{
						action( e, gameTime, rc, c1,c2 );
					}
				});
			}
		}

		public virtual void Update( GameState gs, GameTime gameTime )
		{
			ForEach( gs, gameTime, Process );
		}

		protected abstract TResource Create ( Entity entity, T1 component1, T2 component2 );
		protected abstract void Destroy ( Entity entity, TResource resource );
		protected abstract void Process( Entity entity, GameTime gameTime, TResource resource, T1 component1, T2 component2 );
	}


	public abstract class ProcessingSystem<TResource,T1,T2,T3> : ISystem 
	where T1: IComponent
	where T2: IComponent
	where T3: IComponent
	{
		private readonly Dictionary<uint,TResource> resources = new Dictionary<uint, TResource>();
		private readonly Aspect aspect;
		private readonly object lockObj = new object();
		private readonly IParallelLooper looper;

		public ProcessingSystem(IParallelLooper looper)
		{
			this.looper	=	looper ?? new DefaultLooper();
			aspect		=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2,T3>();
		}

		public void Add( GameState gs, Entity e )
		{
			lock (lockObj)
			{
				var c1	=	e.GetComponent<T1>();
				var c2	=	e.GetComponent<T2>();
				var c3	=	e.GetComponent<T3>();
				var rc	=	Create( e, c1,c2,c3 );
				resources.Add( e.ID, rc );
			}
		}

		public void Remove( GameState gs, Entity e )
		{
			lock (lockObj)
			{
				TResource rc;
				if (resources.TryGetValue( e.ID, out rc))
				{
					resources.Remove( e.ID );
					Destroy( e, rc );
				}
			}
		}

		protected virtual IEnumerable<Entity> OrderEntities( IEnumerable<Entity> entities )
		{
			return entities;
		}

		protected void ForEach( GameState gs, GameTime gameTime, Action<Entity,GameTime,TResource,T1,T2,T3> action )
		{
			lock (lockObj)
			{
				var entities = OrderEntities( gs.QueryEntities(aspect) ).ToArray();

				looper.ForLoop( 0, entities.Length, idx => 
				{
					var e	=	entities[idx];

					var c1	=	e.GetComponent<T1>();
					var c2	=	e.GetComponent<T2>();
					var c3	=	e.GetComponent<T3>();
					var rc	=	default(TResource);

					if (resources.TryGetValue( e.ID, out rc ))
					{
						action( e, gameTime, rc, c1,c2,c3 );
					}
				});
			}
		}

		public virtual void Update( GameState gs, GameTime gameTime )
		{
			ForEach( gs, gameTime, Process );
		}

		protected abstract TResource Create ( Entity entity, T1 component1, T2 component2, T3 component3 );
		protected abstract void Destroy ( Entity entity, TResource resource );
		protected abstract void Process( Entity entity, GameTime gameTime, TResource resource, T1 component1, T2 component2, T3 component3 );
	}


	public abstract class ProcessingSystem<TResource,T1,T2,T3,T4> : ISystem 
	where T1: IComponent
	where T2: IComponent
	where T3: IComponent
	where T4: IComponent
	{
		private readonly Dictionary<uint,TResource> resources = new Dictionary<uint, TResource>();
		private readonly Aspect aspect;
		private readonly object lockObj = new object();
		private readonly IParallelLooper looper;

		public ProcessingSystem( IParallelLooper looper )
		{
			this.looper	=	looper ?? new DefaultLooper();
			aspect		=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2,T3,T4>();
		}

		public void Add( GameState gs, Entity e )
		{
			lock (lockObj)
			{
				var c1	=	e.GetComponent<T1>();
				var c2	=	e.GetComponent<T2>();
				var c3	=	e.GetComponent<T3>();
				var c4	=	e.GetComponent<T4>();
				var rc	=	Create( e, c1,c2,c3,c4 );
				resources.Add( e.ID, rc );
			}
		}

		public void Remove( GameState gs, Entity e )
		{
			lock (lockObj)
			{
				TResource rc;
				if (resources.TryGetValue( e.ID, out rc))
				{
					resources.Remove( e.ID );
					Destroy( e, rc );
				}
			}
		}

		protected virtual IEnumerable<Entity> OrderEntities( IEnumerable<Entity> entities )
		{
			return entities;
		}

		protected void ForEach( GameState gs, GameTime gameTime, Action<Entity,GameTime,TResource,T1,T2,T3,T4> action )
		{
			lock (lockObj)
			{
				var entities = OrderEntities( gs.QueryEntities(aspect) ).ToArray();

				looper.ForLoop( 0, entities.Length, idx => 
				{
					var e	=	entities[idx];

					var c1	=	e.GetComponent<T1>();
					var c2	=	e.GetComponent<T2>();
					var c3	=	e.GetComponent<T3>();
					var c4	=	e.GetComponent<T4>();
					var rc	=	default(TResource);

					if (resources.TryGetValue( e.ID, out rc ))
					{
						action( e, gameTime, rc, c1,c2,c3,c4 );
					}
				});
			}
		}


		public virtual void Update( GameState gs, GameTime gameTime )
		{
			ForEach( gs, gameTime, Process );
		}


		protected abstract TResource Create ( Entity entity, T1 component1, T2 component2, T3 component3, T4 component4 );
		protected abstract void Destroy ( Entity entity, TResource resource );
		protected abstract void Process( Entity entity, GameTime gameTime, TResource resource, T1 component1, T2 component2, T3 component3, T4 component4 );
	}
}
