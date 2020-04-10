using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Shell;

namespace Fusion.Build.Processors {

	public abstract class AssetGenerator{
		
		/// <summary>
		/// 
		/// </summary>
		public AssetGenerator ()
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceStream"></param>
		/// <param name="targetStream"></param>
		public abstract IEnumerable<AssetSource> Generate ( IBuildContext context, BuildResult result );
	}
}
