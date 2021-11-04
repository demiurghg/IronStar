using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using BEPUphysics;
using BEPUphysics.Character;
using BEPUCharacterController = BEPUphysics.Character.CharacterController;
using Fusion.Core.IniParser.Model;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using IronStar.ECS;
using IronStar.Gameplay;


namespace IronStar.ECSPhysics 
{
	public class MaterialComponent : IComponent
	{
		public MaterialType	Material;

		public MaterialComponent() : this(MaterialType.Metal) {}

		public MaterialComponent( MaterialType material )
		{
			Material	=	material;
		}


		public static MaterialType GetMaterial( Entity e )
		{
			var mtrl = e?.GetComponent<MaterialComponent>();
			return (mtrl==null)? MaterialType.Metal : mtrl.Material;
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( (int)Material );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Material	=	(MaterialType)reader.ReadInt32();
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float factor )
		{
			return Clone();
		}
	}
}
