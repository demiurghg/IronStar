using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Utils;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using System.IO;
using IronStar.SFX;
using Fusion.Engine.Graphics;
using Fusion.Core.Content;
using Fusion.Engine.Common;
using Fusion.Engine.Storage;
using System.ComponentModel;
using System.Xml.Serialization;
using IronStar.Editor2;


namespace IronStar.Core {

	public class WeaponFactory {

		public string Projectile { get; set; }	

		public short Damage;

		//public float 


	}




	[ContentLoader( typeof( WeaponFactory ) )]
	public sealed class WeaponFactoryLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return Misc.LoadObjectFromXml( typeof(WeaponFactory), stream, null );
		}
	}
}
