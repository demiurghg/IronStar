using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public sealed class EntityRef
	{
		readonly GameState gs = null;
		readonly uint id = 0;

		EntityRef( Entity e )
		{
			throw new NotImplementedException();
			if (e!=null)
			{
				this.gs	=	e.gs;
				this.id	=	e.ID;
			}
		}


		public Entity Target 
		{
			get 
			{
				return gs?.GetEntity(id);
			}
		}


		public static implicit operator Entity ( EntityRef entityRef )
		{
			return entityRef.Target;
		}

		public static implicit operator EntityRef ( Entity entity )
		{
			return new EntityRef(entity);
		}


		public void AddComponent( IComponent component ) { Target.AddComponent(component); }
		public void RemoveComponent( IComponent component ) { Target.RemoveComponent(component); }
		public void RemoveComponent<TComponent>() where TComponent: IComponent { Target.RemoveComponent<TComponent>(); }
		public bool ContainsComponent<TComponent>() where TComponent: IComponent { return Target.ContainsComponent<TComponent>(); }
		
		public TComponent GetComponent<TComponent>() where TComponent: IComponent 
		{ 
			return Target==null ? default(TComponent) : Target.GetComponent<TComponent>(); 
		}
	}
}
