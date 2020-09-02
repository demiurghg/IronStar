using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class AttachmentComponent : IComponent
	{
		public uint TargetID { get; set; } = 0;
		public Matrix LocalTransform { get; set; } = Matrix.Identity;

		public AttachmentComponent()
		{
		}

		public AttachmentComponent( uint targetId, Matrix transform )
		{
			TargetID		=	targetId;
			LocalTransform	=	transform;
		}

		public void Load( GameState gs, Stream stream )
		{
		}

		public void Save( GameState gs, Stream stream )
		{
		}
	}
}
