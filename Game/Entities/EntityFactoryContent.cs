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
using System.ComponentModel;
using System.Xml.Serialization;
using IronStar.Editor;
using Fusion.Core.Shell;

namespace IronStar {

	public abstract class EntityFactoryContent : JsonContent {

		public abstract ECS.Entity SpawnECS( ECS.IGameState gs );

		/// <summary>
		/// Draws entity in editor
		/// </summary>
		/// <param name="dr"></param>
		/// <param name="transform"></param>
		/// <param name="color"></param>
		public virtual void Draw ( DebugRender dr, Matrix transform, Color color, bool selected )
		{
			dr.DrawBox(	MapEditor.DefaultBox, transform, color );
		}


		public override string ToString()
		{
			return GetType().Name;
		}



		public virtual EntityFactoryContent Duplicate ()
		{	
			return (EntityFactoryContent)MemberwiseClone();
		}


		static Type[] factories = null;

		public static Type[] GetFactoryTypes ()
		{
			if (factories==null) {
				factories = Misc.GetAllSubclassesOf( typeof(EntityFactoryContent) );
			}
			return factories;
		}
	}
}
