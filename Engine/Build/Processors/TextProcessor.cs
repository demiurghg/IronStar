using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Shell;
using Fusion.Core.Content;

namespace Fusion.Build.Processors {

	[AssetProcessor("Text", "Copies file to target directory as string asset")]
	public class TextProcessor : AssetProcessor {
		
		public override string GenerateParametersHash()
		{
			return ContentUtils.CalculateMD5Hash(GetType().AssemblyQualifiedName);
		}


		public override void Process ( AssetSource assetFile, IBuildContext context )
		{
			using ( var sourceStream = assetFile.OpenSourceStream() ) {
				using ( var targetStream = assetFile.OpenTargetStream(typeof(string)) ) {
					sourceStream.CopyTo( targetStream );
				}
			}
		}
	}
}
