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
using Fusion.Core.Content;
using Fusion.Core;
using System.IO;
using Fusion.Engine.Graphics;
using Fusion.Core.Shell;

namespace IronStar.SFX {
	public class ModelFactory : JsonObject, IPrecachable {

		[AECategory( "Appearance" )]
		[Description( "Path to FBX scene" )]
		[AEFileName("scenes", "*.fbx", AEFileNameMode.NoExtension)]
		public string ScenePath { get; set; } = "";

		[AECategory( "Appearance" )]
		[Description( "Entire model scale" )]
		public float Scale { get; set; } = 1;

		[AECategory( "Animation" )]
		[AEClassname("animation")]
		public string AnimController { get; set; } = "";

		[AECategory( "Animation" )]
		[AEDisplayName("Enabled")]
		[AEClassname("animation")]
		public bool AnimEnabled { get; set; } = false;

		[AECategory( "Appearance" )]
		[Description( "Model glow color" )]
		public Color Color { get; set; } = Color.White;

		[AECategory( "Appearance" )]
		[Description( "Model glow intensity" )]
		public float Intensity { get; set; } = 100;

		[AECategory( "First Person View" )]
		public bool FPVEnable { get; set; } = false;

		[AECategory( "First Person View" )]
		public string FPVCamera { get; set; } = "camera1";

		[AECategory( "Animation" )]
		public string Prefix { get; set; } = "anim_";

		[AECategory( "Animation" )]
		public string Clips { get; set; } = "";

		[AECategory("Placeholder")]
		public float BoxWidth { get; set; } = 1;

		[AECategory("Placeholder")]
		public float BoxHeight { get; set; } = 1;

		[AECategory("Placeholder")]
		public float BoxDepth { get; set; } = 1;

		[AECategory("Placeholder")]
		public Color BoxColor { get; set; } = Color.YellowGreen;


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Matrix ComputePreTransformMatrix()
		{
			return Matrix.Scaling( Scale );
		}


		public static ModelFactory LoadFromXml( string xmlText )
		{
			return (ModelFactory)Misc.LoadObjectFromXml( typeof( ModelFactory ), xmlText );
		}


		public void Precache( ContentManager content )
		{
			content.Precache<Scene>(ScenePath);
		}
	}
}
