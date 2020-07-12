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
		public const int MaxSystems = (int)BitSet.MaxBits;

		int count;
		ISystem[] systems;


		public SystemCollection()
		{
			systems	=	new ISystem[ MaxSystems ];
		}


		public int Count 
		{
			get { return count; }
		}


		void Add ( ISystem system )
		{
			if (count>=MaxSystems) 
			{	
				throw new InvalidOperationException("Too much registered systems. Max systems is " + MaxSystems.ToString());
			}

			systems[count] = system;

			count++;
		}

		
		public IEnumerator<ISystem> GetEnumerator()
		{
			return ( (IEnumerable<ISystem>)systems ).GetEnumerator();
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<ISystem>)systems ).GetEnumerator();
		}
	}
}
