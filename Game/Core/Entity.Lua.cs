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

		[LuaApi("get_vspeed")]
		protected int getVerticalVelocity( LuaState L )
		{
			Lua.LuaPushNumber( L, LinearVelocity.Y );
			return 1;
		}


		[LuaApi("get_gspeed")]
		protected int getGroundVelocity( LuaState L )
		{
			var groundVelocity = new Vector2( LinearVelocity.X, LinearVelocity.Z );
			Lua.LuaPushNumber( L, groundVelocity.Length() );
			return 1;
		}


		[LuaApi("get_weapon_state")]
		protected int getWeaponState( LuaState L )
		{
			var groundVelocity = new Vector2( LinearVelocity.X, LinearVelocity.Z );
			Lua.LuaPushString( L, WeaponState.ToString().ToLowerInvariant() );
			return 1;
		}


		[LuaApi("has_traction")]
		protected int getTraction( LuaState L )
		{
			Lua.LuaPushBoolean( L, EntityState.HasFlag( EntityState.HasTraction ) ? 1 : 0 );
			return 1;
		}


		[LuaApi("is_strafe_right")]
		protected int isStrafeRight( LuaState L )
		{
			Lua.LuaPushBoolean( L, EntityState.HasFlag( EntityState.StrafeRight ) ? 1 : 0 );
			return 1;
		}

		[LuaApi("is_strafe_left")]
		protected int isStrafeLeft( LuaState L )
		{
			Lua.LuaPushBoolean( L, EntityState.HasFlag( EntityState.StrafeLeft ) ? 1 : 0 );
			return 1;
		}

		[LuaApi("is_turn_right")]
		protected int isTurnRight( LuaState L )
		{
			Lua.LuaPushBoolean( L, EntityState.HasFlag( EntityState.TurnRight ) ? 1 : 0 );
			return 1;
		}

		[LuaApi("is_turn_left")]
		protected int isTurnLeft( LuaState L )
		{
			Lua.LuaPushBoolean( L, EntityState.HasFlag( EntityState.TurnLeft ) ? 1 : 0 );
			return 1;
		}

	}
}
