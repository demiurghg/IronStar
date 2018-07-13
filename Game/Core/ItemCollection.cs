using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Core.Mathematics;


namespace IronStar.Core {
	public class ItemCollection : Dictionary<uint,Item> {


		/// <summary>
		/// Creates instance of entity collection.
		/// </summary>
		/// <param name="atoms"></param>
		public ItemCollection ()
		{
		}


		/// <summary>
		/// Gets entity with given ID or null if entity does not exist.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		new public Item this[uint id] {
			get {
				Item e;
				if ( TryGetValue( id, out e ) ) {
					return e;
				} else {
					return null;
				}
			}
		}


		/// <summary>
		/// Gets entity with given ID or null if entity does not exist.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Item GetItem( uint id )
		{
			Item e;
			if ( TryGetValue( id, out e ) ) {
				return e;
			} else {
				return null;
			}
		}



		/// <summary>
		/// Gets items owned by given entity
		/// </summary>
		/// <param name="entityId"></param>
		/// <returns></returns>
		public IEnumerable<Item> GetOwnedItems ( uint entityId )
		{
			return this
				.Select( pair => pair.Value )
				.Where( value => value.Owner == entityId );
		}
	}
}
