using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;

namespace IronStar.Animation
{
	public interface ITransformProvider
	{
		/// <summary>
		/// Gets and sets animation weight
		/// Zero means that no animation is applied over destination matrix array when Evaluate is called.
		/// </summary>
		float Weight { get; set; }

		/// <summary>
		/// Indicates that given transform provide is active.
		/// </summary>
		bool IsPlaying { get; }

		/// <summary>
		/// Evaluates animation, apply animation over destination data and and advances animation if needed.
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="destination"></param>
		/// <returns></returns>
		bool Evaluate( GameTime gameTime, Matrix[] destination );
	}
}
