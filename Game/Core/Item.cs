using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.Core;
using IronStar.Entities;
using Fusion.Core;
using System.IO;

namespace IronStar.Core {


	/// <summary>
	/// 
	/// </summary>
	public class Item : JsonObject {
		
		public readonly uint ID;

		public readonly short ClassID;

		public readonly string NiceName;

		internal bool Stale = false;

		public short Count;

		public short MaxCount;

		/// <summary>
		/// Gets and sets item owner.
		/// If owner is zero or owner is dead, 
		/// item will be removed from the world.
		/// </summary>
		public uint Owner { get; set; }

		/// <summary>
		/// Creates new instance of Item
		/// </summary>
		/// <param name="clsid"></param>
		protected Item ( uint id, short clsid, ItemFactory factory )
		{
			ID			=	id;
			ClassID		=	clsid;
			NiceName	=	factory.NiceName;
		}

		/// <summary>
		/// Attempts to use item as weapon.
		/// Returns FALSE if item could not be used as weapon. TRUE otherwise.
		/// </summary>
		public virtual bool Attack ( Entity attacker ) { return false; }

		/// <summary>
		/// Called when holding entity attempts to activate item.
		/// Returns FALSE if item could not be activated. TRUE otherwise.
		/// </summary>
		public virtual bool Activate ( Entity target ) { return false; }

		/// <summary>
		/// Called when holding entity attempts to switch weapon to next one.
		/// Returns FALSE if item could not be switched. TRUE otherwise.
		/// </summary>
		public virtual bool Switch ( Entity target, uint nextItem ) { return false; }

		/// <summary>
		/// Attempts to apply current item on another item.
		/// Return TRUE if succeded, FALSE otherwice, i.e. not applicable item (medkit on weapon)
		/// </summary>
		public virtual bool Apply ( Item target ) { return false; }

		/// <summary>
		/// Indicates that given item could not be used any more and must be removed.
		/// </summary>
		/// <returns></returns>
		public virtual bool IsDepleted () { return false; }

		/// <summary>
		/// Indicates that given item is in use and could not be replaced, reactivated, dropped, etc.
		/// </summary>
		/// <returns></returns>
		public virtual bool IsBusy () { return false; }

		/// <summary>
		/// Called when player attempts to picks the item up.
		/// This method return false, if item decided not to be added
		/// and true otherwice.
		/// </summary>
		public virtual bool Pickup ( Entity player ) { return false; }

		/// <summary>
		/// Called when player or monster drops the item.
		/// On drop, creates new entity.
		/// </summary>
		public virtual Entity Drop () { return null; }

		/// <summary>
		/// Updates internal item state
		/// </summary>
		public virtual void Update ( GameTime gameTime ) {}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public virtual void Write ( BinaryWriter writer ) 
		{
			writer.Write( Owner );
			writer.Write( MaxCount );
			writer.Write( Count );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public virtual void Read ( BinaryReader reader ) 
		{
			Owner		=	reader.ReadUInt32();
			MaxCount	=	reader.ReadInt16();
			Count		=	reader.ReadInt16();
		}
	}
}