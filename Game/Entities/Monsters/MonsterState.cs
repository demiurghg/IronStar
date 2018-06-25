using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Core.IniParser.Model;



namespace IronStar.Entities.Monsters {
	public class CharacterState {
		public virtual bool Damage ( int damage ) { return false; }
		public virtual void Move ( short forward, short right, short up ) { }
		public virtual void Action ( UserAction action ) {} 
	}


	public class Dead : CharacterState {
	}

	public class Stunned : CharacterState {
	}

	public class Panic : CharacterState {
	}

	public class Idle : CharacterState {
	}

	public class Attacking : CharacterState {
	}

	public class Throwing : CharacterState {
	}

	public class Switching : CharacterState {
	}

	public class Dropping : CharacterState {
	}
}
