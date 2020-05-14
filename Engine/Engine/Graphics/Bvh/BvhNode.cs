using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Bvh
{
	public sealed class BvhNode<TPrimitive>
	{
		public readonly bool IsLeaf;
		public readonly TPrimitive Primitive;
		public readonly BoundingBox BoundingBox;

		public readonly BvhNode<TPrimitive>[] Children = new BvhNode<TPrimitive>[2];

		BvhNode() {}

		public BvhNode ( TPrimitive primitive, BoundingBox bbox )
		{
			this.IsLeaf			=	true;
			this.Primitive		=	primitive;
			this.BoundingBox	=	bbox;
		}

		public BvhNode ( BoundingBox bbox )
		{
			this.IsLeaf			=	false;
			this.Primitive		=	default(TPrimitive);
			this.BoundingBox	=	bbox;
		}
	}
}
