using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics.GI2
{
	public interface ILightProbeProvider
	{
		/// <summary>
		/// Returns shader resource (cube texture array) view with HDR data
		/// </summary>
		/// <returns></returns>
		ShaderResource GetLightProbeCubeArray();

		/// <summary>
		/// Gets light-probe index by name
		/// </summary>
		/// <param name="name">Name of light-probe</param>
		/// <returns>Negative value, if light-probe with given name does not exist</returns>
		int GetLightProbeIndex( string name );

		/// <summary>
		/// Updates light-probes if necessary
		/// </summary>
		void Update();
	}
}
