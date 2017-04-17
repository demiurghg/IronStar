using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Fusion.Development;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using System.Xml.Serialization;
using IronStar.Editors;
using Fusion.Core.Content;
using Fusion.Engine.Storage;
using System.IO;
using Fusion.Engine.Graphics;

namespace IronStar.SFX {
	public class ModelDescriptor : IPrecachable {

		[Category( "Appearance" )]
		[Description( "Path to FBX scene" )]
		[Editor( typeof( FbxFileLocationEditor ), typeof( UITypeEditor ) )]
		public string ScenePath { get; set; } = "";

		[Category( "Appearance" )]
		[Description( "Entire model scale" )]
		public float Scale { get; set; } = 1;

		[Category( "Appearance" )]
		[Description( "Indicated whether animation enabled" )]
		public bool UseAnimation { get; set; } = false;

		[Category( "Appearance" )]
		[Description( "Model glow color multiplier" )]
		public Color4 Color { get; set; } = new Color4( 10, 10, 10, 1 );

		[Category( "First Person View" )]
		public bool FPVEnable { get; set; } = false;

		[Category( "First Person View" )]
		public string FPVCamera { get; set; } = "camera1";

		[Category( "Animation" )]
		public string Prefix { get; set; } = "anim_";

		[Category( "Animation" )]
		public string Clips { get; set; } = "";


		public string[] GetClips ()
		{
			if (string.IsNullOrWhiteSpace(ScenePath)) {
				return new string[0];
			}

			var baseDir = Path.GetDirectoryName( ScenePath );
			
			return Clips
				.Split(new[] {',',';'}, StringSplitOptions.RemoveEmptyEntries)
				.Select( name => Path.Combine( baseDir, Prefix + name.Trim() ) )
				.ToArray();
		}


		public Scene[] LoadClips ( ContentManager content )
		{
			return GetClips()
				.Select( name => content.Load<Scene>(name) )
				.ToArray();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Matrix ComputePreTransformMatrix()
		{
			return Matrix.Scaling( Scale );
		}


		public static string SaveToXml( ModelDescriptor descriptor )
		{
			return Misc.SaveObjectToXml( descriptor, descriptor.GetType() );
		}


		public static ModelDescriptor LoadFromXml( string xmlText )
		{
			return (ModelDescriptor)Misc.LoadObjectFromXml( typeof( ModelDescriptor ), xmlText );
		}


		public void Precache( ContentManager content )
		{
			content.Precache<Scene>(ScenePath);

			foreach ( var clip in GetClips() ) {
				content.Precache<Scene>(clip);
			}
		}
	}



	/// <summary>
	/// Scene loader
	/// </summary>
	[ContentLoader( typeof( ModelDescriptor ) )]
	public sealed class ModelDescriptorLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			using ( var sr = new StreamReader( stream ) ) {
				return ModelDescriptor.LoadFromXml( sr.ReadToEnd() );
			}
		}
	}
}
