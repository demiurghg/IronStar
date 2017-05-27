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

namespace Fusion.Core.Shell {
	public partial class LuaInvoker : IDisposable {

		readonly LuaState L;
		public Game Game { get; private set; }
		object lockObject = new object();
		readonly Queue<string> cmdQueue = new Queue<string>();






		/// <summary>
		/// Creates instance of Invoker.
		/// </summary>
		/// <param name="game">Game instance</param>
		public LuaInvoker ( Game game )
		{
			L	=	Lua.LuaOpen();
			Lua.LuaLOpenLibs(L);

			Game	=	game;
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
				LuaObject.LuaPushObject( L, target, false );
				Lua.LuaSetGlobal(L, apiName);
			}
		}


		#if false
		int MtIndex ( object target, LuaState L )
		{
			if (Lua.LuaIsString(L,2)==0) {
				LuaError("Lua API: only string keys are supported to access configuration variables");
			}

			var methodFlags		=	BindingFlags.IgnoreCase|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
			var propertyFlags	=	BindingFlags.IgnoreCase|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;

			var key	= Lua.LuaToString(L,2).ToString();

			//	try method :
			var mi   = target.GetType().GetMethod( key, methodFlags );

			if (mi!=null) {
				if (mi.HasAttribute<LuaApiAttribute>()) {
					Lua.LuaPushCFunction( L, (LuaNativeFunction)mi.CreateDelegate(typeof(LuaNativeFunction), target) );
					return 1;
				}
			}


			//	try property :
			var prop = target.GetType().GetProperty( key, propertyFlags );

			if (prop==null) {
				LuaError("Lua API: no such property or method '{0}'", key);
			}

			if (!prop.HasAttribute<ConfigAttribute>()) {
				LuaError("Lua API: property '{0}' does not have [Config] attirbute", key);
			}


			try {
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
				if (prop.PropertyType==typeof(bool)) {
					Lua.LuaPushBoolean( L, ((bool)prop.GetValue(target)) ? 1 : 0 );
				} else {
					LuaError("Lua API: property '{0}' has unsupported type '{1}'", key, prop.PropertyType.Name);
				}
			} catch ( Exception e ) {
				LuaError("Exception: {0}", e.Message );
			}

			return 1;
		}


		int MtNewIndex ( object target, LuaState L )
		{
			//var sw = new Stopwatch();
			//sw.Start();

			if (Lua.LuaIsString(L,2)==0) {
				LuaError("Lua API: only string keys are supported to access configuration variables");
			}

			var key	= Lua.LuaToString(L,2).ToString();

			var prop = target.GetType().GetProperty( key );

			if (prop==null) {
				LuaError("Lua API: no such property '{0}'", key);
			}

			if (!prop.HasAttribute<ConfigAttribute>()) {
				LuaError("Lua API: property '{0}' does not have [Config] attirbute", key);
			}

			try {
				if (prop.PropertyType.IsEnum) {
					prop.SetValue( target, Enum.Parse(prop.PropertyType, Lua.LuaToString(L,3).ToString() ) );
				} else 
				if (prop.PropertyType==typeof(int)) {
					prop.SetValue( target, Lua.LuaToInteger(L,3) );
				} else
				if (prop.PropertyType==typeof(float)) {
					prop.SetValue( target, (float)Lua.LuaToNumber(L,3) );
				} else
				if (prop.PropertyType==typeof(string)) {
					prop.SetValue( target, Lua.LuaToString(L,3).ToString() );
				} else
				if (prop.PropertyType==typeof(bool)) {
					prop.SetValue( target, (Lua.LuaToBoolean(L,3)!=0) );
				} else {
					LuaError("Lua API: property '{0}' has unsupported type '{1}'", key, prop.PropertyType.Name);
				}
			} catch ( Exception e ) {
				LuaError("Exception: {0}", e.Message );
			}	   

			//sw.Stop();
			//Log.Message("newindex = {0}", sw.Elapsed );

			return 1;
		}
		#endif


		void LuaError ( string frmt, params object[] args )
		{
			Lua.LuaPushString(L, string.Format(frmt, args));
			Lua.LuaError(L);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		public void RemoveCommands ( object obj )
		{
			lock (lockObject) {
			}
		}



		/// <summary>
		/// Parses and pushes command to the queue.
		/// http://stackoverflow.com/questions/6417598/why-subsequent-direct-method-call-is-much-faster-than-the-first-call
		/// </summary>
		/// <param name="commandLine"></param>
		/// <returns></returns>
		public void PushCommand ( string commandLine )
		{				  
			lock (lockObject) {
				cmdQueue.Enqueue( commandLine );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="commandLine"></param>
		public string ExecuteCommand ( string commandLine )
		{
			int errcode;

			using ( new LuaStackGuard(L) ) {
				
				errcode = Lua.LuaLLoadString( L, commandLine);

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
		public void ExecuteCommandQueue ()
		{
			lock (lockObject) {
			
				while ( cmdQueue.Any() ) {
					
					try {
						var r = ExecuteCommand(cmdQueue.Dequeue());

						if (r!=null) {	
							Log.Message("// {0} //", r);
						}

					} catch ( Exception e ) {
						Log.Error("{0}", e.Message);

						if (e.InnerException!=null) {
							Log.Error("{0}", e.InnerException.Message);
						}
					}
				}	

			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="commandLine"></param>
		/// <returns></returns>
		public static IEnumerable<string> SplitCommandLine(string commandLine)
		{
			bool inQuotes = false;

			return commandLine.Split(c =>
									 {
										 if (c == '\"')
											 inQuotes = !inQuotes;

										 return !inQuotes && c == ' ';
									 })
							  .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
							  .Where(arg => !string.IsNullOrEmpty(arg));
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Commands :
		 * 
		-----------------------------------------------------------------------------------------*/
	}
}
