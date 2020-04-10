using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KopiLua;

namespace Fusion.Scripting {

	/// <summary>
	/// Once created, stores value at given stack index in registry and removes it from stack.
	/// </summary>
	public class LuaValue : IDisposable {
	
		readonly int refId;
		readonly int type;
		public readonly LuaState L;

		/// <summary>
		/// Stores value at given index in registry
		/// </summary>
		/// <param name="L"></param>
		/// <param name="index"></param>
		public LuaValue ( LuaState L, int index, int luaType = Lua.LUA_TNONE )
		{	
			this.L = L;
			Lua.LuaPushValue( L, index );

			type = Lua.LuaType(L,-1);

			if (luaType!=Lua.LUA_TNONE) {
				if (luaType!=type) {
					LuaUtils.LuaError( L, "LuaValue: expected type {0}, got {1}", LuaUtils.LuaGetTypeName(type), LuaUtils.LuaGetTypeName(luaType) );
				}
			}

			refId  = Lua.LuaLRef( L, Lua.LUA_REGISTRYINDEX );
		}


		public bool IsFunction {
			get {
				return type == Lua.LUA_TFUNCTION;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		public void LuaPushValue ( LuaState L )
		{
			Debug.Assert( this.L == L );
			Lua.LuaRawGetI( L, Lua.LUA_REGISTRYINDEX, refId );
		}


		private bool disposedValue = false; // To detect redundant calls

		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose( bool disposing )
		{
			if ( !disposedValue ) {
				if ( disposing ) {
					Lua.LuaLUnref( L, Lua.LUA_REGISTRYINDEX, refId );
				}

				disposedValue = true;
			}
		}



		public void Free ()
		{
			Dispose();
		}


		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			Dispose( true );
		}

	}
}
