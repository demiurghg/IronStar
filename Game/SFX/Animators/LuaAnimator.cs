using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion;

namespace IronStar.SFX {
	public class LuaAnimator : Animator {

		


		/// <summary>
		/// 
		/// </summary>
		public LuaAnimator( GameWorld world, Entity entity, ModelInstance model, string scriptPath ) : base(world, entity,model)
		{
		}


		/// <summary>
		/// 
		/// </summary>
		public override void Update( GameTime gameTime, Matrix[] destination )
		{
		}
	}
}
