using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	internal class BagEnumerator<T> : IEnumerator<T>
	{
		private volatile Bag<T> bag;

		private volatile int index;


		public BagEnumerator(Bag<T> bag)
		{
			this.bag = bag;
			this.Reset();
		}


		T IEnumerator<T>.Current
		{
			get
			{
				return this.bag.Get(this.index);
			}
		}


		object IEnumerator.Current
		{
			get
			{
				return this.bag.Get(this.index);
			}
		}


		public void Dispose()
		{
			this.bag = null;
		}


		public bool MoveNext()
		{
			return ++this.index < this.bag.Count;
		}


		public void Reset()
		{
			this.index = -1;
		}
	}
}
