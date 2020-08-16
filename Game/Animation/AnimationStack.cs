using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Animation 
{
	public class AnimationStack : IEnumerable<IAnimationProvider>
	{
		readonly List<IAnimationProvider> stack;

		public AnimationStack()
		{
			stack	=	new List<IAnimationProvider>();
		}


		public void Add( IAnimationProvider animationProvider )
		{
			stack.Add( animationProvider );
		}

		
		public IEnumerator<IAnimationProvider> GetEnumerator()
		{
			return ( (IEnumerable<IAnimationProvider>)stack ).GetEnumerator();
		}

		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<IAnimationProvider>)stack ).GetEnumerator();
		}
	}
}
