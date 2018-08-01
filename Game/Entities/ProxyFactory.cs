using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics;
using IronStar.Core;

namespace IronStar.Entities {
	public class ProxyFactory : EntityFactory {

		
		[AEClassname("entities")]
		public string Classname { 
			get { return classname; }
			set {
				if (classname!=value) {
					classname = value;
					dirty = true;
				}
			}
		}


		string classname = "";
		bool dirty = true;
		EntityFactory factory = null;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="world"></param>
		/// <returns></returns>
		public override Entity Spawn( uint id, short clsid, GameWorld world )
		{
			if (string.IsNullOrWhiteSpace(Classname)) {
				Log.Warning("ProxyFactory: classname is null or white space, null-entity spawned");
				return null;
			}

			factory = world.GetFactoryByName(Classname);

			if (factory==null) {
				Log.Warning("ProxyFactory: failed to get entity factory for '{0}', null-entity spawned", Classname);
				return null;
			}

			return factory.Spawn( id, clsid, world );
		}



		public override void Draw( DebugRender dr, Matrix transform, Color color, bool selected )
		{
			if (factory==null) {
				base.Draw( dr, transform, color, selected );
			} else {
				factory.Draw( dr, transform, color, selected );
			}
		}


	}
}
