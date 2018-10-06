using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KopiLua;

namespace Fusion.Scripting {

	public class LuaScript {

		string		name;
		LuaState	thread;
		LuaValue	threadRef;

		/// <summary>
		/// Creates instance of scriptable object
		/// </summary>
		/// <param name="L"></param>
		/// <param name="loader"></param>
		public LuaScript( LuaState L, byte[] bytecode, string name )
		{
			this.name	=	name;

			int status;

			using ( new LuaStackGuard( L ) ) {
				
				//	create thread :
				thread	=	Lua.LuaNewThread( L );

				//	load bytecode :
				status = Lua.LuaLLoadBuffer( L, bytecode, (uint)bytecode.Length, name );

				LuaException.PrintIfError( L, status );

				if ( status!=0 ) {
					// remove thread
					Lua.LuaPop( L, 1 );
					Terminate();
					return;
				}
					
				//	push object behind function
				LuaObjectTranslator.Get(L).PushObject( L, this );

				//	exchange function and object between threads :
				Lua.LuaXMove( L, thread, 2 );

				//	store thread (keep ref count for gc too) and then remove it from stack :
				threadRef	=	new LuaValue( L, 1, Lua.LUA_TTHREAD );
				Lua.LuaPop( L, 1 );
			}
		}
		


		/// <summary>
		/// Indicated, that script finished execution.
		/// </summary>
		public bool IsFinished {
			get {
				return thread==null;
			}
		}


		/// <summary>
		/// Indicates, that script finished execution with error.
		/// </summary>
		public bool IsError {
			get; private set;
		}


		/// <summary>
		/// Terminates script execution and frees context and threads.
		/// </summary>
		public void Terminate()
		{
			threadRef?.Free();

			threadRef	=	null;
			thread		=	null;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public bool Resume ()
		{
			if (thread==null) {
				return false;
			}

			//	resume execution :
			var status =	Lua.LuaResume( thread, 1 );

			if (status==0) {

				//	execution finished
				Terminate();
				return false;


			} else if (status==Lua.LUA_YIELD) {

				//	execution yielded :
				return true;

			} else {

				//	error happened :
				HandleError( status );
				return false;

			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="status"></param>
		void HandleError (int status)
		{
			IsError	=	true;

			var message = Lua.LuaToString( thread, -1 )?.ToString();
			Lua.LuaPop( thread, 1 );

			switch (status) {
				case Lua.LUA_ERRRUN		:	Log.Error("LUA_ERRRUN : "	 + message ); break;
				case Lua.LUA_ERRSYNTAX	:	Log.Error("LUA_ERRSYNTAX : " + message ); break;
				case Lua.LUA_ERRMEM		:	Log.Error("LUA_ERRMEM : "	 + message ); break;
				case Lua.LUA_ERRERR		:	Log.Error("LUA_ERRERR : "	 + message ); break;
				default: Log.Error("LUA : unknown error status {0}", status); break;
			}

		}
	}
}
