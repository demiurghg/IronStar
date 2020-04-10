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

namespace Fusion.Scripting {
	
	public class LuaObjectTranslator {

		const string tableName		= "__LuaTranslatorTable";
		const string translatorName	= "__LuaTranslator";
		int counter = 0;

		Dictionary<int, LuaObjectWrapper> map = new Dictionary<int, LuaObjectWrapper>();
		Dictionary<object, int> backMap = new Dictionary<object, int>();

		readonly LuaState L;


		/// <summary>
		/// Constructor fo object translator
		/// </summary>
		/// <param name="L"></param>
		private LuaObjectTranslator ( LuaState L )
		{
			this.L = L;

			using ( new LuaStackGuard( L ) ) {

				//	push 
				Lua.LuaPushString( L, tableName );
				Lua.LuaNewTable		(L);
				Lua.LuaNewTable		(L);
				Lua.LuaPushString	(L, "__mode"); // make stored values weak.
				Lua.LuaPushString	(L, "v");
				Lua.LuaSetTable		(L, -3);
				Lua.LuaSetMetatable (L, -2);
				Lua.LuaSetTable		(L, Lua.LUA_REGISTRYINDEX);

				Lua.LuaPushString		( L, translatorName );
				Lua.LuaPushLightUserData( L, new LuaTag(this) );
				Lua.LuaSetTable			( L, Lua.LUA_REGISTRYINDEX );
			}
		}



		/// <summary>
		/// Gets instance of object translator for given Lua state :
		/// </summary>
		/// <param name="L"></param>
		/// <returns></returns>
		static public LuaObjectTranslator Instance( LuaState L )
		{
			using ( new LuaStackGuard( L ) ) {

				Lua.LuaPushString( L, translatorName );
				Lua.LuaGetTable	 ( L, Lua.LUA_REGISTRYINDEX );

				if (Lua.LuaIsNil( L, -1 )) {

					Lua.LuaPop( L, 1 );
					return new LuaObjectTranslator( L );

				} else {

					var tag = Lua.LuaToUserData( L, -1 );
					Lua.LuaPop( L, 1 );

					return (LuaObjectTranslator)tag;
				}

				/*if (L.tag==null) {
					L.tag = new LuaObjectTranslator(L);
				} 
				return (LuaObjectTranslator)L.tag;*/
			}
		}



		/// <summary>
		/// Pushes object on stack
		/// </summary>
		/// <param name="L"></param>
		/// <param name="target"></param>
		/// <param name="allowGGFFinalizer"></param>
		public void PushObject ( LuaState L, object target )
		{	
			//	push nil if null :
			if (target==null) {	
				Lua.LuaPushNil(L);
				return;
			}


			//	try find already registered object:
			int id;
			
			if ( backMap.TryGetValue( target, out id ) ) {

				Lua.LuaGetField( L, Lua.LUA_REGISTRYINDEX, tableName );
				Lua.LuaRawGetI( L, -1, id);
				
				var type = Lua.LuaType (L, -1);
				
				if (type != Lua.LUA_TNIL) {
					// remove table and leave object on stack
					Lua.LuaRemove (L, -2);	 
					return;
				}

				Lua.LuaRemove (L, -1);		// remove the nil object value
				Lua.LuaRemove (L, -1);		// remove the metatable
				CollectObject (target, id);	// Remove from both our tables and fall out to get a new ID
			}


			id = Interlocked.Increment( ref counter );

			PushNewObject( L, target, id );
		}



		/// <summary>
		/// Converts object on stack to C# object. 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public T LuaTo<T> ( LuaState L, int index ) where T: class
		{
			int id = Lua.LuaNetRawNetObj(L,index);

			if (id==-1) {
				LuaUtils.LuaError( L, "value at index {0} is not user data" );
				return default(T);
			}

			LuaObjectWrapper wrapper;

			if (map.TryGetValue(id, out wrapper)) {
				
				var target = wrapper.Target as T;

				if (target==null) {
					LuaUtils.LuaError( L, "value at index {0} is not a {1}, got {2}", index, typeof(T), wrapper.Target.GetType() );
					return default(T);
				}

				return target;

			} else {
				LuaUtils.LuaError( L, "Lua API object (id={0}) does not exist", id );
				return default(T);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="id"></param>
		void CollectObject ( object target, int id )
		{
			#warning Should we call Dispose() for IDisposable?

			//Log.Verbose("...gc object: {0} - {1}", id, target.ToString() );

			map.Remove( id );
			backMap.Remove( target );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="id"></param>
		void CollectObject ( int id )
		{
			LuaObjectWrapper wrapper;
			if (map.TryGetValue(id, out wrapper)) {
				CollectObject( wrapper.Target, id );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <param name="target"></param>
		/// <param name="id"></param>
		void PushNewObject ( LuaState L, object target, int id )
		{
			var wrapper = new LuaObjectWrapper( id, target );

			//Log.Verbose("...new object: {0} - {1}", id, target.ToString() );

			map.Add( id, wrapper );
			backMap.Add( target, id );

			using ( new LuaStackGuard( L, 1 ) ) {

				var array = (byte[])Lua.LuaNewUserData(L,4);
				BitConverter.GetBytes( id ).CopyTo( array, 0 );

				Lua.LuaNewTable(L);										

				//	get by index :
				Lua.LuaPushString(L,"__index");							
				Lua.LuaPushCFunction(L, wrapper.LuaMetaIndex );		
				Lua.LuaRawSet(L, -3);									

				//	set by index :
				Lua.LuaPushString(L,"__newindex");						
				Lua.LuaPushCFunction(L, wrapper.LuaMetaNewIndex );	
				Lua.LuaSetTable(L, -3);									

				//	set GC method :
				Lua.LuaPushString(L,"__gc");						
				Lua.LuaPushCFunction(L, LuaMetaGC );	
				Lua.LuaSetTable(L, -3);									

				//	forbid access to object's metatable
				Lua.LuaPushString(L,"__metatable");						
				Lua.LuaPushBoolean(L, 0 );								
				Lua.LuaSetTable(L, -3);

				Lua.LuaSetMetatable(L,-2);	


				//	add new object to registry :
				Lua.LuaGetField( L, Lua.LUA_REGISTRYINDEX, tableName );
				Lua.LuaPushValue( L, -2 );
				Lua.LuaRawSetI( L, -2, id );
				Lua.LuaPop(L,1);
			}		
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <returns></returns>
		int LuaMetaGC ( LuaState L )
		{
			int id = Lua.LuaNetRawNetObj(L,1);

			if (id != -1) {
				CollectObject (id);
			}
			
			return 0;
		}
	}
}
