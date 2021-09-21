using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace IronStar.ECS
{
	public abstract class DrawSystem<TResource,T1,T2> : DisposableBase, IDrawSystem 
	where T1: IComponent
	where T2: IComponent
	{
		private readonly Dictionary<uint,TResource> resources = new Dictionary<uint, TResource>();
		private readonly Aspect aspect;
		private readonly object lockObj = new object();

		ConcurrentQueue<Tuple<Entity,T1,T2>>	creationQueue	=	new ConcurrentQueue<Tuple<Entity,T1,T2>>();
		ConcurrentQueue<Entity>					destroyQueue	=	new ConcurrentQueue<Entity>();

		public DrawSystem()
		{
			aspect	=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2>();
		}

		public void Add( GameState gs, Entity e )
		{
			var c1	=	(T1)e.GetComponent<T1>().Clone();
			var c2	=	(T2)e.GetComponent<T2>().Clone();
			creationQueue.Enqueue( Tuple.Create(e, c1, c2) );
		}

		public void Remove( GameState gs, Entity e )
		{
			destroyQueue.Enqueue( e );
		}

		protected virtual IEnumerable<Entity> OrderEntities( IEnumerable<Entity> entities )
		{
			return entities;
		}

		protected void ForEach( GameState gs, GameTime gameTime, Action<Entity,GameTime,TResource,T1,T2> action )
		{
			lock (lockObj)
			{
				var entities = OrderEntities( gs.QueryEntities(aspect) );

				foreach ( var e in entities )
				{
					var c1	=	e.GetComponent<T1>();
					var c2	=	e.GetComponent<T2>();
					var rc	=	default(TResource);

					var integral = ( c1!=null && c2!=null );

					if (integral && resources.TryGetValue( e.ID, out rc ))
					{
						action( e, gameTime, rc, c1, c2 );
					}
				}
			}
		}

		public virtual void Draw( GameState gs, GameTime gameTime )
		{
			Tuple<Entity,T1,T2> cd;
			Entity e;
			TResource rc;

			while (creationQueue.TryDequeue(out cd))
			{
				var ce	=	cd.Item1;
				var c1	=	cd.Item2;
				var c2	=	cd.Item3;
				resources.Add( ce.ID, Create( ce, c1, c2 ) );
			}

			ForEach( gs, gameTime, DrawEntity );

			while (destroyQueue.TryDequeue(out e))
			{
				if (resources.TryGetValue( e.ID, out rc ))
				{
					Destroy( e, rc );
				}
			}
		}

		public virtual void Update( GameState gs, GameTime gameTime )
		{
		}

		protected abstract TResource Create ( Entity entity, T1 component1, T2 component2 );
		protected abstract void Destroy ( Entity entity, TResource resource );
		protected abstract void DrawEntity( Entity entity, GameTime gameTime, TResource resource, T1 component1, T2 component2 );
	}



	public abstract class DrawSystem<TResource,T1,T2,T3> : DisposableBase, IDrawSystem 
	where T1: IComponent
	where T2: IComponent
	where T3: IComponent
	{
		private readonly Dictionary<uint,TResource> resources = new Dictionary<uint, TResource>();
		private readonly Aspect aspect;
		private readonly object lockObj = new object();

		ConcurrentQueue<Tuple<Entity,T1,T2,T3>>	creationQueue	=	new ConcurrentQueue<Tuple<Entity,T1,T2,T3>>();
		ConcurrentQueue<Entity>					destroyQueue	=	new ConcurrentQueue<Entity>();

		public DrawSystem()
		{
			aspect	=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2,T3>();
		}

		public void Add( GameState gs, Entity e )
		{
			var c1	=	(T1)e.GetComponent<T1>().Clone();
			var c2	=	(T2)e.GetComponent<T2>().Clone();
			var c3	=	(T3)e.GetComponent<T3>().Clone();
			creationQueue.Enqueue( Tuple.Create(e, c1, c2, c3) );
		}

		public void Remove( GameState gs, Entity e )
		{
			destroyQueue.Enqueue( e );
		}

		protected virtual IEnumerable<Entity> OrderEntities( IEnumerable<Entity> entities )
		{
			return entities;
		}

		protected void ForEach( GameState gs, GameTime gameTime, Action<Entity,GameTime,TResource,T1,T2,T3> action )
		{
			lock (lockObj)
			{
				var entities = OrderEntities( gs.QueryEntities(aspect) );

				foreach ( var e in entities )
				{
					var c1	=	e.GetComponent<T1>();
					var c2	=	e.GetComponent<T2>();
					var c3	=	e.GetComponent<T3>();
					var rc	=	default(TResource);

					var integral = ( c1!=null && c2!=null && c3!=null);

					if (integral && resources.TryGetValue( e.ID, out rc ))
					{
						action( e, gameTime, rc, c1, c2, c3 );
					}
				}
			}
		}

		public virtual void Draw( GameState gs, GameTime gameTime )
		{
			Tuple<Entity,T1,T2,T3> cd;
			Entity e;
			TResource rc;

			while (creationQueue.TryDequeue(out cd))
			{
				var ce	=	cd.Item1;
				var c1	=	cd.Item2;
				var c2	=	cd.Item3;
				var c3	=	cd.Item4;
				resources.Add( ce.ID, Create( ce, c1, c2, c3 ) );
			}

			ForEach( gs, gameTime, DrawEntity );

			while (destroyQueue.TryDequeue(out e))
			{
				if (resources.TryGetValue( e.ID, out rc ))
				{
					Destroy( e, rc );
				}
			}
		}

		public virtual void Update( GameState gs, GameTime gameTime )
		{
		}

		protected abstract TResource Create ( Entity entity, T1 component1, T2 component2, T3 component3 );
		protected abstract void Destroy ( Entity entity, TResource resource );
		protected abstract void DrawEntity( Entity entity, GameTime gameTime, TResource resource, T1 component1, T2 component2, T3 component3 );
	}



	public abstract class DrawSystem<TResource,T1,T2,T3,T4> : DisposableBase, IDrawSystem 
	where T1: IComponent
	where T2: IComponent
	where T3: IComponent
	where T4: IComponent
	{
		private readonly Dictionary<uint,TResource> resources = new Dictionary<uint, TResource>();
		private readonly Aspect aspect;
		private readonly object lockObj = new object();

		ConcurrentQueue<Tuple<Entity,T1,T2,T3,T4>>	creationQueue	=	new ConcurrentQueue<Tuple<Entity,T1,T2,T3,T4>>();
		ConcurrentQueue<Entity>						destroyQueue	=	new ConcurrentQueue<Entity>();

		public DrawSystem()
		{
			aspect	=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2,T3,T4>();
		}

		public void Add( GameState gs, Entity e )
		{
			var c1	=	(T1)e.GetComponent<T1>().Clone();
			var c2	=	(T2)e.GetComponent<T2>().Clone();
			var c3	=	(T3)e.GetComponent<T3>().Clone();
			var c4	=	(T4)e.GetComponent<T4>().Clone();
			creationQueue.Enqueue( Tuple.Create(e, c1, c2, c3, c4) );
		}

		public void Remove( GameState gs, Entity e )
		{
			destroyQueue.Enqueue( e );
		}

		protected virtual IEnumerable<Entity> OrderEntities( IEnumerable<Entity> entities )
		{
			return entities;
		}

		protected void ForEach( GameState gs, GameTime gameTime, Action<Entity,GameTime,TResource,T1,T2,T3,T4> action )
		{
			lock (lockObj)
			{
				var entities = OrderEntities( gs.QueryEntities(aspect) );

				foreach ( var e in entities )
				{
					var c1	=	e.GetComponent<T1>();
					var c2	=	e.GetComponent<T2>();
					var c3	=	e.GetComponent<T3>();
					var c4	=	e.GetComponent<T4>();
					var rc	=	default(TResource);

					var integral = ( c1!=null && c2!=null && c3!=null && c4!=null);

					if (integral && resources.TryGetValue( e.ID, out rc ))
					{
						action( e, gameTime, rc, c1, c2, c3, c4 );
					}
				}
			}
		}

		public virtual void Draw( GameState gs, GameTime gameTime )
		{
			Tuple<Entity,T1,T2,T3,T4> cd;
			Entity e;
			TResource rc;

			while (creationQueue.TryDequeue(out cd))
			{
				var ce	=	cd.Item1;
				var c1	=	cd.Item2;
				var c2	=	cd.Item3;
				var c3	=	cd.Item4;
				var c4	=	cd.Item5;
				resources.Add( ce.ID, Create( ce, c1, c2, c3, c4 ) );
			}

			ForEach( gs, gameTime, DrawEntity );

			while (destroyQueue.TryDequeue(out e))
			{
				if (resources.TryGetValue( e.ID, out rc ))
				{
					Destroy( e, rc );
				}
			}
		}

		public virtual void Update( GameState gs, GameTime gameTime )
		{
		}

		protected abstract TResource Create ( Entity entity, T1 component1, T2 component2, T3 component3, T4 component4 );
		protected abstract void Destroy ( Entity entity, TResource resource );
		protected abstract void DrawEntity( Entity entity, GameTime gameTime, TResource resource, T1 component1, T2 component2, T3 component3, T4 component4 );
	}
}
