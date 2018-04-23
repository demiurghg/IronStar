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

namespace Fusion.Core.Shell {
	public partial class Invoker {

		public delegate ICommand CommandCreator ( ArgList args );

		readonly object lockObject = new object();
		readonly Stack<ICommand> undoStack	= new Stack<ICommand>(1024);
		readonly Stack<ICommand> redoStack	= new Stack<ICommand>(1024);
		readonly Queue<ICommand> cmdQueue	= new Queue<ICommand>(1024);
		readonly Dictionary<string,CommandCreator> commandsRegistry = new Dictionary<string, CommandCreator>();


		/// <summary>
		/// 
		/// </summary>
		public Invoker ()
		{
			//RegisterCommand("set", (args)=>new Set(this,args));
		}


		/// <summary>
		/// Adds named command to command registry
		/// </summary>
		/// <param name="commandName"></param>
		/// <param name="creator"></param>
		public void RegisterCommand ( string commandName, CommandCreator creator )
		{
			lock (lockObject) {
				if (commandsRegistry.ContainsKey( commandName ) ) {
					Log.Warning("Command '{0}' is already registered", commandName );
					return;
				}
				commandsRegistry.Add( commandName, creator );
			}
		}



		/// <summary>
		/// Removes command from command registry
		/// </summary>
		/// <param name="commandName"></param>
		public void UnregisterCommand ( string commandName )
		{
			lock (lockObject) {
				if (!commandsRegistry.ContainsKey( commandName ) ) {
					Log.Warning("Command '{0}' is not registered", commandName );
					return;
				}
				commandsRegistry.Remove( commandName );
			}
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

				if (command.IsRollbackable()) {
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
			if (string.IsNullOrWhiteSpace(commandLine)) {
				throw new InvokerException("Empty command line");
			}

			var args = commandLine.SplitCommandLine().ToArray();

			lock (lockObject) {
				
				CommandCreator creator;
				
				if (!commandsRegistry.TryGetValue(args[0], out creator )) {
					throw new InvokerException("Unknown command '{0}'", args[0] );
				}

				var command = creator( new ArgList(args) );

				return Execute( command );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void ExecuteDeferredCommands ()
		{
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="commandLine"></param>
		/// <returns></returns>
		public Suggestion AutoComplete ( string commandLine )
		{
			return new Suggestion( commandLine );
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
