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
using Fusion.Core;
using System.ComponentModel;
using System.Xml.Serialization;
using IronStar.Editor;
using Fusion.Core.Shell;

namespace IronStar.Core {

	public abstract class EntityFactory {

		public abstract Entity Spawn ( uint id, short clsid, GameWorld world );

		/// <summary>
		/// Draws entity in editor
		/// </summary>
		/// <param name="dr"></param>
		/// <param name="transform"></param>
		/// <param name="color"></param>
		public virtual void Draw ( DebugRender dr, Matrix transform, Color color )
		{
			dr.DrawBox(	MapEditor.DefaultBox, transform, color );
		}


		public override string ToString()
		{
			return GetType().Name;
		}



		public virtual EntityFactory Duplicate ()
		{	
			return (EntityFactory)MemberwiseClone();
		}


		static Type[] factories = null;

		public static Type[] GetFactoryTypes ()
		{
			if (factories==null) {
				factories = Misc.GetAllSubclassesOf( typeof(EntityFactory) );
			}
			return factories;
		}
	}



	[ContentLoader( typeof( EntityFactory ) )]
	public sealed class EntityFactoryLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return content.Game.GetService<Factory>().ImportJson( stream );
		}
	}
}
