using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Imaging;
using Fusion.Engine.Storage;
using Fusion.Core.IniParser.Model;
using Fusion.Core.Content;
using System.Drawing.Design;
using Fusion.Development;
using System.Windows.Forms;
using System.ComponentModel;
using Fusion.Core.IniParser;

namespace Fusion.Build.Mapping {

	public class VTMaterial {

		[Category( "General" )]
		[ReadOnly( true )]
		public string KeyPath { get; set; }

		[Category( "General" )]
		public bool SkipProcessing { get; set; }

		[Category( "Textures" )]
		[Editor( typeof(ImageFileLocationEditor), typeof( UITypeEditor ) )]
		public string  BaseColor { get;set; } = "";

		[Category( "Textures" )]
		[Editor( typeof(ImageFileLocationEditor), typeof( UITypeEditor ) )]
		public string  NormalMap { get;set; } = "";

		[Category( "Textures" )]
		[Editor( typeof(ImageFileLocationEditor), typeof( UITypeEditor ) )]
		public string  Metallic { get;set; } = "";

		[Category( "Textures" )]
		[Editor( typeof(ImageFileLocationEditor), typeof( UITypeEditor ) )]
		public string  Roughness { get;set; } = "";

		[Category( "Textures" )]
		[Editor( typeof(ImageFileLocationEditor), typeof( UITypeEditor ) )]
		public string  Emission { get;set; } = "";

		[Category( "Surface" )]
		public bool Transparent { get;set; } = false;


		/// <summary>
		/// 
		/// </summary>
		public VTMaterial()
		{
		}


		/// <summary>
		/// Creates VTMaterial from scene's material ref.
		/// </summary>
		/// <param name="mtrlRef"></param>
		public VTMaterial ( MaterialRef mtrlRef )
		{
			KeyPath		=	mtrlRef.Name;
			BaseColor	=	mtrlRef.Texture;
		}



		[Browsable(true)]
		[DisplayName("Replace Texture Names")]
		public void AutoReplaceTextureNames ()
		{
			NormalMap	= ReplaceIfExists( BaseColor, "NormalMap" );
			Metallic	= ReplaceIfExists( BaseColor, "Metallic"  );
			Roughness	= ReplaceIfExists( BaseColor, "Roughness" );
			Emission	= ReplaceIfExists( BaseColor, "Emission"  );
		}


		string ReplaceIfExists ( string baseColor, string suffix )
		{
			var dir = Builder.FullInputDirectory;

			var fn  = baseColor.Replace("_BaseColor.", "_" + suffix + "." );

			if ( File.Exists( Path.Combine(dir,fn) ) ) { 
				return fn;
			} else {
				return "";
			}
		}


		[Browsable(true)]
		[DisplayName("Import from Scene...")]
		public static void ImportFromScene ()
		{
			Log.Warning("Not implemented");
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="materials"></param>
		public static void ExportToIniFile ( Stream stream, IEnumerable<VTMaterial> materials )
		{
			var ip = new StreamIniDataParser();

			ip.Parser.Configuration.AllowDuplicateSections	=	true;
			ip.Parser.Configuration.AllowDuplicateKeys		=	true;
			ip.Parser.Configuration.CommentString			=	"#";
			ip.Parser.Configuration.OverrideDuplicateKeys	=	true;
			ip.Parser.Configuration.KeyValueAssigmentChar	=	'=';
			ip.Parser.Configuration.AllowKeysWithoutValues	=	true;

			IniData iniData = new IniData();

			foreach ( var mtrl in materials ) {

				var section = new SectionData( mtrl.KeyPath );

				section.Keys.AddKey( "basecolor"	, mtrl.BaseColor ?? "" );
				section.Keys.AddKey( "emission"		, mtrl.Emission );
				section.Keys.AddKey( "metallic"		, mtrl.Metallic );
				section.Keys.AddKey( "roughness"	, mtrl.Roughness );
				section.Keys.AddKey( "normalmap"	, mtrl.NormalMap );
				section.Keys.AddKey( "transparent"	, mtrl.Transparent ? "true" : "false" );

				iniData.Sections.Add( section );
			}
											  
			using ( var writer = new StreamWriter( stream ) ) {
				ip.WriteData( writer, iniData ); 
			}
		}



		static bool StringToBoolean ( string value, bool def )
		{
			if (string.IsNullOrWhiteSpace(value)) {
				return def;
			} else {
				return value.Equals( "true", StringComparison.OrdinalIgnoreCase );
			}
		}


		static string RebasePath ( string path, string basePath )
		{
			if (string.IsNullOrWhiteSpace(path)) {
				return path;
			} else {
				return Path.Combine( basePath, path );
			}
		}



		public void RebaseTextures ( string basePath )
		{
			BaseColor	=	RebasePath ( BaseColor	 , basePath );
			Emission	=	RebasePath ( Emission	 , basePath );
			Metallic	=	RebasePath ( Metallic	 , basePath );
			Roughness	=	RebasePath ( Roughness	 , basePath );
			NormalMap	=	RebasePath ( NormalMap	 , basePath );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public static IEnumerable<VTMaterial> ImportFromIniFile ( Stream stream )
		{
			var ip = new StreamIniDataParser();

			ip.Parser.Configuration.AllowDuplicateSections	=	true;
			ip.Parser.Configuration.AllowDuplicateKeys		=	true;
			ip.Parser.Configuration.CommentString			=	"#";
			ip.Parser.Configuration.OverrideDuplicateKeys	=	true;
			ip.Parser.Configuration.KeyValueAssigmentChar	=	'=';
			ip.Parser.Configuration.AllowKeysWithoutValues	=	true;

			IniData iniData;

			using ( var reader = new StreamReader( stream ) ) {
				iniData	=	ip.ReadData( reader );
			}

			var mtrlList = new List<VTMaterial>();

			foreach ( var section in iniData.Sections ) {

				var mtrl = new VTMaterial();

				mtrl.KeyPath		=	section.SectionName;

				mtrl.BaseColor		=	section.Keys[ "basecolor"	] ?? "";
				mtrl.Emission		=	section.Keys[ "emission"	] ?? "";
				mtrl.Metallic		=	section.Keys[ "metallic"	] ?? "";
				mtrl.Roughness		=	section.Keys[ "roughness"	] ?? "";
				mtrl.NormalMap		=	section.Keys[ "normalmap"	] ?? "";
				mtrl.Transparent	=	StringToBoolean( section.Keys[ "transparent" ], false );

				mtrlList.Add( mtrl );
			}
											  
			return mtrlList;
		}
	}
}
