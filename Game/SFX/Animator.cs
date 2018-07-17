using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Extensions;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Converters;
using Fusion.Engine.Graphics;
using IronStar.Core;

namespace IronStar.SFX {

	public class Animator {

		readonly Scene scene;
		readonly AnimatorFactory factory;

		readonly Dictionary<string,AnimTake> takes;

		int			animFrame = 0;
		AnimState	animState = AnimState.NoAnimation;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="factory"></param>
		public Animator ( Scene scene, AnimatorFactory factory )
		{
			this.scene		=	scene;
			this.factory	=	factory;
				
			takes			=	scene.Takes.ToDictionary( take => take.Name );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="destination"></param>
		public void Update ( Entity entity, GameTime gameTime, Matrix[] destination )
		{
			animFrame++;

			var take = scene.Takes.First( t => t.Name=="anim_idle");

			take.Evaluate( animFrame + take.FirstFrame, AnimationMode.Repeat, destination );

			/*if (animState!=entity.WeaponAnimation) {
				for (int
			}

				if (state != targetState) {
				  for (int i = 0; i < transCount; i++) {
					if (trans[i].state == targetState &&
						trans[i].loFrame <= currentFrame &&
						trans[i].hiFrame >= currentFrame) {
						// нашли подходящий переход
						setAnimation(trans[i].nextAnimation, trans[i].nextFrame);            
						return;
					}
				  }
				}*/
		}
	}

}
