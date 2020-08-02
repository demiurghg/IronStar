using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.ECS
{
	public abstract class ProcessingSystem<TResource,T1> : ISystem 
	where T1: IComponent
	{
		private readonly Dictionary<uint,TResource> resources = new Dictionary<uint, TResource>();
		private readonly Aspect aspect;

		public ProcessingSystem()
		{
			aspect	=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1>();
		}

		public void Add( GameState gs, Entity e )
		{
			var c1	=	e.GetComponent<T1>();
			var rc	=	Create( gs, c1 );
			resources.Add( e.ID, rc );
		}

		public void Remove( GameState gs, Entity e )
		{
			TResource rc;
			if (resources.TryGetValue( e.ID, out rc))
			{
				resources.Remove( e.ID );
				Destroy( gs, rc );
			}
		}

		public void Update( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities(aspect);

			foreach ( var e in entities )
			{
				var c1	=	e.GetComponent<T1>();
				var rc	=	resources[ e.ID ];
				Process( gs, gameTime, rc, c1 );
			}
		}

		public abstract TResource Create ( GameState gs, T1 component1 );
		public abstract void Destroy ( GameState gs, TResource resource );
		public abstract void Process( GameState gs, GameTime gameTime, TResource resource, T1 component1 );
	}


	public abstract class ProcessingSystem<TResource,T1,T2> : ISystem 
	where T1: IComponent
	where T2: IComponent
	{
		private readonly Dictionary<uint,TResource> resources = new Dictionary<uint, TResource>();
		private readonly Aspect aspect;

		public ProcessingSystem()
		{
			aspect	=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2>();
		}

		public void Add( GameState gs, Entity e )
		{
			var c1	=	e.GetComponent<T1>();
			var c2	=	e.GetComponent<T2>();
			var rc	=	Create( gs, c1, c2 );
			resources.Add( e.ID, rc );
		}

		public void Remove( GameState gs, Entity e )
		{
			TResource rc;
			if (resources.TryGetValue( e.ID, out rc))
			{
				resources.Remove( e.ID );
				Destroy( gs, rc );
			}
		}

		public void Update( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities(aspect);

			foreach ( var e in entities )
			{
				var c1	=	e.GetComponent<T1>();
				var c2	=	e.GetComponent<T2>();
				var rc	=	resources[ e.ID ];
				Process( gs, gameTime, rc, c1, c2 );
			}
		}

		public abstract TResource Create ( GameState gs, T1 component1, T2 component2 );
		public abstract void Destroy ( GameState gs, TResource resource );
		public abstract void Process( GameState gs, GameTime gameTime, TResource resource, T1 component1, T2 component2 );
	}


	public abstract class ProcessingSystem<TResource,T1,T2,T3> : ISystem 
	where T1: IComponent
	where T2: IComponent
	where T3: IComponent
	{
		private readonly Dictionary<uint,TResource> resources = new Dictionary<uint, TResource>();
		private readonly Aspect aspect;

		public ProcessingSystem()
		{
			aspect	=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2,T3>();
		}

		public void Add( GameState gs, Entity e )
		{
			var c1	=	e.GetComponent<T1>();
			var c2	=	e.GetComponent<T2>();
			var c3	=	e.GetComponent<T3>();
			var rc	=	Create( gs, c1,c2,c3 );
			resources.Add( e.ID, rc );
		}

		public void Remove( GameState gs, Entity e )
		{
			TResource rc;
			if (resources.TryGetValue( e.ID, out rc))
			{
				resources.Remove( e.ID );
				Destroy( gs, rc );
			}
		}

		public void Update( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities(aspect);

			foreach ( var e in entities )
			{
				var c1	=	e.GetComponent<T1>();
				var c2	=	e.GetComponent<T2>();
				var c3	=	e.GetComponent<T3>();
				var rc	=	resources[ e.ID ];
				Process( gs, gameTime, rc, c1,c2,c3 );
			}
		}

		public abstract TResource Create ( GameState gs, T1 component1, T2 component2, T3 component3 );
		public abstract void Destroy ( GameState gs, TResource resource );
		public abstract void Process( GameState gs, GameTime gameTime, TResource resource, T1 component1, T2 component2, T3 component3 );
	}


	public abstract class ProcessingSystem<TResource,T1,T2,T3,T4> : ISystem 
	where T1: IComponent
	where T2: IComponent
	where T3: IComponent
	where T4: IComponent
	{
		private readonly Dictionary<uint,TResource> resources = new Dictionary<uint, TResource>();
		private readonly Aspect aspect;

		public ProcessingSystem()
		{
			aspect	=	GetAspect();
		}

		public virtual Aspect GetAspect()
		{
			return new Aspect().Include<T1,T2,T3,T4>();
		}

		public void Add( GameState gs, Entity e )
		{
			var c1	=	e.GetComponent<T1>();
			var c2	=	e.GetComponent<T2>();
			var c3	=	e.GetComponent<T3>();
			var c4	=	e.GetComponent<T4>();
			var rc	=	Create( gs, c1,c2,c3,c4 );
			resources.Add( e.ID, rc );
		}

		public void Remove( GameState gs, Entity e )
		{
			TResource rc;
			if (resources.TryGetValue( e.ID, out rc))
			{
				resources.Remove( e.ID );
				Destroy( gs, rc );
			}
		}

		public void Update( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities(aspect);

			foreach ( var e in entities )
			{
				var c1	=	e.GetComponent<T1>();
				var c2	=	e.GetComponent<T2>();
				var c3	=	e.GetComponent<T3>();
				var c4	=	e.GetComponent<T4>();
				var rc	=	resources[ e.ID ];
				Process( gs, gameTime, rc, c1,c2,c3,c4 );
			}
		}

		public abstract TResource Create ( GameState gs, T1 component1, T2 component2, T3 component3, T4 component4 );
		public abstract void Destroy ( GameState gs, TResource resource );
		public abstract void Process( GameState gs, GameTime gameTime, TResource resource, T1 component1, T2 component2, T3 component3, T4 component4 );
	}
}
