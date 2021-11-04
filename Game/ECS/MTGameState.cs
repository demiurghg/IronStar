using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Content;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using Fusion.Core.Extensions;

namespace IronStar.ECS
{
	public class MTGameState : IGameState
	{
		public ContentManager Content { get { throw new NotImplementedException(); } }

		public Game Game { get { return game; } }

		public bool Paused
		{
			get	{ return simulation.Paused;	}
			set { simulation.Paused = value; }
		}

		IGameState simulation;
		IGameState presentation;
		Game game;

		bool		terminate;
		Thread		gameThread;
		TimeSpan	timestep;

		object	snapshotLock	=	new object();
		byte[]	snapshotWrite	=	new byte[ 512 * 1024 ];
		byte[]	snapshotRead	=	new byte[ 512 * 1024 ];


		void SwapSnapshots()
		{
			lock (snapshotLock)
			{
				Misc.Swap( ref snapshotRead, ref snapshotWrite );
			}
		}


		public MTGameState( Game game, IGameState simulation, IGameState presentation, TimeSpan timestep )
		{
			this.game			=	game;
			this.simulation		=	simulation;
			this.presentation	=	presentation;
			this.timestep		=	timestep;

			gameThread					=	new Thread( new ThreadStart( GameLoop ) );
			gameThread.Name				=	"ECS Thread";
			gameThread.IsBackground		=	true;

			gameThread.Start();
		}


		public void Dispose()
		{
			terminate	=	true;

			gameThread.Join();

			DisposableBase.SafeDispose( ref simulation );
			DisposableBase.SafeDispose( ref presentation );
		}

		
		void GameLoop()
		{
			long	 frames			=	0;
			TimeSpan dt				=	timestep;
			TimeSpan currentTime	=	GameTime.CurrentTime;
			TimeSpan accumulator	=	TimeSpan.Zero;
			var		 stopwatch		=	new Stopwatch();

			simulation.Update( new GameTime(dt, frames++) );
			simulation.Update( new GameTime(dt, frames++) );

			while (!terminate)
			{
				TimeSpan newTime	=	GameTime.CurrentTime;
				TimeSpan frameTime	=	newTime - currentTime;
				currentTime			=	newTime;

				accumulator	+=	frameTime;

				while (accumulator >= dt)
				{
					simulation.Update( new GameTime(dt, frames++) );

					accumulator -= dt;
				}

				using ( var ms = new MemoryStream(snapshotWrite) )
				{
					((GameState)simulation).Save( ms, newTime, dt );
					SwapSnapshots();
				}

				Thread.Sleep( 0 );
			}

			KillAll();
			
			simulation.Update( new GameTime(dt, frames++) );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Update :
		-----------------------------------------------------------------------------------------*/

		public void Update( GameTime gameTime )
		{
			lock (snapshotLock)
			{
				using ( var ms = new MemoryStream(snapshotRead) ) 
				{
					((GameState)presentation).Load( ms );
				}
			}

			presentation.Update(gameTime);
		}

		/*-----------------------------------------------------------------------------------------
		 *	Interface implementation :
		-----------------------------------------------------------------------------------------*/

		public Entity GetEntity( uint id ) { throw new NotImplementedException(); }

		public TService GetService<TService>() where TService : class {	throw new NotImplementedException(); }
		public IEnumerable<Entity> QueryEntities( Aspect aspect ) {	throw new NotImplementedException(); }

		public void KillAll()
		{
			simulation.KillAll();
		}

		public Entity Spawn( IFactory factory )
		{
			return simulation.Spawn(factory);
		}
	}
}
