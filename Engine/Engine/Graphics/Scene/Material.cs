using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SharpDX;
using Fusion.Engine.Graphics;
using System.Reflection;
using System.ComponentModel.Design;
using Fusion.Core;
using Fusion.Core.IniParser;
using Fusion.Core.IniParser.Model;
using Fusion.Core.IniParser.Model.Formatting;

namespace Fusion.Engine.Graphics {


	/// <summary>
	/// Material reference. 
	/// Keeps material name, base texture and reference to material.
	/// </summary>
	public sealed class Material : IEquatable<Material> {

		/// <summary>
		/// Material name.
		/// </summary>
		public	string	Name { get; set; }

		/// <summary>
		/// Base color texture path.
		/// </summary>
		public	string	ColorMap { get; set; }

		/// <summary>
		/// Normal map texture path.
		/// </summary>
		public	string	NormalMap { get; set; }

		/// <summary>
		/// Metallic texture path.
		/// </summary>
		public	string	MetallicMap { get; set; }

		/// <summary>
		/// Roughness texture path.
		/// </summary>
		public	string	RoughnessMap { get; set; }

		/// <summary>
		/// Emission texture path.
		/// </summary>
		public	string	EmissionMap { get; set; }

		/// <summary>
		/// Emission texture path.
		/// </summary>
		public	bool	Transparent { get; set; }



		/// <summary>
		/// Creates materail 
		/// </summary>
		public Material ()
		{
			Name			=	"";
			ColorMap		=	null;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		static public void SaveToIniFile ( Material material, Stream stream )
		{
			var ip = new StreamIniDataParser();
			ip.Parser.Configuration.AllowDuplicateSections	=	true;
			ip.Parser.Configuration.AllowDuplicateKeys		=	true;
			ip.Parser.Configuration.CommentString			=	"#";
			ip.Parser.Configuration.OverrideDuplicateKeys	=	true;
			ip.Parser.Configuration.KeyValueAssigmentChar	=	'=';
			ip.Parser.Configuration.AllowKeysWithoutValues	=	true;

			var iniData = new IniData();

			var sectionTextures		= new SectionData("Textures");
			var sectionGeneral		= new SectionData("General");

			sectionGeneral.Keys.AddKey("Transparent", material.Transparent ? "true" : "false" );

			sectionTextures.Keys.AddKey("BaseColor",	material.ColorMap		?? "" );
			sectionTextures.Keys.AddKey("NormalMap",	material.NormalMap		?? "" );
			sectionTextures.Keys.AddKey("Metallic",		material.MetallicMap	?? "" );
			sectionTextures.Keys.AddKey("Roughness",	material.RoughnessMap	?? "" );
			sectionTextures.Keys.AddKey("Emission",		material.EmissionMap	?? "" );

			iniData.Sections.Add( sectionGeneral );
			iniData.Sections.Add( sectionTextures );

			using ( var writer = new StreamWriter( stream ) ) {
				ip.WriteData( writer, iniData, new AlignedIniDataFormatter(ip.Parser.Configuration) );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		static public Material LoadFromIniFile ( Stream stream, string name )
		{
			var ip = new StreamIniDataParser();
			ip.Parser.Configuration.AllowDuplicateSections	=	true;
			ip.Parser.Configuration.AllowDuplicateKeys		=	true;
			ip.Parser.Configuration.CommentString			=	"#";
			ip.Parser.Configuration.OverrideDuplicateKeys	=	true;
			ip.Parser.Configuration.KeyValueAssigmentChar	=	'=';
			ip.Parser.Configuration.AllowKeysWithoutValues	=	true;

			using ( var reader = new StreamReader( stream ) ) {

				var iniData = ip.ReadData( reader );

				var sectionTextures		= iniData.Sections["Textures"];
				var sectionGeneral		= iniData.Sections["General"];

				if (sectionGeneral==null) {
					throw new InvalidDataException("Missing [General] section");
				}
				
				if (sectionTextures==null) {
					throw new InvalidDataException("Missing [Textures] section");
				}

				var material = new Material();

				material.Name	=	name;

				material.Transparent	=	(sectionGeneral["Transparent"] ?? "").Equals("true", StringComparison.OrdinalIgnoreCase);

				material.ColorMap		=	sectionTextures["BaseColor"	] ?? "";
				material.NormalMap		=	sectionTextures["NormalMap"	] ?? "";
				material.MetallicMap	=	sectionTextures["Metallic"	] ?? "";
				material.RoughnessMap	=	sectionTextures["Roughness"	] ?? "";
				material.EmissionMap	=	sectionTextures["Emission"	] ?? "";

				return material;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		public void Deserialize( BinaryReader reader )
		{
			Name =  reader.ReadString();

			ColorMap = null;
			if ( reader.ReadBoolean() == true ) {
				ColorMap =  reader.ReadString();
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public void Serialize( BinaryWriter writer )
		{
			writer.Write( Name );

			if ( ColorMap == null ) {
				writer.Write( false );
			} else {
				writer.Write( true );
				writer.Write( ColorMap );
			}
		}



		public bool Equals ( Material other )
		{
			if (other==null) return false;

			return ( Name		== other.Name		)
				&& ( ColorMap	== other.ColorMap	)
				;
		}


		public override bool Equals ( object obj )
		{
			if (obj==null) return false;
			if (obj as Material==null) return false;
			return Equals((Material)obj);
		}

		public override int GetHashCode ()
		{
			int hashCode = 0;
            hashCode = (hashCode * 397) ^ Name.GetHashCode();
            hashCode = (hashCode * 397) ^ ColorMap.GetHashCode();
			//hashCode = (hashCode * 397) ^ Tag.GetHashCode(); ???
			return hashCode;
		}


		public static bool operator == (Material obj1, Material obj2)
		{
			if ((object)obj1 == null || ((object)obj2) == null)
				return Object.Equals(obj1, obj2);

			return obj1.Equals(obj2);
		}

		public static bool operator != (Material obj1, Material obj2)
		{
			if (obj1 == null || obj2 == null)
				return ! Object.Equals(obj1, obj2);

			return ! (obj1.Equals(obj2));
		}
	}
}
