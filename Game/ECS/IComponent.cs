using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace IronStar.ECS
{
	public interface IComponent
	{
		/// <summary>
		/// Saves all component data to stream.
		/// </summary>
		/// <param name="stream"></param>
		void Save( GameState gs, Stream stream );

		/// <summary>
		/// Loads component data from stream.
		/// </summary>
		/// <param name="stream"></param>
		void Load( GameState gs, Stream stream );

		/// <summary>
		/// Makes the copy of the entire component
		/// </summary>
		/// <returns></returns>
		IComponent Clone();

		/// <summary>
		/// Creates interpolated state of the component.
		/// </summary>
		/// <param name="previous">Previous component data, might be null</param>
		/// <param name="factor">Component interpolation factor. 
		/// One means that current state is returned. 
		/// Zero means the old one.</param>
		/// <returns>Interpolated component</returns>
		IComponent Interpolate( IComponent previous, float factor );
	}
}
