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
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;


namespace IronStar.Entities {
	public partial class CharacterWeaponry {

		readonly GameWorld world;
		readonly Entity entity;
		readonly CharacterFactory factory;
		readonly Character character;

		int warmupTimer;
		int	cooldownTimer;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="entity"></param>
		/// <param name="factory"></param>
		public CharacterWeaponry ( Character character, GameWorld world, Entity entity, CharacterFactory factory )
		{
			this.world		=	world;
			this.entity		=	entity;
			this.factory	=	factory;
			this.character	=	character;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		Vector3 AttackPos ( Entity e )
		{
			var c = e.Controller as Character;
			var m = Matrix.RotationQuaternion(e.Rotation);
			return e.PointOfView + m.Right * 0.1f + m.Down * 0.1f + m.Forward * 0.3f;
		}


	}
}
