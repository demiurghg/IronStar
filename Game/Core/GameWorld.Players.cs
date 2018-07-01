using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using IronStar.Entities;
using IronStar.Entities.Players;

namespace IronStar.Core {
	public partial class GameWorld {

		readonly Random rand = new Random();
		readonly Dictionary<Guid,string> usersInfo = new Dictionary<Guid, string>();


		public Entity GetPlayerEntity ( Guid guid )
		{
			return GetEntities()
				.Where( e1 => e1.UserGuid==guid )
				.LastOrDefault();
		}


		public Player GetPlayerCharacter ( Guid guid )
		{
			return GetEntities()
				.Where( e1 => e1.UserGuid==guid )
				.Select( e2 => e2 as Player )
				.Where( ch => ch!=null )
				.LastOrDefault();
		}


		public Entity[] GetAllPlayerEntities ()
		{
			return GetEntities()
				.Where( e1 => e1.UserGuid!=Guid.Empty )
				.ToArray();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void MPWorld_EntityKilled ( object sender, EntityEventArgs e )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="userInfo"></param>
		public Entity SpawnPlayer ( Guid guid, string userInfo )
		{
			var sp = GetEntities()
				.Where( e1 => e1 is StartPoint && (e1 as StartPoint).StartPointType==StartPointType.SinglePlayer)
				.OrderBy( e => rand.Next() )
				.FirstOrDefault();					

			Entity ent;

			if (sp==null) {
				throw new GameException("No start point");
			}

			ent = Spawn( "player" );
			ent.Teleport( sp.Position, sp.Rotation );
			SpawnFX("TeleportOut", ent.ID, sp.Position );
			ent.UserGuid = guid;

			return ent;		
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="command"></param>
		public void FeedPlayerCommand ( Guid guid, UserCommand command )
		{
			var targets = entities
							.Where( e => e.Value.UserGuid==guid )
							.ToArray();

			foreach ( var target in targets ) {
				target.Value.UserControl( command );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="userInfo"></param>
		/// <returns></returns>
		public bool ApprovePlayer ( Guid guid, string userInfo )
		{
			return !entities.Any( p => p.Value.UserGuid == guid );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="userInfo"></param>
		public void PlayerConnected ( Guid guid, string userInfo )
		{
			usersInfo.Add( guid, userInfo );
			MessageService?.Push( string.Format("Client connected : {0} {1}", guid, userInfo) );
		}



		/// <summary>
		/// Called internally when player entered.
		/// </summary>
		/// <param name="guid"></param>
		public void PlayerEntered ( Guid guid )
		{
			LogTrace("player entered: {0}", guid );

			var ent = SpawnPlayer( guid, usersInfo[guid] );
		}



		/// <summary>
		/// Called internally when player left.
		/// </summary>
		/// <param name="guid"></param>
		public void PlayerLeft ( Guid guid )
		{
			LogTrace("player left: {0}", guid );

			Kill( e => e.UserGuid==guid );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		public void PlayerDisconnected ( Guid guid )
		{
			usersInfo.Remove( guid );
			MessageService?.Push( string.Format("Client disconnected : {0}", guid) );
		}

	}
}
