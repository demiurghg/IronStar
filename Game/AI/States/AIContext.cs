using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.AI.States
{
	public class AIContext
	{
		public AIContext( AIState initialState )
		{
			Current = initialState;
		}
		
		AIState current = null;
		public AIState Current 
		{
			get { return current; }
			set
			{
				current = value;
			}
		}
	}
}
