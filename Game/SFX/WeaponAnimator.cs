using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.Core;

namespace IronStar.SFX {
	public class WeaponAnimator : Animator {

		AnimationTrack		trackWeapon;
		AnimationTrack		trackBarrel;
		AnimationTrack		trackShakes;


		/// <summary>
		/// 
		/// </summary>
		public WeaponAnimator ( Entity entity, ModelInstance model ) : base(entity,model)
		{
			trackWeapon	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Override );
			trackShakes	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Additive );

			composer.Tracks.Add( trackWeapon );
			composer.Tracks.Add( trackShakes );

			trackWeapon.Sequence( "anim_idle", true, true );
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Update ( GameTime gameTime, Matrix[] destination )
		{
			composer.Update( gameTime, destination ); 
		}
		
	}
}
