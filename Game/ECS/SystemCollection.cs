using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	class SystemCollection : IEnumerable<ISystem>
	{
		readonly object lockObj = new object();
		readonly List<SystemWrapper> systemWrappers;

		public SystemCollection()
		{
			systemWrappers	=	new List<SystemWrapper>( GameState.MaxSystems );
		}
			

		public void Add( ISystem system )
		{
			lock (lockObj)
			{
				systemWrappers.Add( new SystemWrapper( system ) );
			}
		}


		public IEnumerator<ISystem> GetEnumerator()
		{
			var systems = systemWrappers.Select( sw => sw.System ).ToArray();
			return ((IEnumerable<ISystem>)systems).GetEnumerator();
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			var systems = systemWrappers.Select( sw => sw.System ).ToArray();
			return systems.GetEnumerator();
		}
	}
}
