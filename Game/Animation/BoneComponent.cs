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

		public BoneComponent()
		{
			bones = Misc.CreateArray( RenderSystem.MaxBones, Matrix.Identity );
		}


		public void Load( GameState gs, Stream stream )
		{
			throw new NotImplementedException();
		}

		public void Save( GameState gs, Stream stream )
		{
			throw new NotImplementedException();
		}
	}
}
