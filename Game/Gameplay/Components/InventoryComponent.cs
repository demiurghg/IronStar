using System;
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
	public class InventoryComponent : IComponent, IEnumerable<Entity>
	{
		Entity activeWeapon = null;
		Entity pendingWeapon = null;
		readonly List<Entity> items = new List<Entity>();

		public Entity ActiveWeapon { get { return activeWeapon; } }
		public bool HasPendingWeapon { get { return pendingWeapon!=null; } }


		public bool SwitchWeapon( Entity weaponEntity )
		{
			if (items.Contains(weaponEntity) && activeWeapon!=weaponEntity) 
			{
				pendingWeapon	=	weaponEntity;
				return true;
			}
			else return false;
		}


		public void FinalizeWeaponSwitch()
		{
			activeWeapon	=	pendingWeapon;
			pendingWeapon	=	null;
		}


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


		public TComponent FindItem<TComponent>( GameState gs, Func<TComponent,bool> predicate )	where TComponent: IComponent
		{
			var aspect = new Aspect().Include<TComponent>();

			foreach ( var e in items )
			{
				if (aspect.Accept(e))
				{
					var c = e.GetComponent<TComponent>();

					if (predicate(c))
					{
						return c;
					}
				}
			}

			return default(TComponent);
		}


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


		public bool ContainsItem( GameState gs, string name )
		{
			return FindItem(gs, name) != null;
		}


		public bool ContainsItem( GameState gs, Aspect itemAspect )
		{
			return FindItem(gs, itemAspect) != null;
		}


		public Entity FindItem ( GameState gs, string name )
		{
			foreach ( var e in items )
			{
				var n = e?.GetComponent<NameComponent>()?.Name;

				if (n==name)
				{
					return e;
				}
			}

			return null;
		}

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
	}
}
