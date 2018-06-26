using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using IronStar.SFX;

namespace IronStar.Core {
	public class SnapshotReader {

		int recvSnapshotCounter = 0;

		/// <summary>
		/// 
		/// </summary>
		public SnapshotReader ()
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="snapshotStream"></param>
		/// <param name="entities"></param>
		/// <param name="fxEvents"></param>
		public void Read ( GameWorld world, Stream snapshotStream, IStorable header, IDictionary<uint,Entity> entities, Action<FXEvent> runfx, Action<Entity> spawned, Action<uint> killed )
		{
			using ( var reader = new BinaryReader( snapshotStream ) ) {

				header.Read( reader, 1 );

				int snapshotCounter			=	reader.ReadInt32();
				int snapshotCountrerDelta	=	snapshotCounter - recvSnapshotCounter;
				recvSnapshotCounter			=	snapshotCounter;

				reader.ExpectFourCC("ENT0", "Bad snapshot");

				int length	=	reader.ReadInt32();
				var oldIDs	=	entities.Select( pair => pair.Key ).ToArray();
				var newIDs	=	new uint[length];

				for ( int i=0; i<length; i++ ) {

					uint  id	=	reader.ReadUInt32();
					short clsId	=	reader.ReadInt16();
					newIDs[i]	=	id;

					if ( entities.ContainsKey(id) ) {

						//	Entity with given ID exists.
						//	Just update internal state.
						#warning LERP FACTOR!
						entities[id].Read( reader, 1 );

					} else {
					
						//	Entity does not exist.
						//	Create new one.
						var ent = world.Spawn(clsId, id);

						ent.Read( reader, 1 );

						//	teleport entity stuff (controllers, physical shapes etc)
						//	to created position
						ent.Teleport( ent.Position, ent.Rotation );

						//ConstructEntity( ent );
						spawned?.Invoke( ent );
					}
				}

				//	Kill all stale entities :
				var staleIDs = oldIDs.Except( newIDs );

				foreach ( var id in staleIDs ) {
					killed?.Invoke( id );
				}


				//
				//	Run FX events
				//
				reader.ExpectFourCC("FXE0", "Bad snapshot");

				int count = reader.ReadInt32();

				for (int i=0; i<count; i++) {
					var fxe = new FXEvent();
					fxe.Read( reader );

					if (fxe.SendCount<=snapshotCountrerDelta) {
						runfx?.Invoke( fxe );
					}
				}

			}
		}
	}
}
