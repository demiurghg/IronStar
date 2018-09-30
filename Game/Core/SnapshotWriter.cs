using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using IronStar.SFX;

namespace IronStar.Core {
	public class SnapshotWriter {

		int sendSnapshotCounter = 1;

		/// <summary>
		/// 
		/// </summary>
		public SnapshotWriter ()
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="snapshotStream"></param>
		/// <param name="entities"></param>
		/// <param name="fxEvents"></param>
		public void Write ( Stream snapshotStream, IStorable header, Dictionary<uint,Entity> entities, Dictionary<uint,Item> items, List<FXEvent> fxEvents )
		{
			using ( var writer = new BinaryWriter( snapshotStream ) ) {

				header.Write( writer );

				var entityArray = entities.OrderBy( pair => pair.Value.ID ).Select( pair1 => pair1.Value ).ToArray();
				var itemsArray  = items   .OrderBy( pair => pair.Value.ID ).Select( pair1 => pair1.Value ).ToArray();

				writer.Write( sendSnapshotCounter );
				sendSnapshotCounter++;

				//
				//	Write fat entities :
				//
				writer.WriteFourCC("ENT1");
				writer.Write( entityArray.Length );

				foreach ( var ent in entityArray ) {
					writer.Write( ent.ID );
					writer.Write( ent.ClassID );
					ent.Write( writer );
				}

				//
				//	Write items :
				//
				writer.WriteFourCC("ITM1");
				writer.Write( itemsArray.Length );

				foreach ( var item in itemsArray ) {
					writer.Write( item.ID );
					writer.Write( item.ClassID );
					item.Write( writer );
				}


				//
				//	Write FX events :
				//
				writer.WriteFourCC("FXE1");
				writer.Write( fxEvents.Count );
			
				foreach ( var fxe in fxEvents ) {
					fxe.SendCount ++;
					fxe.Write( writer );
				}

				fxEvents.RemoveAll( fx => fx.SendCount >= 3 );
			}
		}
	}
}
