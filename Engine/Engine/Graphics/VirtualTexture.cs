using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Content;
using Newtonsoft.Json;
using Fusion.Build.Mapping;

namespace Fusion.Engine.Graphics {
	
	/// <summary>
	/// Represents virtual texture resource
	/// </summary>
	public sealed class VirtualTexture {

		[ContentLoader(typeof(VirtualTexture))]
		internal class Loader : ContentLoader {

			public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
			{
				return new VirtualTexture(stream);
			}
		}

		[JsonRequired]
		public VTSegmentCollection VTSegments { get; set; }

		[JsonIgnore]
		HashSet<string> warnings = new HashSet<string>();


		/// <summary>
		/// Creates VT instance from allocator and segments collection
		/// </summary>
		/// <param name="allocator"></param>
		/// <param name="segments"></param>
		public VirtualTexture ( IEnumerable<VTSegment> segments )
		{
			VTSegments	=	new VTSegmentCollection( segments.ToDictionary( s => s.Name ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		internal VirtualTexture ( Stream stream )
		{
			int num;

			using ( var reader = new BinaryReader( stream ) ) 
			{
				num	=	reader.ReadInt32();

				VTSegments = new VTSegmentCollection();

				for ( int i=0; i<num; i++ ) 
				{
					var name	=	reader.ReadString();
					var x		=	reader.ReadInt32();
					var y		=	reader.ReadInt32();
					var w		=	reader.ReadInt32();
					var h		=	reader.ReadInt32();
					var t		=	reader.ReadBoolean();
					var c		=	reader.Read<Color>();

					VTSegments.Add( name, new VTSegment( name, x, y, w, h, c, t ) );
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal VTSegment GetTextureSegmentInfo ( string name )
		{
			if (string.IsNullOrWhiteSpace(name)) 
			{
				return VTSegment.Empty;
			}

			VTSegment segmentInfo;

			if ( VTSegments.TryGetValue( name, out segmentInfo ) ) 
			{
				return segmentInfo;
			} 
			else 
			{
				var warning = string.Format("Missing VT region {0}", name);

				if (warnings.Add(warning)) 
				{
					Log.Warning(warning);
				}

				return VTSegment.Empty;
			}
		}


		public static void SaveToStream ( VirtualTexture vt, Stream stream )
		{
			var serializer					=	new JsonSerializer();
			serializer.NullValueHandling	=	NullValueHandling.Ignore;
			serializer.Formatting			=	Formatting.Indented;

			using (var sw = new StreamWriter(stream)) 
			{
				using (var writer = new JsonTextWriter(sw)) 
				{
					serializer.Serialize(writer, vt);
				}
			}
		}


		public static VirtualTexture LoadToStream ( Stream stream )
		{
			var serializer					=	new JsonSerializer();
			serializer.NullValueHandling	=	NullValueHandling.Ignore;

			using (var sw = new StreamReader(stream)) 
			{
				using (var reader = new JsonTextReader(sw)) 
				{
					return serializer.Deserialize<VirtualTexture>(reader);
				}
			}
		}
	}
}
