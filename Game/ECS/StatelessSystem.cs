using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUutilities.Threading;
using Fusion.Core;

namespace IronStar.ECS
{
	public abstract class StatelessSystem<T1> : DisposableBase, ISystem 
	where T1: IComponent
	{
		private readonly Aspect aspect;
		private readonly IParallelLooper looper;

		public StatelessSystem(IParallelLooper looper)
		{
			this.looper	=	looper ?? new DefaultLooper();
			aspect		=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1>();
		}

		public virtual void Add( GameState gs, Entity e ) {}
		public virtual void Remove( GameState gs, Entity e ) {}

		public virtual void Update( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities(aspect).ToArray();

			looper.ForLoop(0, entities.Length, idx =>
			{
				var e	=	entities[idx];
				var c1	=	e.GetComponent<T1>();
				Process( e, gameTime, c1 );
			});
		}

		protected abstract void Process( Entity entity, GameTime gameTime, T1 component1 );
	}


	public abstract class StatelessSystem<T1,T2> : DisposableBase, ISystem 
	where T1: IComponent
	where T2: IComponent
	{
		private readonly Aspect aspect;

		public StatelessSystem()
		{
			aspect	=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2>();
		}

		public virtual void Add( GameState gs, Entity e ) {}
		public virtual void Remove( GameState gs, Entity e ) {}

		public virtual void Update( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities(aspect);

			foreach ( var e in entities )
			{
				var c1	=	e.GetComponent<T1>();
				var c2	=	e.GetComponent<T2>();
				Process( e, gameTime, c1, c2 );
			}
		}

		protected abstract void Process( Entity entity, GameTime gameTime, T1 component1, T2 component2 );
	}


	public abstract class StatelessSystem<T1,T2,T3> : ISystem 
	where T1: IComponent
	where T2: IComponent
	where T3: IComponent
	{
		private readonly Aspect aspect;

		public StatelessSystem()
		{
			aspect	=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2,T3>();
		}

		public virtual void Add( GameState gs, Entity e ) {}
		public virtual void Remove( GameState gs, Entity e ) {}

		protected void ForEach( GameState gs, GameTime gameTime, Action<Entity,GameTime,T1,T2,T3> action )
		{
			var entities = gs.QueryEntities(aspect);

			foreach ( var e in entities )
			{
				var c1	=	e.GetComponent<T1>();
				var c2	=	e.GetComponent<T2>();
				var c3	=	e.GetComponent<T3>();
				action( e, gameTime, c1,c2,c3 );
			}
		}

		public virtual void Update( GameState gs, GameTime gameTime )
		{
			ForEach( gs, gameTime, Process );
		}

		protected abstract void Process( Entity entity, GameTime gameTime, T1 component1, T2 component2, T3 component3 );
	}


	public abstract class StatelessSystem<T1,T2,T3,T4> : ISystem 
	where T1: IComponent
	where T2: IComponent
	where T3: IComponent
	where T4: IComponent
	{
		private readonly Aspect aspect;

		public StatelessSystem()
		{
			aspect	=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2,T3,T4>();
		}

		public virtual void Add( GameState gs, Entity e ) {}
		public virtual void Remove( GameState gs, Entity e ) {}

		public virtual void Update( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities(aspect);

			foreach ( var e in entities )
			{
				var c1	=	e.GetComponent<T1>();
				var c2	=	e.GetComponent<T2>();
				var c3	=	e.GetComponent<T3>();
				var c4	=	e.GetComponent<T4>();
				Process( e, gameTime, c1,c2,c3,c4 );
			}
		}

		protected abstract void Process( Entity entity, GameTime gameTime, T1 component1, T2 component2, T3 component3, T4 component4 );
	}
}
