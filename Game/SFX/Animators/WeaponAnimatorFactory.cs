using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Core.Shell;
using IronStar.Core;
using IronStar.Editor;
using Fusion;

namespace IronStar.SFX {
	public class WeaponAnimatorFactory : AnimatorFactory {

		[AEClassname("fx")]
		public string MuzzleFX { get; set; } = "";

		public float MuzzleFXScale { get; set; } = 0.1f;

		public override Animator Create( GameWorld world, Entity entity, ModelInstance model )
		{
			return new WeaponAnimator( world, entity, model, this );
		}

	}
}
