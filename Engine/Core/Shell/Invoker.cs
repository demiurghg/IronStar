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
using Fusion.Core.Mathematics;
using System.Collections.Concurrent;
using Fusion.Drivers.Graphics;

namespace Fusion.Core.Shell {
	public partial class Invoker {

		public readonly Game Game;

		public delegate ICommand CommandCreator ();

		readonly object lockObject = new object();
		readonly Stack<ICommand> undoStack	= new Stack<ICommand>(1024);
		readonly Stack<ICommand> redoStack	= new Stack<ICommand>(1024);
		readonly Queue<ICommand> cmdQueue	= new Queue<ICommand>(1024);
		readonly Dictionary<string,CommandEntry> commandsRegistry = new Dictionary<string, CommandEntry>();

		class CommandEntry {
			public CommandCreator Creator;
			public CommandLineParser Parser;
		}


		class TestCommand : ICommand {
			
			[CommandLineParser.Required()]
			[CommandLineParser.Name("name")]
			public string Name { get; set; } = "";

			[CommandLineParser.Option]
			[CommandLineParser.Name("mode")]
			public StereoMode Mode { get; set; } = StereoMode.Disabled;

			public object Execute()
			{
				return string.Format("{0} {1}", Name, Mode );
			}

			public bool IsHistoryOn()
			{
				return false;
			}

