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
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using Fusion.Core;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;

namespace IronStar.Items {


	public abstract class ItemFactory {

		[Browsable(false)]
		[Description("Unique item name")]
		public string Name { get; set; }

		[Category("Inventory")]
		[Description("Displayable item name")]
		public string NiceName { get; set; } = "Unnamed Item";

		[Category("Inventory")]
		[Description("Item HUD icon")]
		public string Icon { get; set; } = "";

		/// <summary>
		/// Creates instance of item.
		/// </summary>
		/// <returns></returns>
		public abstract Item Spawn ( GameWorld world );
	}



	[ContentLoader( typeof( ItemFactory ) )]
	public sealed class ItemFactoryLoader : ContentLoader {

		static Type[] extraTypes;

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			if (extraTypes==null) {
				extraTypes = Misc.GetAllSubclassesOf( typeof(ItemFactory) );
			}

			var factory = (ItemFactory)Misc.LoadObjectFromXml( typeof(ItemFactory), stream, extraTypes );

			factory.Name = assetPath;

			return factory;
		}
	}
}
