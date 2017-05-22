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
	public partial class Invoker : IDisposable {

		readonly LuaState L;

		/// <summary>
		/// Game reference.
		/// </summary>
		public Game Game { get; private set; }

		public IEnumerable<string> CommandNames { get { return commandNames; } }
		string[] commandNames;

		Dictionary<string, Binding> commands = new Dictionary<string, Binding>();

		object lockObject = new object();

		Queue<string> cmdQueue = new Queue<string>();

		class Binding {
			public readonly object Object;
			public readonly MethodInfo Method;
			public readonly string Name;
			public readonly string Description;

			public Binding( object obj, MethodInfo mi, string name, string description )
			{
				Object		=	obj;
				Method		=	mi;
				Name		=	name;
				Description	=	description;
			}

			public string Run ( string[] args )
			{
				return (string)Method.Invoke( Object, new object[]{ args });
			}
		}



		/// <summary>
		/// Creates instance of Invoker.
		/// </summary>
		/// <param name="game">Game instance</param>
		public Invoker ( Game game )
		{
			L	=	Lua.LuaOpen();
			Lua.LuaLOpenLibs(L);

			Game	=	game;
			commandNames	=	new string[0];

			ExposeApi(this);
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
		/// <param name="obj"></param>
		public void ExposeApi ( object obj, string apiName = null )
		{
			lock (lockObject) {

				var bindAttr = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;

				ExposeApi( apiName, obj, obj.GetType().GetMethods(bindAttr) );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		public void ExposeApi ( Type type, string apiName = null )
		{
			lock (lockObject) {

				var bindAttr = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static;

				ExposeApi( type.GetMethods(bindAttr), apiName );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="apiName"></param>
		/// <param name="obj"></param>
		/// <param name="methods"></param>
		void ExposeApi ( string apiName, object obj, IEnumerable<MethodInfo> methods )
		{
			bool useNamespace = !string.IsNullOrEmpty(apiName);


			using ( new LuaStackGuard( L ) ) {

				//	create table if necessary :
				if (useNamespace) {
					Lua.LuaGetGlobal(L, apiName);

					if (Lua.LuaType(L,-1)!=Lua.LUA_TTABLE) {
						Lua.LuaPop(L,1);
						Lua.LuaNewTable(L);
						Lua.LuaSetGlobal(L, apiName);
						Lua.LuaGetGlobal(L, apiName);
					}
				}

				#region API Methods
				foreach ( var mi in methods ) {

					var cmdAttr     = mi.GetCustomAttribute<LuaApiAttribute>();

					if ( cmdAttr==null ) {
						continue;
					}

					if ( mi.ReturnType!=typeof( int ) ) {
						Log.Warning( "API function '{0}' must return int. Ignored.", cmdAttr.Name );
						continue;
					}

					var parameters = mi.GetParameters();

					if ( parameters.Length!=1 && parameters[0].ParameterType!=typeof( string[] ) ) {
						Log.Warning( "Input parameters of API function '{0}' must be LuaState. Ignored.", cmdAttr.Name );
						continue;
					}


					var cfunc =  (LuaNativeFunction)mi.CreateDelegate( typeof(LuaNativeFunction), obj );

					if (useNamespace) {
						Lua.LuaPushString( L, cmdAttr.Name );
						Lua.LuaPushCFunction( L, cfunc );
						Lua.LuaSetTable( L, -3 );
					} else {
						Lua.LuaPushCFunction( L, cfunc );
						Lua.LuaSetGlobal( L, cmdAttr.Name );
					}
				}

				if (useNamespace) {
					Lua.LuaPop(L,1);
				}
				#endregion
			}


			#warning remove command names
			commandNames	=	new string[0];
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

			/*var argList	=	SplitCommandLine( commandLine ).ToArray();

			if (!argList.Any()) {
				Log.Error("Empty command line");
			} 

			var cmdName	=	argList[0];

			Binding binding;
			ConfigVariable variable;

			lock (lockObject) {
				if (commands.TryGetValue( cmdName, out binding )) {

					return binding.Run( argList );

				} else if (Game.Config.Variables.TryGetValue( cmdName, out variable )) {
					if (argList.Count()==1) {
						Log.Message("{0} = {1}", variable.Name, variable.Get() );
						return variable.Get();
					} else {
						return ExecuteCommand( string.Format("set {0} \"{1}\"", cmdName, string.Join(" ", argList.Skip(1)) ) );
					}
				} else {
					throw new InvokerException(string.Format("Unknown command '{0}'", cmdName));
				}
			} */
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

		[LuaApi("set")]
		int Set_f (LuaState L)
		{
			var varName  = Lua.LuaLCheckString(L,1)?.ToString();
			var varValue = Lua.LuaLCheckString(L,2)?.ToString();

			ConfigVariable variable;

			if (!Game.Config.Variables.TryGetValue( varName, out variable )) {
				throw new Exception(string.Format("Variable '{0}' does not exist", varName) );
			}

			var oldValue = variable.Get();

			variable.Set( varValue );

			Lua.LuaPushString(L,oldValue);

			return 1;
		}


		#region Old Invoker Stuff
		#if false
		[LuaApi("toggle")]
		string Toggle_f (string[] args)
		{
			if (args.Length<2) {
				throw new InvokerException("Usage: set <variable> <value>");
			}

			var varName  = args[1];

			ConfigVariable variable;

			if (!Game.Config.Variables.TryGetValue( varName, out variable )) {
				throw new Exception(string.Format("Variable '{0}' does not exist", varName) );
			}

			var oldValue	= variable.Get();
			var value		= oldValue.ToLowerInvariant();

			if (value=="false") {
				variable.Set("true");
			} else if (value=="true") {
				variable.Set("false");
			} else if (value=="0") {
				variable.Set("1");
			} else {
				variable.Set("0");
			}

			return null;
		}



		[LuaApi("listCmds")]
		string ListCommands_f ( string[] args )
		{
			Log.Message("");
			Log.Message("Commands:");

			var list = commands
				.Select( pair => pair.Value )
				.OrderBy( cmd1 => cmd1.Name )
				.ToArray();
			
			foreach ( var cmd in list ) {
				Log.Message("  {0,-25} {1}", cmd.Name, cmd.Description );
			}
			Log.Message("{0} cmds", list.Length );

			return null;
		}



		[LuaApi("listVars")]
		string ListVariables_f ( string[] args )
		{
			Log.Message("");
			Log.Message("Variables:");

			var list = Game.Config.Variables.ToList()
					.Select( e1 => e1.Value )
					.OrderBy( e => e.Name )
					.ToList();
			
			foreach ( var variable in list ) {
				Log.Message("  {0,-35} = {1}", variable.Name, variable.Get() );
			}
			Log.Message("{0} vars", list.Count );

			return null;
		}



		[LuaApi("echo")]
		string Echo_f ( string[] args )
		{
			Log.Message( string.Join(" ", args) );

			return null;
		}
		#endif
		#endregion
	}
}
