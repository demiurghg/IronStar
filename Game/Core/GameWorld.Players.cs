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
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using IronStar.Entities;

namespace IronStar.Core {
	public partial class GameWorld {

		Random rand = new Random();

		/// <summary>
		/// List of players;
		/// </summary>
		public readonly List<Player> Players = new List<Player>();



		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		Player GetPlayer ( Guid guid )
		{
			return Players.LastOrDefault( p => p.Guid==guid );
		}


		public Entity GetPlayerEntity ( Guid guid )
		{
			return GetEntities()
				.Where( e1 => e1.UserGuid==guid )
				.LastOrDefault();
		}


		public Character GetPlayerCharacter ( Guid guid )
		{
			return GetEntities()
				.Where( e1 => e1.UserGuid==guid )
				.Select( e2 => e2.Controller as Character )
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
			foreach ( var pe in Players.Where( p => p.PlayerEntity == e.Entity ) ) {
				pe.Killed(e.Entity);
			}
			
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="n"></param>
		public void AddScore ( Guid guid, int n )
		{
			var p = GetPlayer(guid);
			if (p!=null) {
				p.Score += n;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dt"></param>
		public void UpdatePlayers ( float dt )
		{
			foreach ( var player in	Players ) {
				player.Update(this, dt);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="command"></param>
		/// <param name="lag"></param>
		public void PlayerCommand ( Guid guid, byte[] command, float lag )
		{
			var p = GetPlayer(guid);
			if (p!=null) {
				p.FeedCommand(this, command);
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
			return !Players.Any( p => p.Guid == guid );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="userInfo"></param>
		public void PlayerConnected ( Guid guid, string userInfo )
		{
			MessageService.Push( string.Format("Client connected : {0} {1}", guid, userInfo) );
			Players.Add( new Player( guid, userInfo ) );
		}



		/// <summary>
		/// Called internally when player entered.
		/// </summary>
		/// <param name="guid"></param>
		public void PlayerEntered ( Guid guid )
		{
			LogTrace("player entered: {0}", guid );

			var p = GetPlayer(guid);

			if (p!=null) {
				p.Ready = true;
			}
		}



		/// <summary>
		/// Called internally when player left.
		/// </summary>
		/// <param name="guid"></param>
		public void PlayerLeft ( Guid guid )
		{
			LogTrace("player left: {0}", guid );

			var ent = GetPlayerEntity( guid );

			if (ent!=null) {
				Kill( ent.ID );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		public void PlayerDisconnected ( Guid guid )
		{
			MessageService.Push( string.Format("Client disconnected : {0}", guid) );
			Players.RemoveAll( p => p.Guid == guid );
		}

	}
}
