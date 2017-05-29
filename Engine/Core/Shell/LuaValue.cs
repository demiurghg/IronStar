using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KopiLua;

namespace Fusion.Core.Shell {
	
	public class LuaValue : IDisposable {
	
		readonly int refId;
		public readonly LuaState L;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="index"></param>
		public LuaValue ( LuaState L, int index )
		{	
			this.L = L;
			Lua.LuaPushValue( L, index );
			refId  = Lua.LuaLRef( L, Lua.LUA_REGISTRYINDEX );
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



		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="nArgs"></param>
		public void SafeCall ( LuaState L, int nArgs )
		{
			//Debug.Assert( this.L == L );
			//LuaPushValue( L );
			//int status = Lua.LuaPCall( L, nArgs, 0, 0 );
			//LuaException.PrintIfError( L, status );
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


		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			Dispose( true );
		}

	}
}
