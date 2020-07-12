using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public class GameState : DisposableBase
	{
		public readonly Game Game;

		readonly EntityCollection entities;


		/// <summary>
		/// Game state constructor
		/// </summary>
		/// <param name="game"></param>
		public GameState( Game game )
		{
			this.Game	=	game;

			entities	=	new EntityCollection();
		}


		/// <summary>
		/// Disposes stuff
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				
			}

			base.Dispose( disposing );
		}


		/// <summary>
		/// Updates game state
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update ( GameTime gameTime )
		{
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Entity stuff :
		-----------------------------------------------------------------------------------------------*/

		public Entity Spawn ( Vector3 position, Quaternion rotation )
		{
			throw new NotImplementedException();
		}


		public Entity Spawn ( Vector3 position )
		{
			return Spawn( position, Quaternion.Identity );
		}


		public void Kill ( uint id )
		{
			throw new NotImplementedException();
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Component stuff :
		-----------------------------------------------------------------------------------------------*/

		public bool Attach ( Entity entity, Component component )
		{
			throw new NotImplementedException();
		}


		public bool Detach( Entity entity, Component component )
		{
			throw new NotImplementedException();
		}

		/*-----------------------------------------------------------------------------------------------
		 *	System stuff :
		-----------------------------------------------------------------------------------------------*/

		public void AddSystem ( ISystem system,  )
		{
		}
	}
}
