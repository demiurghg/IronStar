using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Animation 
{
	public class AnimationStack : IEnumerable<ITransformProvider>
	{
		readonly List<ITransformProvider> stack;

		public AnimationStack()
		{
			stack	=	new List<ITransformProvider>();
		}


		public void Add( ITransformProvider animationProvider )
		{
			stack.Add( animationProvider );
		}

		
		public IEnumerator<ITransformProvider> GetEnumerator()
		{
			return ( (IEnumerable<ITransformProvider>)stack ).GetEnumerator();
		}

		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<ITransformProvider>)stack ).GetEnumerator();
		}
	}
}
