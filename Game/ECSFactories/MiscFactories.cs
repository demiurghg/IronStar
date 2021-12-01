using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.ECSFactories
{
	public class DetectorVoumeFactory : EntityFactory
	{
		public float Width	{ get; set; } =	8;
		public float Height	{ get; set; } =	8;
		public float Depth	{ get; set; } =	8;

		public override void Construct( Entity e, IGameState gs )
		{
			e.AddComponent( new Transform( Position, Rotation, Scaling ) );
			e.AddComponent( new ECSPhysics.DetectorComponent( new BoundingBox( Width, Height, Depth ) ) );
		}
	}
}
