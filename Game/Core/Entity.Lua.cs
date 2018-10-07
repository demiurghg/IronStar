using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Utils;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using System.IO;
using IronStar.SFX;
using Fusion.Engine.Graphics;
using Fusion.Core.Content;
using Fusion.Engine.Common;
using IronStar.Views;
using Fusion.Scripting;
using KopiLua;

namespace IronStar.Core {
	public partial class Entity {

		[LuaApi("vspeed")]
		protected int getVerticalVelocity( LuaState L )
		{
			Lua.LuaPushNumber( L, LinearVelocity.Y );
			return 1;
		}


		[LuaApi("gspeed")]
		protected int getGroundVelocity( LuaState L )
		{
			var groundVelocity = new Vector2( LinearVelocity.X, LinearVelocity.Z );
			Lua.LuaPushNumber( L, groundVelocity.Length() );
			return 1;
		}


		[LuaApi("wpnState")]
		protected int getWeaponState( LuaState L )
		{
			var groundVelocity = new Vector2( LinearVelocity.X, LinearVelocity.Z );
			Lua.LuaPushString( L, WeaponState.ToString().ToLowerInvariant() );
			return 1;
		}


		[LuaApi("traction")]
		protected int getTraction( LuaState L )
		{
			var traction = EntityState.HasFlag( EntityState.HasTraction ) ? 1 : 0;
			Lua.LuaPushBoolean( L, traction );
			return 1;
		}

	}
}
