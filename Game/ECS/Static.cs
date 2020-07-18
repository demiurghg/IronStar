using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	/// <summary>
	/// Entity with static component will be immidiatly killed after update	
	/// </summary>
	public class Static : IComponent
	{
		public void Added( GameState gs, Entity entity ) {}
		public void Removed( GameState gs ) {}
		public void Load( GameState gs, Stream stream ) {}
		public void Save( GameState gs, Stream stream ) {}
	}
}
