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
using Fusion.Core.Shell;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;

namespace IronStar.Items {

	public class AmmoFactory : ItemFactory {

		[AECategory("Ammo")]
		public int Count { get; set; } = 10;
		
		[AECategory("Ammo")]
		public int MaxCount { get; set; } = 50;


		public override Item Spawn( uint id, short clsid, GameWorld world )
		{
			return new Ammo( id, clsid, world, this );
		}
	}
}
