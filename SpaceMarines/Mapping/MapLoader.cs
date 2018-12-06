using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Extensions;

namespace SpaceMarines.Mapping {

	[ContentLoader(typeof(Map))]
	public class MapLoader : ContentLoader {


		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			var text = Encoding.UTF8.GetString( stream.ReadAllBytes() );

			return LoadLegacyMap( text );

		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="mapPath"></param>
		/// <returns></returns>
		public Map LoadLegacyMap ( string text )
		{
			var map = new Map();

			return map;
		}

	}
}
