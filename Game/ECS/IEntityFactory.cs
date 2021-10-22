using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	/// <summary>
	/// Contains all required data for deferred entity creation.
	/// </summary>
	public interface IEntityFactory
	{
		void Construct( Entity entity, IGameState gs );
	}
}
