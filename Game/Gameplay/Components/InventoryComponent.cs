﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public enum InventoryFlags
	{
		None			=	0x00,
		InfiniteAmmo	=	0x01,
	}

	public class InventoryComponent : IComponent, IEnumerable<Entity>
	{
		public InventoryFlags Flags { get { return flags; } }
		InventoryFlags flags;

		Entity activeWeapon = null;
		Entity pendingWeapon = null;
		readonly List<Entity> items = new List<Entity>();

		public InventoryComponent( InventoryFlags flags = InventoryFlags.None )
		{
			this.flags	=	flags;
		}

		/// <summary>
		/// Gets active weapon entity. Could be null.
		/// </summary>
		public Entity ActiveWeapon { get { return activeWeapon; } }

		/// <summary>
		/// Indicates, that inventory has pending weapon.
		/// </summary>
		public bool HasPendingWeapon { get { return pendingWeapon!=null; } }

		public void Load( GameState gs, Stream stream ) {}
		public void Save( GameState gs, Stream stream ) {}

		public IEnumerator<Entity> GetEnumerator()
		{
			return ( (IEnumerable<Entity>)items ).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<Entity>)items ).GetEnumerator();
		}


		/// <summary>
		/// Sets provided weapon entity as pending weapon.
		/// </summary>
		/// <param name="weaponEntity">Weapon to set</param>
		/// <returns>Return false, if weapon is already current or does not exist in inventory. True otherwice.</returns>
		public bool SwitchWeapon( Entity weaponEntity )
		{
			if (items.Contains(weaponEntity) && activeWeapon!=weaponEntity) 
			{
				pendingWeapon	=	weaponEntity;
				return true;
			}
			else return false;
		}


		/// <summary>
		/// Completes switching of the weapon.
		/// </summary>
		public void FinalizeWeaponSwitch()
		{
			activeWeapon	=	pendingWeapon;
			pendingWeapon	=	null;
		}


		/// <summary>
		/// Adds entity to the inventory
		/// </summary>
		/// <param name="entity">Entity to add</param>
		/// <returns>True is item successfully added. False is item is already in inventory</returns>
		public bool AddItem( Entity entity )
		{
			if (!items.Contains(entity)) 
			{
				items.Add(entity);
				return true;
			} 
			else
			{
				Log.Warning("InventoryComponent : attempt to put the same entity twice");
				return false;
			}
		}


		/// <summary>
		/// Removes item from inventory
		/// </summary>
		/// <param name="entity">Item to remove</param>
		/// <returns>True if item was in inventory</returns>
		public bool RemoveItem( Entity entity )
		{
			if (items.Contains(entity)) 
			{
				items.Remove(entity);
				return true;
			} 
			else
			{
				Log.Warning("InventoryComponent : attempt to remove non-existing entity");
				return false;
			}
		}


		/// <summary>
		/// Finds item by inclusive component and predicate on given component.
		/// </summary>
		/// <typeparam name="TComponent">Component type to search</typeparam>
		/// <param name="gs">Game state</param>
		/// <param name="predicate">Preicate</param>
		/// <param name="component">Output component</param>
		/// <returns>True if item found, false otherwice</returns>
		public Entity FindItem<TComponent>( GameState gs, Func<TComponent,bool> predicate, out TComponent component )	where TComponent: IComponent
		{
			var aspect = new Aspect().Include<TComponent>();
			component = default(TComponent);

			foreach ( var e in items )
			{
				if (aspect.Accept(e))
				{
					var c = e.GetComponent<TComponent>();

					if (predicate(c))
					{
						component = c;
						return e;
					}
				}
			}

			return null;
		}


		/// <summary>
		/// Finds item by inclusive component and predicate on given component.
		/// </summary>
		/// <typeparam name="TComponent1"></typeparam>
		/// <typeparam name="TComponent2"></typeparam>
		/// <param name="gs"></param>
		/// <param name="predicate"></param>
		/// <param name="component1"></param>
		/// <param name="component2"></param>
		/// <returns></returns>
		public Entity FindItem<TComponent1,TComponent2>( GameState gs, Func<TComponent1,TComponent2,bool> predicate, 
			out TComponent1 component1, 
			out TComponent2 component2 )
			where TComponent1: IComponent 
			where TComponent2: IComponent
		{
			var aspect	= new Aspect().Include<TComponent1,TComponent2>();

			component1	= default(TComponent1);
			component2	= default(TComponent2);

			foreach ( var e in items )
			{
				if (aspect.Accept(e))
				{
					var c1 = e.GetComponent<TComponent1>();
					var c2 = e.GetComponent<TComponent2>();

					if (predicate(c1,c2))
					{
						component1 = c1;
						component2 = c2;
						return e;
					}
				}
			}

			return null;
		}


		public Entity FindItem<TComponent>( GameState gs, Func<TComponent,bool> predicate )
			where TComponent: IComponent 
		{
			TComponent c;
			return FindItem( gs, predicate, out c );
		}


		public Entity FindItem<TComponent1,TComponent2>( GameState gs, Func<TComponent1,TComponent2,bool> predicate )
			where TComponent1: IComponent 
			where TComponent2: IComponent
		{
			TComponent1 c1;
			TComponent2 c2;
			return FindItem( gs, predicate, out c1, out c2 );
		}


		/// <summary>
		/// Finds first item by aspect.
		/// </summary>
		/// <param name="gs">Gamestate</param>
		/// <param name="itemAspect">Entity aspect</param>
		/// <returns>First occurance of the item, that meets given aspect. Null otherwice.</returns>
		public Entity FindItem ( GameState gs, Aspect itemAspect )
		{
			foreach ( var e in items )
			{
				if (itemAspect.Accept(e))
				{
					return e;
				}
			}

			return null;
		}


		public bool ContainsItem<TComponent>( GameState gs, Func<TComponent,bool> predicate ) where TComponent: IComponent
		{
			TComponent c;
			return FindItem(gs, predicate, out c)!=null;
		}


		public bool ContainsItem<TComponent1,TComponent2>( GameState gs, Func<TComponent1,TComponent2,bool> predicate ) 
			where TComponent1: IComponent 
			where TComponent2: IComponent
		{
			return FindItem(gs, predicate)!=null;
		}


		public bool ContainsItem( GameState gs, Aspect itemAspect )
		{
			return FindItem(gs, itemAspect) != null;
		}
	}
}
