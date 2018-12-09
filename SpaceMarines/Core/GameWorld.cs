using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using SpaceMarines.Mapping;
using SpaceMarines.SFX;

namespace SpaceMarines.Core {

	public class GameWorld {
		
		readonly Game Game;
		readonly Dictionary<uint,Entity> entitites;
		uint counter = 1;

		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="map"></param>
		public GameWorld( Game game, Map map )
		{
			this.Game	=	game;
			entitites	=	new Dictionary<uint, Entity>();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="classname"></param>
		/// <returns></returns>
		public Entity Spawn ( string classname )
		{
			var factory =	Game.Content.Load<EntityFactory>(@"entities\" + classname);
			var entity	=	factory.Spawn( counter++, this ); 

			entitites.Add( entity.ID, entity );

			return entity;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void Simulate( GameTime gameTime )
		{
			foreach ( var idEntityPair in entitites ) {

				var entity	=	idEntityPair.Value;

				entity.Update( gameTime );

			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void Present ( GameTime gameTime )
		{
		}
	}
}
