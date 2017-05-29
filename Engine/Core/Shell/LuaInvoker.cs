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

		public readonly LuaState L;
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
				
				//errcode = Lua.LuaLLoadString( L, commandLine);

				errcode = Lua.LuaLLoadBuffer( L, commandLine, (uint)commandLine.Length, "cmdline");

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



		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		public void ExecuteFile ( string path )
		{
			LuaUtils.LuaDoFile( L, Game.Content.Load<string>(path), path );
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Commands :
		 * 
		-----------------------------------------------------------------------------------------*/
	}
}
