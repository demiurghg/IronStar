using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using Fusion.Core;
using Fusion.Engine.Tools;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;
using System.Threading;


namespace Fusion.Engine.Common {

	public abstract class GameService {

		public Game Game { get; protected set; }

		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="game"></param>
		public GameService ( Game game )
		{
			this.Game = game;
		}


		/// <summary>
		/// Intializes component.
		/// </summary>
		public virtual void Initialize () 
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="command"></param>
		public virtual void SendCommand ( string command )
		{
		}


		/// <summary>
		/// 
		/// </summary>
		public virtual void DispatchCommands ()
		{
		}


		/// <summary>
		/// Called when game 
		/// </summary>
		/// <param name="gameTime"></param>
		public virtual void Update ( GameTime gameTime ) 
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public virtual bool Wait ( int timeout )
		{
			return true;
		}


		/// <summary>
		/// 
		/// </summary>
		public virtual void Shutdown () 
		{
		}
	}
}
