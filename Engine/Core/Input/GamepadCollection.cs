﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Input;
using Fusion.Engine.Common;

namespace Fusion.Core.Input {
	public sealed class GamepadCollection {

		
		Gamepad[] gamepads;
		

		internal GamepadCollection ( Game Game )
		{
			gamepads	=	new Gamepad[4];
			gamepads[0]	=	Gamepad.GetGamepad( 0 );
			gamepads[1]	=	Gamepad.GetGamepad( 1 );
			gamepads[2]	=	Gamepad.GetGamepad( 2 );
			gamepads[3]	=	Gamepad.GetGamepad( 3 );
		}
		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="playerIndex"></param>
		/// <returns></returns>
		public Gamepad this[int playerIndex] {
			get {							  
				if (playerIndex>gamepads.Length) {
					throw new ArgumentOutOfRangeException("playerIndex must be in 0,1,2 or 3");
				}

				return gamepads[playerIndex];
			}
		}
	}
}