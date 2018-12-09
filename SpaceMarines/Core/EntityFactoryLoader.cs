using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Content;

namespace SpaceMarines.Core {

	[ContentLoader( typeof( EntityFactory ) )]
	public class EntityFactoryLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			var text = Encoding.UTF8.GetString( stream.ReadAllBytes() );

			var dict = text
				.Split('\r','\n')
				.Select( s0 => s0.Trim(' ','\t',';') )
				.Where( s1 => !s1.StartsWith("//") )
				.Where( s2 => !string.IsNullOrWhiteSpace(s2) )
				.Select( s3 => s3.Replace('\t', ' ') )
				.Select( s4 => s4.SplitCommandLine().ToArray() )
				.ToDictionary( a1 => a1[0], a2 => a2[1] );


			throw new NotImplementedException();
		}
	}
}
