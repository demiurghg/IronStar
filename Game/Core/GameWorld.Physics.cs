﻿using System;
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
using IronStar.Views;
using Fusion.Engine.Graphics;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUVector3 = BEPUutilities.Vector3;
using BEPUTransform = BEPUutilities.AffineTransform;


namespace IronStar.Core {

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class GameWorld {

		Space physSpace;

		/// <summary>
		/// Gets physical space
		/// </summary>
		public Space PhysSpace { 
			get {
				return physSpace;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gravity"></param>
		void InitWorldPhysics ( float gravity )
		{
			physSpace	=	new BEPUphysics.Space();
			physSpace.ForceUpdater.Gravity	=	BEPUVector3.Down * gravity;
		}



	}
}
