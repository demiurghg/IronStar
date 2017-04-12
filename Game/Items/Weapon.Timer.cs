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

	public partial class Weapon : Item {

		class Timer {

			int counter = 0;
			int period = 0;
			public Timer() {}

			public float Fraction {
				get {
					return MathUtil.Clamp( 1 - counter / (float)period, 0, 1 );
				}
			}

			public void Stop ()
			{
				period = 9999999;
				counter = 0;
			}

			public void Restart ( int period )
			{
				this.period = period;
				counter += period;
			}

			public bool Trigger ( int dt ) 
			{ 
				counter -= dt;

				if (counter<=0) {
					return true;
				} else {
					return false;
				}
			}
		}
	}
}
