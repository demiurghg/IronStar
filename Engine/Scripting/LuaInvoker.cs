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

			using ( new LuaStackGuard( L ) ) {

				Lua.LuaPushCFunction( L, Print );
				Lua.LuaSetGlobal( L, "print" );

				Lua.LuaPushCFunction( L, DoFile );
				Lua.LuaSetGlobal( L, "dofile" );

				var contentLib = new[]{
					new Lua.LuaLReg("dofile",	DoFile			),
					new Lua.LuaLReg("loader",	LoaderContent	),
					new Lua.LuaLReg(null	,	null			),
				};

				Lua.LuaLRegister( L, "content", contentLib );
				Lua.LuaPop( L, 1 );

				ExecuteString( L, "table.insert( package.loaders, content.loader );" );

				ExecuteFile( L, "init" );
			}

			Game.Instance.Reloading += (s,e) => ExecuteFile( L, "reload" );

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
		public static bool ExecuteString ( LuaState L, string commandLine )
		{
			using ( new LuaStackGuard( L ) ) {

				var status = Lua.LuaLLoadBuffer( L, commandLine, (uint)commandLine.Length, "[string]" );

				if (status!=0) {
					Log.Error( "error loading string '{0}'", commandLine );
					return false;
				}

				status = Lua.LuaPCall(L, 0, 0, 0);

				if (status!=0) {
					var error = Lua.LuaToString(L,-1).ToString();
					Lua.LuaPop( L, 1 );
					Log.Error( error );
					return false;
				}

				return true;
			}
		}



		/// <summary>
		/// Executes given file from content.
		/// Returns true if succeded. 
		/// False otherwice and prints error message.
		/// </summary>
		/// <param name="L"></param>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static bool ExecuteFile ( LuaState L, string fileName )
		{
			using ( new LuaStackGuard( L ) ) {

				int n		 = Lua.LuaGetTop(L);

				if (!content.Exists(fileName)) {
					Log.Error( "file '{0}' does not exist", fileName );
					return false;
				}
				
				var bytecode = content.Load<byte[]>( fileName );

				var status = Lua.LuaLLoadBuffer( L, bytecode, (uint)bytecode.Length, fileName );

				if (status!=0) {
					Log.Error( "error loading file '{0}'", fileName );
					return false;
				}

				status = Lua.LuaPCall(L, 0, 0, 0);

				if (status!=0) {
					var error = Lua.LuaToString(L,-1).ToString();
					Lua.LuaPop( L, 1 );
					Log.Error( error );
					return false;
				}

				return true;
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



		static int LoaderContent ( LuaState L )
		{
			using ( new LuaStackGuard( L, 1 ) ) {

				var filename = Lua.LuaLCheckString(L, 1)?.ToString();

				Lua.LuaGetGlobal( L, "package" );
				Lua.LuaGetField( L, -1, "path" );

				var searchPath = Lua.LuaToString( L, -1 ).ToString();

				Lua.LuaPop( L, -2 );

				byte[] bytecode;

				foreach ( var pattern in searchPath.Split(';') ) {

					var path = pattern.Replace( "?", filename );

					if (content.TryLoad( path, out bytecode )) {
						
						var status = Lua.LuaLLoadBuffer( L, bytecode, (uint)bytecode.Length, filename );

						if (status!=0) {
							Lua.LuaLError( L, "error loading file '{0}'", filename);
						}

						return 1;
					}
				}

				Lua.LuaPushFString( L, "no file {0}", filename );

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
