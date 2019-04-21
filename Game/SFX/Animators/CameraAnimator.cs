using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;

namespace IronStar.SFX.Animators {


	public class CameraAnimator {


		/// <summary>
		///	Gets current animation matrix
		/// </summary>
		public Matrix Transform {
			get {
				return transform;
			}
		}
		Matrix transform;


		/// <summary>
		/// Creates instance of camera animator
		/// </summary>
		public CameraAnimator ()
		{
			
		}


		TimeSpan standTimer = new TimeSpan();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update ( GameTime gameTime )
		{ 
		}



		public void Stand ()
		{
		}


		public void Crouch ()
		{
		}


		public void Land ()
		{
		}


		public void Jump()
		{
		}

	}
}
