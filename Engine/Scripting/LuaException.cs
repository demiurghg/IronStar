﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using KopiLua;

namespace Fusion.Scripting
{
	[Serializable]
	public class LuaException : Exception {

		public LuaException ()
		{
		}
		
		public LuaException ( string message ) : base( message )
		{
		}

		static string GetErrMessage (LuaState L, int errcode)
		{
			var message = Lua.LuaToString(L,-1)?.ToString();
			Lua.LuaPop(L,1);

			switch (errcode) {
				case Lua.LUA_ERRRUN		:	return "Runtime error : " + message;
				case Lua.LUA_ERRSYNTAX	:	return "Syntex error : " + message;
				case Lua.LUA_ERRMEM		:	return "Memory allocation error : " + message;
				case Lua.LUA_ERRERR		:	return "Error while running the message handler : " + message;
				default: return "Internal API error. Probably exception was thrown." + message;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="errcode"></param>
		public static void ThrowIfError ( LuaState L, int errcode )
		{
			if (errcode>1) {
				throw new LuaException( L, errcode );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="errcode"></param>
		public static bool PrintIfError ( LuaState L, int errcode )
		{
			if (errcode>1) {
				Log.Error( GetErrMessage(L, errcode ) );
				return true;
			} else {
				return false;
			}
		}

		/// <summary>
		/// Create exception from error message on stack
		/// </summary>
		/// <param name="L"></param>
		public LuaException ( LuaState L, int errcode ) : base( GetErrMessage(L,errcode) )
		{
		}

		public LuaException ( string format, params object[] args ) : base( string.Format(format, args) )
		{
			
		}

		public LuaException ( string message, Exception inner ) : base( message, inner )
		{
		}
	}
}