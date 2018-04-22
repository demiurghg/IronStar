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
using Fusion.Core.Mathematics;

namespace Fusion.Core.Shell {
	public partial class Invoker {

		object lockObject = new object();
		Stack<ICommand> undoStack = new Stack<ICommand>(1024);
		Stack<ICommand> redoStack = new Stack<ICommand>(1024);



		/// <summary>
		/// 
		/// </summary>
		public Invoker ()
		{
			//RegisterCommand("set", (args)=>new Set(this,args));
		}


		public void RegisterCommand ( string commandName, Func<string[],ICommand> creator )
		{
		}


		public void UnregisterCommand ( string commandName )
		{
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

				command.Execute();

				if (command.IsRollbackable) {
					redoStack.Clear();
					undoStack.Push(command);
				}

				return command.Result;
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
