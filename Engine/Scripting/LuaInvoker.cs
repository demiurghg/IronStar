using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Configuration;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using System.Diagnostics;
using Fusion.Engine;
using Fusion.Engine.Server;
using System.Reflection;
using System.ComponentModel;
using KopiLua;
using Fusion.Core.Content;

namespace Fusion.Scripting {
	public partial class LuaInvoker : IDisposable {

		public readonly LuaState L;
		object lockObject = new object();

		readonly ContentManager content;


		/// <summary>
		/// Creates instance of Invoker.
		/// </summary>
		/// <param name="game">Game instance</param>
		public LuaInvoker ( ContentManager content )
		{
			this.content	=	content;
			
			L	=	Lua.LuaOpen();
			Lua.LuaLOpenLibs(L);

			Lua.LuaPushCFunction( L, LuaBPrint );
			Lua.LuaSetGlobal( L, "print" );
		}



		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
		{
			if ( !disposedValue ) {
				if ( disposing ) {
					Lua.LuaClose(L);
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose( true );
		}
		#endregion


		/// <summary>
		/// 
		/// </summary>
		/// <param name="apiName"></param>
		/// <param name="obj"></param>
		/// <param name="methods"></param>
		public void ExposeApi ( object target, string apiName )
		{
			if (apiName==null) {
				throw new ArgumentNullException("apiName");
			}

			using ( new LuaStackGuard( L ) ) {
				LuaUtils.PushObject( L, target );
				Lua.LuaSetGlobal(L, apiName);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="commandLine"></param>
		public string ExecuteString ( string commandLine )
		{
			int errcode;

			using ( new LuaStackGuard(L) ) {
				
				//errcode = Lua.LuaLLoadString( L, commandLine);

				errcode = Lua.LuaLLoadBuffer( L, commandLine, (uint)commandLine.Length, "cmdline");

				if (errcode!=0) {
					throw new LuaException(L, errcode);
				}


				errcode = Lua.LuaPCall(L,0,1,0);

				if (errcode!=0) {
					throw new LuaException(L, errcode);
				} else {
					
					var result = Lua.LuaToString(L,-1)?.ToString();
					Lua.LuaPop(L, 1);

					return result;

				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		public void ExecuteFile ( string path )
		{
			LuaUtils.LuaDoFile( L, content.Load<string>(path), path );
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Commands :
		 * 
		-----------------------------------------------------------------------------------------*/

		private static int LuaBPrint (LuaState L) {

			StringBuilder sb = new StringBuilder(256);

			sb.Append("[Lua] ");

			int n = Lua.LuaGetTop(L);  /* number of arguments */
			int i;
			Lua.LuaGetGlobal( L, "tostring" );
			for ( i=1; i<=n; i++ ) {
				CharPtr s;
				Lua.LuaPushValue( L, -1 );  /* function to be called */
				Lua.LuaPushValue( L, i );   /* value to print */
				Lua.LuaCall( L, 1, 1 );
				s = Lua.LuaToString( L, -1 );  /* get result */
				if ( s == null )
					return Lua.LuaLError( L, Lua.LUA_QL( "tostring" ) + " must return a string to " +
										 Lua.LUA_QL( "print" ) );
				if ( i > 1 ) sb.Append( "\t" );
				sb.Append( s );
				Lua.LuaPop( L, 1 );  /* pop result */
			}

			Log.Verbose( sb.ToString() );

			return 0;
		}
	}
}
