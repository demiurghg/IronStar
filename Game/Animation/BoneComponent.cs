using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Engine.Audio;
using Fusion.Core.Extensions;
using KopiLua;
using Fusion.Scripting;
using IronStar.SFX;
using IronStar.SFX2;
using IronStar.ECS;
using System.IO;

namespace IronStar.Animation 
{
	public class BoneComponent : IComponent
	{
		public Matrix[] Bones { get { return bones; } }
		readonly Matrix[] bones;

		private BoneComponent(Matrix[] matricies) : this()
		{
			Array.Copy( matricies, bones, Math.Min( matricies.Length, bones.Length ) );
		}

		public BoneComponent()
		{
			bones = Misc.CreateArray( RenderSystem.MaxBones, Matrix.Identity );
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( bones, RenderSystem.MaxBones );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			reader.Read( bones, RenderSystem.MaxBones );
		}

		public IComponent Clone()
		{
			return new BoneComponent( bones );
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}
	}
}
