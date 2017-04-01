using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Content;
using Fusion.Engine.Server;
using Fusion.Engine.Client;
using Fusion.Core.Extensions;
using IronStar.SFX;
using Fusion.Core.IniParser.Model;
using Fusion.Engine.Graphics;
using IronStar.Mapping;
using Fusion.Core;
using IronStar.Physics;

namespace IronStar.Core {

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class GameWorld : DisposableBase {

		public void InitServerAtoms ()
		{
			Atoms = new AtomCollection();

			var atoms = new List<string>();

			atoms.Add("*rail_trail");

			atoms.AddRange( Content.EnumerateAssets( "fx" ) );
			atoms.AddRange( Content.EnumerateAssets( "entities" ) );
			atoms.AddRange( Content.EnumerateAssets( "models" ) );
			atoms.AddRange( Content.EnumerateAssets( "decals" ) );
			atoms.AddRange( Content.EnumerateAssets( "items" ) );
			atoms.AddRange( Content.EnumerateAssets( "weapon" ) );

			Atoms.AddRange( atoms );
		}
	}
}
