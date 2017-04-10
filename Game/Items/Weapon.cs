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
		public Weapon( string name, WeaponFactory factory ) : base( name )
		{
		}

		public override bool IsBusy {
			get { return false;	}
		}

		public override bool IsDepleted {
			get { return false;	}
		}

		public override bool IsDroppable {
			get { return true;	}
		}

		public override bool IsUsable {
			get { return true;	}
		}

		public override bool IsWeapon {
			get { return true;	}
		}

		public override void Attack()
		{
			Log.Warning("WEAPON ATTACK");
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

		public override void Reload()
		{
			Log.Warning("WEAPON RELOAD");
		}

		public override void Throw()
		{
			Log.Warning("WEAPON THROW");
		}

		public override void Update( float elsapsedTime )
		{
		}

		public override void Use()
		{
			Log.Warning("WEAPON USE");
		}
	}
}
