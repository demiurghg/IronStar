using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Build.Mapping
{
	public interface IImageCache<TTag>
	{
		/// <summary>
		/// Adds tagged image to the image cache.
		/// </summary>
		/// <param name="size">Size of the image. Only square image are supported.</param>
		/// <param name="tag">Image tag data</param>
		/// <param name="region">Region where image is successfully placed. If not placed value is undefined.</param>
		/// <returns>True if success</returns>
		bool TryAdd( int size, TTag tag, out Rectangle region );

		/// <summary>
		/// Tries to get image tag
		/// </summary>
		/// <param name="region">Region where image was placed</param>
		/// <param name="tag">Image tag</param>
		/// <returns>True if image is in the cache. False otherwice.</returns>
		bool TryGet( Rectangle region, out TTag tag );

		/// <summary>
		///	Removes all entires
		/// </summary>
		void Clear();
	}
}
