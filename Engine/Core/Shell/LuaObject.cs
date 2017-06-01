﻿using System;
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
	class LuaObject {
	
		#warning add LuaState.Tag and use it to store object translation context

		static int counter = 0;
		readonly static Dictionary<int,LuaObject> objects = new Dictionary<int, LuaObject>();

		class Value {
			public LuaNativeFunction function;
			public PropertyInfo property;
			public bool readOnly;
		}

		readonly object target;
		readonly Type type;
		readonly Dictionary<string,Value> values = new Dictionary<string, Value>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		public LuaObject ( object target )
		{
			this.target	=	target;

			type		=	target.GetType();
			var flags	=	BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;

			//
			//	retrieve methods :
			//
			foreach ( var mi in type.GetMembers(flags) ) {

				try {

					var luaAttr		= mi.GetCustomAttribute<LuaApiAttribute>();
					var cfgAttr		= mi.GetCustomAttribute<ConfigAttribute>();
					var luaRO		= mi.HasAttribute<LuaReadonly>();

					if (mi is MethodInfo) {

						if (luaAttr!=null) {

							var method	=	(MethodInfo)mi;

							var name	=	luaAttr.Name;
							var func	=	(LuaNativeFunction)method.CreateDelegate( typeof(LuaNativeFunction), target );

							var value	=	new Value { function = func, property = null, readOnly = true };

							values.Add( name, value );
						}

					} else if (mi is PropertyInfo) {

						if (luaAttr!=null || cfgAttr!=null) {

							var name	=	(luaAttr == null) ? mi.Name : luaAttr.Name;
							var prop	=	(PropertyInfo)mi;

							var value	=	new Value { function = null, property = prop, readOnly = luaRO };

							values.Add( name, value );
						}				
					} 

				} catch (Exception e) {
					Log.Warning("Error reflecting member '{0}.{1}': {2}", target.GetType().Name, mi.Name, e.Message );
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="name"></param>
		public int LuaMetaIndex ( LuaState L )
		{
			Value value;

			var id	=	Lua.LuaToUserData(L,1);
			var key	=	LuaUtils.ExpectString(L, 2, "method or property name");

			if (values.TryGetValue( key, out value )) {

				if (value.function!=null) {

					Lua.LuaPushCFunction( L, value.function );

				} else {
					
					var prop = value.property;

					if (prop.PropertyType.IsEnum) {
						Lua.LuaPushString( L, prop.GetValue(target).ToString() );
					} else 
					if (prop.PropertyType==typeof(int)) {
						Lua.LuaPushInteger( L, (int)prop.GetValue(target) );
					} else
					if (prop.PropertyType==typeof(float)) {
						Lua.LuaPushNumber( L, (float)prop.GetValue(target) );
					} else
					if (prop.PropertyType==typeof(string)) {
						Lua.LuaPushString( L, (string)prop.GetValue(target) );
					} else
					if (prop.PropertyType==typeof(Color)) {
						LuaUtils.PushHexColorString( L, (Color)prop.GetValue(target) );
					} else
					if (prop.PropertyType==typeof(bool)) {
						Lua.LuaPushBoolean( L, ((bool)prop.GetValue(target)) ? 1 : 0 );
					} else
					if (prop.PropertyType==typeof(LuaValue)) {
						var propValue = (LuaValue)prop.GetValue( target );
						propValue.LuaPushValue( L );
					} else {
						Lua.LuaPushNil(L);
					}
				}

			} else {
				LuaUtils.LuaError(L, "Unknown method or property: {0}", key);
			}

			return 1;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="name"></param>
		public int LuaMetaNewIndex ( LuaState L )
		{
			Value value;

			var id	=	Lua.LuaToUserData(L,1);
			var key	=	LuaUtils.ExpectString(L, 2, "method or property name");

			if (values.TryGetValue( key, out value )) {

				if (value.readOnly) {

					LuaUtils.LuaError(L, "Field '{0}' is readonly", key);

				} else {
					
					var prop = value.property;

					if (prop.PropertyType.IsEnum) {
						var s = LuaUtils.ExpectString(L,3);
						try {
							prop.SetValue( target, Enum.Parse(prop.PropertyType, s, true ) );
						} catch ( ArgumentException ae ) {
							LuaUtils.LuaError(L, "Bad enum value {0} for enum type {1}", s, prop.PropertyType );
						}
					} else 
					if (prop.PropertyType==typeof(int)) {
						prop.SetValue( target, LuaUtils.ExpectInteger(L,3) );
					} else
					if (prop.PropertyType==typeof(float)) {
						prop.SetValue( target, LuaUtils.ExpectFloat(L,3) );
					} else
					if (prop.PropertyType==typeof(string)) {
						prop.SetValue( target, LuaUtils.ExpectString(L,3) );
					} else
					if (prop.PropertyType==typeof(bool)) {
						prop.SetValue( target, LuaUtils.ExpectBoolean(L,3) );
					} else
					if (prop.PropertyType==typeof(Color)) {
						prop.SetValue( target, LuaUtils.ExpectHexColorString(L,3) );
					} else
					if (prop.PropertyType==typeof(LuaValue)) {
						var oldPropValue = (LuaValue)prop.GetValue( target );
						oldPropValue?.Dispose();
						if (Lua.LuaIsNil(L,3)) {
							prop.SetValue( target, null );
						} else {
							prop.SetValue( target, new LuaValue(L,3) );
						}
					} else {
						LuaUtils.LuaError(L, "Lua API: property '{0}' has unsupported type '{1}'", key, prop.PropertyType.Name);
					}
				}

			} else {
				LuaUtils.LuaError(L, "Unknown Lua API method or property: {0}", key);
			}

			return 1;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static T LuaTo<T> ( LuaState L, int index ) where T: class
		{
			var data = Lua.LuaToUserData(L, index) as byte[];

			if ( data == null ) {
				LuaUtils.LuaError( L, "value at index {0} is not a user data", index );
				return default(T);
			}

			int id = BitConverter.ToInt32(data,0);

			lock (objects) {
				LuaObject obj;
				if (objects.TryGetValue(id, out obj)) {
					
					var target = obj.target as T;

					if (target==null) {
						LuaUtils.LuaError( L, "value at index {0} is not a {1}, got {2}", index, typeof(T), obj.target.GetType() );
					}

					return target;
				} else {
					LuaUtils.LuaError( L, "Lua API object does not exist" );
					return default(T);
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="target"></param>
		/// <param name="allowGGFFinalizer"></param>
		public static void LuaPushObject ( LuaState L, object target, bool allowGGFFinalizer )
		{
			lock (objects) {
				
				var id		=	Interlocked.Increment( ref counter );
				var wrapper	=	new LuaObject(target);


				objects.Add( id, wrapper );

				using ( new LuaStackGuard( L, 1 ) ) {

					var array = (byte[])Lua.LuaNewUserData(L,4);
					BitConverter.GetBytes( id ).CopyTo( array, 0 );

					Lua.LuaNewTable(L);										

					Lua.LuaPushString(L,"__index");							
					Lua.LuaPushCFunction(L, wrapper.LuaMetaIndex );		
					Lua.LuaRawSet(L, -3);									

					Lua.LuaPushString(L,"__newindex");						
					Lua.LuaPushCFunction(L, wrapper.LuaMetaNewIndex );	
					Lua.LuaSetTable(L, -3);									

					Lua.LuaPushString(L,"__metatable");						
					Lua.LuaPushBoolean(L, 0 );								
					Lua.LuaSetTable(L, -3);

					Lua.LuaSetMetatable(L,-2);								
				}
			}
		}
	}
}
