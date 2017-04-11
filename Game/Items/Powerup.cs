using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.Core;
using IronStar.Entities;

namespace IronStar.Items {

	public class Powerup : Item {

		/// <summary>
		/// 
		/// </summary>
		/// <param name="factory"></param>
		public Powerup ( string name ) : base(name)
		{
		}


		public bool IsExhausted ()
		{
			throw new NotImplementedException();
		}

		public bool Activate ()
		{
			throw new NotImplementedException();
		}

		public override Entity Drop()
		{
			throw new NotImplementedException();
		}

		public override bool Pickup( Entity player )
		{
			throw new NotImplementedException();
		}

		public override void Update( float elsapsedTime )
		{
			throw new NotImplementedException();
		}
	}
}
