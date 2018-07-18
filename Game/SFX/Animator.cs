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
using Fusion;

namespace IronStar.SFX {

	public class Animator {

		readonly Scene scene;
		readonly AnimatorFactory factory;

		readonly Dictionary<string,Animation> anims;
		readonly Dictionary<string,AnimTake> takes;

		int			currentFrame	=	0;
		AnimState	currentState	=	AnimState.Weapon_Idle;
		Animation	currentAnim		=	null;
		AnimTake	currentTake		=	null;


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
			anims			=	factory.Animations.ToDictionary( anim => anim.Take );
			
			SetAnimation( factory.Animations.FirstOrDefault().Name, 0, false );
			#warning about empty take list.
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="destination"></param>
		public void Update ( Entity entity, GameTime gameTime, Matrix[] destination )
		{
			currentFrame++;

			currentTake.Evaluate( currentFrame + currentTake.FirstFrame, AnimationMode.Clamp, destination );

			if (currentState!=entity.WeaponAnimation) {
				
				foreach ( var trans in currentAnim.Transitions ) {
					if (trans.State==entity.WeaponAnimation) {
						if ( trans.Low <= currentFrame && trans.High >= currentFrame ) {

							currentState	=	entity.WeaponAnimation;

							SetAnimation( trans.NextAnim, trans.NextKey, true );
						}
					}
				}
			}


			if (currentFrame>=currentTake.FrameCount) {
				SetAnimation( currentAnim.NextAnim, 0, false );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="anim"></param>
		/// <param name="frame"></param>
		void SetAnimation ( string anim, int frame, bool transition )
		{
			Log.Verbose("... anim :  {0} : {1}, {2}", anim, frame, transition ? "trnasition" : "next" );

			if (string.IsNullOrWhiteSpace(anim)) {

				currentFrame	=	0;

			} else {

				currentAnim		=	anims[ anim ];
				currentTake		=	takes[ currentAnim.Take ];
				currentFrame	=	frame;
				currentState	=	currentAnim.State;
			}
		}
	}

}
