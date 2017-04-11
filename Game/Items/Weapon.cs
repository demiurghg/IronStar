using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using Fusion.Engine.Storage;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;

namespace IronStar.Items {

	public class Weapon : Item {

		abstract class State {
			public abstract void Attack();
			public abstract void TakeOut ();
			public abstract void PutDown ();
			public abstract void Update( float dt );
		}

		#if false
		class Packed : State {
		}

		class Idle : State {
		}

		class Takeout : State {
		}

		class Putdown : State {
		}
													
		class Warmup : State {
		}

		class Cooldown : State {
		}
		#endif

		readonly WeaponFactory factory;



		public Weapon( string name, WeaponFactory factory ) : base( name )
		{
			this.factory	=	factory;
		}



		public override Entity Drop()
		{
			Log.Warning("WEAPON DROP");
			return null;
		}

		
		public override bool Pickup( Entity player )
		{
			Log.Warning("WEAPON PICKUP");
			return true;
		}


		public override void Update( float elsapsedTime )
		{
		}



		public void Attack()
		{
			Log.Warning("WEAPON ATTACK");
		}


		public void TakeOut ()
		{
		}


		public void PutDown()
		{
		}



		public bool IsBusy()
		{
			return false; /* STATE != IDLE */
		}

	}
}
