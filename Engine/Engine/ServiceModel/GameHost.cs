using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Storage;

namespace Fusion.Engine.ServiceModel {

	public class GameHost : IDisposable {

		readonly List<GameService> services = new List<GameService>();


		/// <summary>
		/// 
		/// </summary>
		public GameHost ()
		{
		}



		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetService<T>() where T: GameService
		{
			return (T)services.FirstOrDefault( svc => svc is T );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameService"></param>
		protected void Attach( GameService gameService )
		{
			lock (services) {
				services.Add( gameService );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public virtual void Initialize ()
		{
			foreach ( var svc in services ) {

				Log.Message("Initialize: {0}/{1}", GetType().Name, svc.GetType().Name );

				svc.Initialize();
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void SaveConfiguration ( IStorage storage )
		{
			Log.Message("Saving configuration...");

			var storage = game.UserStorage;

			storage.DeleteFile(filename);
			Save( storage.OpenFile(filename, FileMode.Create, FileAccess.Write) );
			foreach ( var svc in services ) {

				Log.Message("Saving configuration: {0}/{1}", GetType().Name, svc.GetType().Name );

				svc.Initialize();
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void LoadConfiguration ( IStorage storage )
		{
		}



		private bool disposedValue = false; // To detect redundant calls


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose( bool disposing )
		{
			if ( !disposedValue ) {

				if ( disposing ) {
					
					services.Reverse();

					foreach ( var svc in services ) {
					
						Log.Message("Dispose: {0}/{1}", GetType().Name, svc.GetType().Name );

						var svcDispose = svc as IDisposable;

						svcDispose?.Dispose();
					}

				}

				disposedValue = true;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			Dispose( true );
		}
	}
}
