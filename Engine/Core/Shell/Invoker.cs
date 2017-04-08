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
	public partial class Invoker {

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
			Game	=	game;
			commandNames	=	new string[0];
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		public void AddCommands ( object obj )
		{
			lock (lockObject) {

				foreach ( var mi in obj.GetType().GetMethods() ) {

					var cmdAttr		= mi.GetCustomAttribute<CommandAttribute>();

					if(cmdAttr==null) {
						continue;
					}

					if (mi.ReturnType!=typeof(string)) {
						Log.Warning("Command '{0}' must return string. Ignored.", cmdAttr.Name);
						continue;
					}

					var parameters = mi.GetParameters();

					if (parameters.Length!=1 && parameters[0].ParameterType!=typeof(string[])) {
						Log.Warning("Input parameters if command '{0}' must be string[]. Ignored.", cmdAttr.Name);
						continue;
					}

					var descAttr	= mi.GetCustomAttribute<DescriptionAttribute>();

					var binding = new Binding( obj, mi, cmdAttr.Name, descAttr?.Description);

					commands.Add( cmdAttr.Name, binding );
				}


				commandNames	=	commands
					.Select( pair => pair.Key )
					.ToArray();	
			}
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
	}
}
