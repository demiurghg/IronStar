using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using System.Reflection;
using Fusion;
using System.Runtime.Remoting;

namespace IronStar.ECS
{
	public partial class GameState
	{
		enum OpCode
		{
			Spawn,
			Kill,
			AddComp,
			RemoveComp,
		}

		class Command 
		{
			public readonly OpCode OpCode;
			public readonly uint   EntityID;
			public readonly IComponent Component;

			public Command( OpCode opCode, uint entityID, IComponent component )
			{
				this.OpCode		=	opCode;
				this.EntityID	=	entityID;
				this.Component	=	component;
			}
		}

		readonly Queue<Command> commands = new Queue<Command>(64);


		void ExecuteCommands()
		{
			while (commands.Any())
			{
				var cmd = commands.Dequeue();

				switch (cmd.OpCode) 
				{
					case OpCode.Spawn		:	break;
					case OpCode.Kill		:	break;
					case OpCode.AddComp		:	break;
					case OpCode.RemoveComp	:	break;
					default: throw new InvalidOperationException("Bad GameState VM op code");
				}
			}
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Entity & component public stuff :
		-----------------------------------------------------------------------------------------------*/

		public void KillCmd ( uint id )
		{
			commands.Enqueue( new Command( OpCode.Kill, id, null ) );
		}


	}
}
