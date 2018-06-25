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
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using Fusion.Core;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;
using IronStar.Entities;

namespace IronStar.Items {

	public partial class Weapon : Item {

		public Weapon( ItemFactory factory ) : base( factory )
		{
		}

		public override bool AllowDrop {
			get {
				return true;
			}
		}

		public override bool Depleted {
			get {
				return false;
			}
		}

		public override bool Activate()
		{
			throw new NotImplementedException();
		}

		public override bool Attack()
		{
			throw new NotImplementedException();
		}

		public override Entity Drop()
		{
			throw new NotImplementedException();
		}

		public override bool Pickup( Entity player )
		{
			throw new NotImplementedException();
		}

		public override bool Reload()
		{
			throw new NotImplementedException();
		}

		public override void Update( float elsapsedTime )
		{
			throw new NotImplementedException();
		}
	}
}

