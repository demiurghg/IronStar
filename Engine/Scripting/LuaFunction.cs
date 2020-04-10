using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KopiLua;

namespace Fusion.Scripting {
	
	public class LuaFunction : LuaValue {

		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="index"></param>
		public LuaFunction ( LuaState L, int index ) : base(L, index, Lua.LUA_TFUNCTION)
		{	
		}
	}
}