			public void Rollback()
			{
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Invoker ( Game game )
		{
			this.Game = game; // optional ComponentCollection???
			RegisterCommand("set",  ()=>new Set(this)		);
			RegisterCommand("get",  ()=>new Get(this)		);
			RegisterCommand("undo", ()=>new UndoCmd(this)	);
			RegisterCommand("redo", ()=>new RedoCmd(this)	);

			RegisterCommand("test", () => new TestCommand() );

			Game.Components.ComponentAdded   += (s,e) => FlushNameCache();
			Game.Components.ComponentRemoved += (s,e) => FlushNameCache();
		}



		/// <summary>
		/// Adds named command to command registry
		/// </summary>
		/// <param name="commandName"></param>
		/// <param name="creator"></param>
		public void RegisterCommand<TCommand> ( string commandName, Func<TCommand> creator ) where TCommand: ICommand
		{
			lock (lockObject) {
				if (commandsRegistry.ContainsKey( commandName ) ) {
					Log.Warning("Command '{0}' is already registered", commandName );
					return;
				}

				CommandCreator		commandCreator	= () => creator();
				CommandLineParser	commandParser	= new CommandLineParser(typeof(TCommand), commandName );

				commandsRegistry.Add( commandName, new CommandEntry() { Creator = commandCreator, Parser = commandParser } );
			}
		}


		/// <summary>
		/// Removes command from command registry
		/// </summary>
		/// <param name="commandName"></param>
		public void UnregisterCommand ( string commandName )
		{
			lock (lockObject) {
				if (!commandsRegistry.Remove( commandName )) {
					Log.Warning("Command '{0}' is not registered", commandName );
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectName"></param>
		/// <param name="pi"></param>
		/// <param name="obj"></param>
		private bool TryGetComponentProperty ( string objectName, out PropertyInfo propertyInfo, out IGameComponent component )
		{
			propertyInfo = null;
			component = null;

			string left, right;

			if (!objectName.Split('.', out left, out right)) {
				return false;
			}

			component = Game.Components.FirstOrDefault( c1 => c1.GetType().Name==left);

			if (component==null) {
				return false;
			}

			propertyInfo = component.GetType().GetProperty(right);

			if (propertyInfo==null) {
				return false;
			}

			return true;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="value"></param>
		private void SetComponentProperty ( string variable, string value )
		{
			IGameComponent component;
			PropertyInfo pi;

			if (!TryGetComponentProperty( variable, out pi, out component )) {
				throw new InvokerException("bad component property name '{0}'", variable);
			}

			object newVal;

			if (!StringConverter.TryConvertFromString(pi.PropertyType, value, out newVal )) {
				throw new InvokerException("can not set {0} from '{1}'", pi.PropertyType, value);
			}
			pi.SetValue( component, newVal );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		private string GetComponentProperty ( string variable )
		{
			IGameComponent component;
			PropertyInfo pi;

			if (!TryGetComponentProperty( variable, out pi, out component )) {
				throw new InvokerException("bad component property name '{0}'", variable);
			}

			string strValue;

			if (!StringConverter.TryConvertToString( pi.GetValue(component), out strValue )) {
				throw new InvokerException("can not convert {0} to string", pi.PropertyType);
			}

			return strValue;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="command"></param>
		/// <param name="immediate"></param>
		/// <returns></returns>
		public object Execute ( ICommand command )
		{
			lock (lockObject) {

				var result = command.Execute();

				if (command.IsHistoryOn()) {
					redoStack.Clear();
					undoStack.Push(command);
				}

				return result;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="commands"></param>
		public void ExecuteBatch ( params ICommand[] commands )
		{
			var batch = new Batch( commands.ToArray() );
			Execute( batch );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="commandLine"></param>
		/// <returns></returns>
		public object ExecuteString ( string commandLine )
		{
			lock (lockObject) {
				return Execute( ParseCommand( commandLine ) );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="commandLine"></param>
		/// <returns></returns>
		public void ExecuteStringDeferred ( string commandLine )
		{
			lock (lockObject) {
				cmdQueue.Enqueue( ParseCommand( commandLine ) );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		CommandLineParser GetParser ( string name )
		{
			CommandEntry entry;

			if (commandsRegistry.TryGetValue( name, out entry )) {
				return entry.Parser;
			}
			
			throw new InvalidOperationException(string.Format("Unknown command '{0}'.", name));
		}



		/// <summary>
		/// Parses command line and returns command instance.
		/// </summary>
		/// <param name="comm"></param>
		/// <returns></returns>
		ICommand ParseCommand ( string commandLine )
		{
			CommandEntry commandEntry;
			PropertyInfo pi;
			IGameComponent gc;

			if (string.IsNullOrWhiteSpace( commandLine )) {
				throw new InvokerException("Empty command line");
			}

			var args = commandLine.SplitCommandLine().ToArray();


			if (commandsRegistry.TryGetValue(args[0], out commandEntry )) {
				
				var command = commandEntry.Creator();

				commandEntry.Parser?.ParseCommandLine( command, args.Skip(1).ToArray() );

				return command;

			} else if ( TryGetComponentProperty( args[0], out pi, out gc ) ) {

				if (args.Length==2) {
					return new Set(this, args[0], args[1] );
				} else {
					return new Get(this, args[0], true );
				}

			} else {
				throw new InvokerException("Unknown command : {0}", args.First() );
			}
		}



		/// <summary>
		/// Executes deferred commands 
		/// </summary>
		public void ExecuteDeferredCommands ( bool showResult = true )
		{
			lock ( lockObject ) {
				while ( cmdQueue.Any() ) {

					try {
						
						var result = Execute( cmdQueue.Dequeue() );	

						if (showResult && result!=null) {
							Log.Message("// {0} //", StringConverter.ConvertToString(result) );
						}

					} catch ( Exception e ) {

						Log.Error( e.Message );
					
					}
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="count"></param>
		public bool Undo ( int count )
		{
			lock (lockObject) {

				for (int i=0; i<count; i++) {

					if (undoStack.Any()) {
					
						var cmd = undoStack.Pop();
						cmd.Rollback();
						redoStack.Push(cmd);

					} else {
						return false;
					}
				}
			}

			return true;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="count"></param>
		public bool Redo ( int count )
		{
			lock (lockObject) {

				for (int i=0; i<count; i++) {

					if (redoStack.Any()) {
					
						var cmd = redoStack.Pop();
						cmd.Execute();
						undoStack.Push(cmd);

					} else {
						return false;
					}
				}
			}

			return true;
		}



		/// <summary>
		/// 
		/// </summary>
		public void ClearHistory ()
		{
			lock (lockObject) {
				undoStack.Clear();
				redoStack.Clear();
			}
		}

	}
}
