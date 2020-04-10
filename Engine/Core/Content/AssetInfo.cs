using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace Fusion.Core.Content {
	public sealed class AssetInfo {

		public readonly static AssetInfo NonExisting = new AssetInfo(null, null);

		public readonly string Name;
		public readonly Type ContentType;

		/// <summary>
		/// Creates instance of asset info class
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		public AssetInfo ( string name, Type contentType )
		{
			Name		=	name;
			ContentType	=	contentType;
		}

	}
}
