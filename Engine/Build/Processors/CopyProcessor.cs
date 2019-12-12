using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Shell;

namespace Fusion.Build.Processors {

	[AssetProcessor("Copy", "Copies file to target directory as byte array asset")]
	public class CopyProcessor : AssetProcessor {

		[CommandLineParser.Name("class", "target class name")]
		[CommandLineParser.Option]
		public string TargetClass { get; set; } = "";
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceStream"></param>
		/// <param name="targetStream"></param>
		public override void Process ( AssetSource assetFile, BuildContext context )
		{
			var targetType = typeof(byte[]);

			if (!string.IsNullOrWhiteSpace(TargetClass))
			{
				targetType	=	Type.GetType(TargetClass, true);
				Log.Message("...resolved class name: " + targetType.AssemblyQualifiedName ); 
			}

			using ( var sourceStream = assetFile.OpenSourceStream() ) 
			{
				using ( var targetStream = assetFile.OpenTargetStream(targetType) ) 
				{
					sourceStream.CopyTo( targetStream );
				}
			}
		}
	}
}
