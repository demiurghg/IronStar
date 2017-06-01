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
using System.Threading;
using Fusion.Core.Mathematics;

namespace Fusion.Core.Shell {
	public static class LuaUtils {

		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="frmt"></param>
		/// <param name="args"></param>
		public static void LuaError ( LuaState L, string frmt, params object[] args )
		{
			var text = string.Format(frmt, args);

			var ar = new LuaDebug();

			string func_name = "";

			if (Lua.LuaGetStack(L, 0, ref ar)!=0) {
				Lua.LuaGetInfo(L, "nSlu", ref ar);
				if (ar.name!=null) {
					func_name = ar.name.ToString();
					func_name += "()";
				}
			}

			if (Lua.LuaGetStack(L, 1, ref ar)!=0) {
				Lua.LuaGetInfo(L, "nSlu", ref ar);
				Lua.LuaPushString(L, string.Format("{0}:{1}: {2}: {3}", ar.short_src, ar.currentline, func_name, text));
			} else {
				Lua.LuaPushString(L, text);
			}

			Lua.LuaError(L);
		}



		public static string LuaGetTypeName ( int type )
		{
            switch (type) {
                case Lua.LUA_TNIL			: return "nil"			; break;
                case Lua.LUA_TNUMBER		: return "number"		; break;
                case Lua.LUA_TSTRING		: return "string"		; break;
                case Lua.LUA_TTABLE			: return "table"		; break;
                case Lua.LUA_TFUNCTION		: return "function"		; break;
                case Lua.LUA_TBOOLEAN		: return "boolean"		; break;
                case Lua.LUA_TUSERDATA		: return "userdata"		; break;
                case Lua.LUA_TTHREAD		: return "thread"		; break;
                case Lua.LUA_TLIGHTUSERDATA	: return "lightuserdata"; break;
                default: return "unknown";
            }
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="text"></param>
		/// <param name="fileName"></param>
		public static void LuaDoFile ( LuaState L, string text, string path )
		{
			using ( new LuaStackGuard( L ) ) {

				int status;
				
				int fnameindex = Lua.LuaGetTop(L) + 1; 
				Lua.LuaPushFString(L, "@%s", path);

				//	load file :
				status = Lua.LuaLLoadBuffer( L, text, (uint)text.Length, Lua.LuaToString(L, -1));

				Lua.LuaRemove(L, fnameindex);

				LuaException.ThrowIfError( L, status );

				//	call chunk :
				status = Lua.LuaPCall( L, 0, Lua.LUA_MULTRET, 0);

				LuaException.ThrowIfError( L, status );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="L"></param>
		/// <param name="index"></param>
		/// <param name="argument"></param>
		/// <returns></returns>
		public static T Expect<T>( LuaState L, int index, string argument ) where T: class
		{
			return LuaObject.LuaTo<T>( L, index );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="obj"></param>
		public static void PushObject( LuaState L, object obj, bool allowGC = false )
		{
			LuaObject.LuaPushObject( L, obj, allowGC );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="index"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public static string ExpectString ( LuaState L, int index, string argument = null )
		{
			var s = Lua.LuaToString(L,index);

			if (s==null) {
				LuaError( L, "{0} at index {1} is not a string", argument ?? "value", index );
				return "";
			}

			return s.ToString();
		}


		public static Color ExpectHexColorString ( LuaState L, int index, string argument = null )
		{
			var s = ExpectString( L, index, argument );

			if (!s.StartsWith("#")) {
				LuaError(L, "hex-coded color string must contain leading '#'");
			}
			
			try {
				return Color.FromHexString(s);
			} catch ( Exception e ) {
				LuaError(L, e.Message);
			}
			return Color.Zero;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="color"></param>
		public static void PushHexColorString ( LuaState L, Color color )
		{
			Lua.LuaPushString( L, color.ToHexString() );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="index"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public static float ExpectFloat ( LuaState L, int index, string argument = null )
		{
			if (Lua.LuaIsNumber(L,index)==0) {
				LuaError( L, "{0} at index {1} is not a number", argument ?? "value", index );
				return 0;
			}

			return (float)Lua.LuaToNumber(L,index);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="index"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public static int ExpectInteger ( LuaState L, int index, string argument = null )
		{
			if (Lua.LuaIsNumber(L,index)==0) {
				LuaError( L, "{0} at index {1} is not a number", argument ?? "value", index );
				return 0;
			}

			return Lua.LuaToInteger(L,index);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="index"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public static bool ExpectBoolean ( LuaState L, int index, string argument = null )
		{
			if (!Lua.LuaIsBoolean(L,index)) {
				LuaError( L, "{0} at index {1} is not a boolean", argument ?? "value", index );
				return false;
			}

			return (Lua.LuaToBoolean(L,index)!=0);
		}

	}
}
