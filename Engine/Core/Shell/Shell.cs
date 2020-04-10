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

namespace Fusion.Core.Shell {
	public partial class Shell {

		/// <summary>
		/// Game reference.
		/// </summary>
		public Game Game { get; private set; }
		object lockObject = new object();


		/// <summary>
		/// Creates instance of Invoker.
		/// </summary>
		/// <param name="game">Game instance</param>
		public Shell ( Game game )
		{
			Game	=	game;
			commandNames	=	new string[0];

			AddCommands(this);
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
			var argList	=	SplitCommandLine( commandLine ).ToArray();

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

		[Command("set")]
		string Set_f (string[] args)
		{
			if (args.Length<3) {
				throw new InvokerException("Usage: set <variable> <value>");
			}

			var varName  = args[1];
			var varValue = args[2];

			ConfigVariable variable;

			if (!Game.Config.Variables.TryGetValue( varName, out variable )) {
				throw new Exception(string.Format("Variable '{0}' does not exist", varName) );
			}

			variable.Set( varValue );

			return null;
		}



		[Command("toggle")]
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



		[Command("listCmds")]
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



		[Command("listVars")]
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



		[Command("echo")]
		string Echo_f ( string[] args )
		{
			Log.Message( string.Join(" ", args) );

			return null;
		}
	}
}
