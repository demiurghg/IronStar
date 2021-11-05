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
using Fusion;

namespace IronStar.ECS
{
	public class MTGameState : IGameState
	{
		public ContentManager Content 
		{ 
			get { return simulation.Content ?? presentation.Content; } 
		}

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

		const int SnapshotCount = 5;
		const int SnapshotSize	= 512 * 1024;

		ConcurrentQueue<byte[]>	recycleQueue	=	new ConcurrentQueue<byte[]>();
		ConcurrentQueue<byte[]>	dispatchQueue	=	new ConcurrentQueue<byte[]>();



		public MTGameState( Game game, IGameState simulation, IGameState presentation, TimeSpan timestep )
		{
			this.game			=	game;
			this.simulation		=	simulation;
			this.presentation	=	presentation;
			this.timestep		=	timestep;

			for (int i=0; i<SnapshotCount; i++)
			{
				recycleQueue.Enqueue( new byte[SnapshotSize] );
			}

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
			var		 stopwatch		=	new Stopwatch();
			long	 frames			=	0;
			TimeSpan dt				=	timestep;
			TimeSpan currentTime	=	GameTime.CurrentTime;
			TimeSpan accumulator	=	TimeSpan.Zero;

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
					stopwatch.Restart();
					
					simulation.Update( new GameTime(dt, frames++) );
					
					stopwatch.Stop();

					if (stopwatch.Elapsed > dt)
					{
						Log.Warning("LOOP TIME {0} > DT {1}", stopwatch.Elapsed, dt);
					}

					accumulator -= dt;

					byte[] snapshot;

					if (recycleQueue.TryDequeue(out snapshot))
					{
						using ( var ms = new MemoryStream(snapshot) )
						{
							((GameState)simulation).Save( ms, newTime, dt );
							dispatchQueue.Enqueue( snapshot );
						}
					}
					else
					{
						Log.Warning("SNAPSHOT STARVATION");
					}
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
			byte[] snapshot;

			while (dispatchQueue.TryDequeue(out snapshot))
			{
				using ( var ms = new MemoryStream(snapshot) ) 
				{
					((GameState)presentation).Load( ms );
				}
				recycleQueue.Enqueue( snapshot );
			}

			((GameState)presentation).InterpolateState( gameTime.Current );

			presentation.Update(gameTime);
		}

		/*-----------------------------------------------------------------------------------------
		 *	Interface implementation :
		-----------------------------------------------------------------------------------------*/

		public Entity GetEntity( uint id ) { throw new NotImplementedException(); }
		public void ForceRefresh() { throw new NotImplementedException(); }
		public IEnumerable<Entity> QueryEntities( Aspect aspect ) {	throw new NotImplementedException(); }
		public IEnumerable<ISystem> Systems { get { throw new NotImplementedException(); } }


		public TService GetService<TService>() where TService : class 
		{	
			return simulation.GetService<TService>() ?? presentation.GetService<TService>();
		}

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
