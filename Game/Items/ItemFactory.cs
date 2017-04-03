using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using Fusion.Engine.Storage;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;

namespace IronStar.Items {


	public class ItemFactory {

		[Category("Appearance")]
		public string NiceName { get; set; } = "<NiceName>";

		[Category("Appearance")]
		public string Icon { get; set; } = "";

		[Category("Appearance")]
		[Description("World model of the item")]
		public string WorldModel { get; set; } = "";

		[Category("Appearance")]
		[Description("World model of the item")]
		public string IdleFX { get; set; } = "";

		[Category("Appearance")]
		public string PickupFX { get; set; } = "";

		[Category("Appearance")]
		public string DropFX { get; set; } = "";

		[Category("Appearance")]
		public string FallFX { get; set; } = "";



		[Category("Item Physics")]
		[Description("Width of physical box that represent item")]
		public float Width { get; set; } = 1;

		[Category("Item Physics")]
		[Description("Height of physical box that represent item")]
		public float Height { get; set; } = 1;

		[Category("Item Physics")]
		[Description("Depth of physical box that represent item")]
		public float Depth { get; set; } = 1;

		[Category("Item Physics")]
		[Description("Mass of physical box that represent item")]
		public float Mass { get; set; } = 1;
		

		public void Draw( DebugRender dr, Matrix transform, Color color )
		{
			var w = Width/2;
			var h = Height/2;
			var d = Depth/2;

			dr.DrawBox( new BoundingBox( new Vector3(-w, -h, -d), new Vector3(w, h, d) ), transform, color );
			dr.DrawPoint( transform.TranslationVector, (w+h+d)/3/2, color );
		}
	}



	[ContentLoader( typeof( ItemFactory ) )]
	public sealed class ItemFactoryLoader : ContentLoader {

		static Type[] extraTypes;

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			if (extraTypes==null) {
				extraTypes = Misc.GetAllSubclassesOf( typeof(ItemFactory) );
			}

			return Misc.LoadObjectFromXml( typeof(ItemFactory), stream, extraTypes );
		}
	}
}
