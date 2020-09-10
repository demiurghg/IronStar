using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.BTCore;
using IronStar.ECS;
using Fusion;

namespace IronStar.BTCore.Actions
{
	public class Print : BTAction
	{
		readonly string message;
		
		public Print( string message )
		{
			this.message = message;
		}

		
		public override BTStatus Update( GameTime gameTime, Entity entity )
		{
			Log.Message("BT Print: #{0} -- {1}", entity.ID, message );

			return BTStatus.Success;
		}
	}
}
