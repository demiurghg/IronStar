using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core {

	public sealed class FpsCounter {

		public readonly int maxCount;
		public readonly List<float> timeRecord;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="max">Max number of time records</param>
		public FpsCounter ( int maxCount )
		{
			this.maxCount	=	maxCount;
			timeRecord		=	new List<float>( maxCount + 1 );
			timeRecord.Add( 0.016666f );
		}


		/// <summary>
		/// Adds time record
		/// </summary>
		/// <param name="gameTime"></param>
		public void AddTime ( GameTime gameTime )
		{
			timeRecord.Add( gameTime.ElapsedSec );

			while (timeRecord.Count>maxCount) {
				timeRecord.RemoveAt(0);
			}
		}

		
		/// <summary>
		/// Gets max FPS
		/// </summary>
		public float MaxFps {
			get {
				return 1 / timeRecord.Max( time => time );
			}
		}

		
		/// <summary>
		/// Gets min FPS
		/// </summary>
		public float MinFps {
			get {
				return 1 / timeRecord.Min( time => time );
			}
		}

		
		/// <summary>
		/// Gets average FPS
		/// </summary>
		public float AverageFps {
			get {
				return 1 / timeRecord.Average( time => time );
			}
		}


		/// <summary>
		/// Gets last recorded FPS
		/// </summary>
		public float CurrentFps {
			get {
				return 1 / timeRecord.Last();
			}
		}
	}
}
