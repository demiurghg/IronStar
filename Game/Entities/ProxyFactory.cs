using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics;

namespace IronStar {
	public class ProxyFactory : EntityFactoryContent {

		
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
		string classnameEcs = "";
		bool dirty = true;
		EntityFactoryContent factory = null;


		public override ECS.Entity SpawnECS( ECS.GameState gs )
		{
			if (!string.IsNullOrWhiteSpace(classname))
			{
				return gs.Spawn( classname );
			}
			else
			{
				Log.Warning("ProxyFactory: classname is null or white space, null-entity spawned");
				return null;
			}
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
