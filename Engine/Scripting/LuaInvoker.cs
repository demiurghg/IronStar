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
using Fusion.Core;

namespace Fusion.Scripting {
	public static class LuaInvoker {


		/// <summary>
		/// Creates instance of LuaState with Fusion related stuff.
		/// </summary>
		/// <returns></returns>
		public static LuaState CreateLuaState ()
		{
			var L	=	Lua.LuaOpen();
			Lua.LuaLOpenLibs(L);

			Lua.LuaPushCFunction( L, Print );
			Lua.LuaSetGlobal( L, "print" );

			Lua.LuaPushCFunction( L, DoFile );
			Lua.LuaSetGlobal( L, "dofile" );

			var contentLib = new[]{
				new Lua.LuaLReg("dofile",  DoFile),
				new Lua.LuaLReg("load",  Load),
				new Lua.LuaLReg(null, null),
			};

			Lua.LuaLRegister( L, "content", contentLib );
			Lua.LuaPop( L, 1 );

			return L;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="apiName"></param>
		/// <param name="obj"></param>
		/// <param name="methods"></param>
		public static void ExposeApi ( LuaState L, object target, string apiName )
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
		[Obsolete]
		public static string ExecuteString ( LuaState L, string commandLine )
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

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Commands :
		 * 
		-----------------------------------------------------------------------------------------*/

		static ContentManager content {
			get { return Game.Instance.Content; }
		}


		static int DoFile ( LuaState L )
		{
			using ( new LuaStackGuard( L ) ) {
				var filename = Lua.LuaLCheckString(L, 1)?.ToString();
				int n		 = Lua.LuaGetTop(L);

				if (!content.Exists(filename)) {
					Lua.LuaLError( L, "file '{0}' does not exist", filename);
				}
				
				var bytecode = content.Load<byte[]>( filename );

				var status = Lua.LuaLLoadBuffer( L, bytecode, (uint)bytecode.Length, filename );

				if (status!=0) {
					Lua.LuaLError( L, "error loading file '{0}'", filename);
				}

				Lua.LuaCall(L, 0, Lua.LUA_MULTRET);

				return Lua.LuaGetTop(L) - n;
			}
		}


		static int Load ( LuaState L )
		{
			using ( new LuaStackGuard( L, 1 ) ) {
				var filename = Lua.LuaLCheckString(L, 1)?.ToString();
				int n		 = Lua.LuaGetTop(L);

				if (!content.Exists(filename)) {
					Lua.LuaLError( L, "file '{0}' does not exist", filename);
				}
				
				var bytecode = content.Load<byte[]>( filename );

				var status = Lua.LuaLLoadBuffer( L, bytecode, (uint)bytecode.Length, filename );

				if (status!=0) {
					Lua.LuaLError( L, "error loading file '{0}'", filename);
				}

				return 1;
			}
		}


		static int Print (LuaState L) {

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
