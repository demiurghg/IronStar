using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace Fusion.Drivers.Graphics {
	static class HardwareProfileChecker {

		/// <summary>
		/// Gets the level of feature ?
		/// </summary>
		/// <param name="profile"></param>
		/// <returns></returns>
		public static FeatureLevel GetFeatureLevel ()
		{
			return FeatureLevel.Level_11_0;
		}
	}
}
