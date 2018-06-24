﻿using System;
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
using Fusion.Core;
using System.IO;
using Fusion.Engine.Graphics;
using Fusion.Core.Shell;

namespace IronStar.SFX {
	public class ModelDescriptor : IPrecachable {

		[AECategory( "Appearance" )]
		[Description( "Path to FBX scene" )]
		[AEFileName("scenes", "*.fbx", AEFileNameMode.NoExtension)]
		public string ScenePath { get; set; } = "";

		[AECategory( "Appearance" )]
		[Description( "Entire model scale" )]
		public float Scale { get; set; } = 1;

		[AECategory( "Appearance" )]
		[Description( "Indicated whether animation enabled" )]
		public bool UseAnimation { get; set; } = false;

		[AECategory( "Appearance" )]
		[Description( "Indicated whether advanced animation controller is used" )]
		public bool UseAnimator { get; set; } = false;

		[AECategory( "Appearance" )]
		[Description( "Model glow color multiplier" )]
		public Color4 Color { get; set; } = new Color4( 10, 10, 10, 1 );

		[AECategory( "First Person View" )]
		public bool FPVEnable { get; set; } = false;

		[AECategory( "First Person View" )]
		public string FPVCamera { get; set; } = "camera1";

		[AECategory( "Animation" )]
		public string Prefix { get; set; } = "anim_";

		[AECategory( "Animation" )]
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
			if (!UseAnimation) {
				return new Scene[0];
			}

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
				return content.Game.GetService<Factory>().ImportJson( stream );
			}
		}
	}
}
