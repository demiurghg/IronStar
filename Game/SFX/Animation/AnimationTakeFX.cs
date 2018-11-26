using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics;

namespace IronStar.SFX.Animation {
	public class AnimationTakeFX {

		class FXKey {
			public int Frame;
			public string Name;
			public string Joint;
			public float Scale;
		}


		public readonly AnimationTake Take;

		List<FXKey> fxKeyList;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceTake"></param>
		public AnimationTakeFX ( AnimationTake sourceTake )
		{
			fxKeyList = new List<FXKey>();
			Take = sourceTake;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="fxName"></param>
		/// <param name="joint"></param>
		/// <param name="scale"></param>
		public void KeyFX ( int frame, string fxName, string joint, float scale )
		{
			var key = new FXKey {
				Frame	=	frame,
				Name	=	fxName,
				Joint	=	joint,
				Scale	=	scale,
			};

			fxKeyList.Add( key );
		}
		
	}
}
