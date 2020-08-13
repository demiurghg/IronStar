using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.ECSGraphics
{
	class AttachmentComponent : IComponent
	{
		/// <summary>
		/// Target entity
		/// </summary>
		public uint TargetID = 0;

		/// <summary>
		/// Attachment transform relative to target entity
		/// </summary>
		public Matrix Transform;

		/// <summary>
		/// Attachment joint name, null means no attachment
		/// </summary>
		public string JointName = null;

		/// <summary>
		/// Attachment joint index, negative value means that joint index is not resolved
		/// </summary>
		public int JointIndex = -1;

		public void Load( GameState gs, Stream stream )
		{
		}

		public void Save( GameState gs, Stream stream )
		{
		}
	}
}
