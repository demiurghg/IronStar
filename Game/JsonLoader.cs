using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Extensions;

namespace IronStar {

	[ContentLoader( typeof( object ) )]
	public sealed class JsonLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return content.Game.GetService<Factory>().ImportJson( stream );
		}
	}
}

