using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.AI
{
	public enum Difficulty
	{
		Easy,
		Medium,
		Hard,
	}

	static class DifficultyUtils
	{
		public static int GetTokenCount( Difficulty difficulty )
		{
			switch (difficulty)
			{
				case Difficulty.Easy:	return 1;
				case Difficulty.Medium:	return 2;
				case Difficulty.Hard:	return 3;
				default:				return 0;
			}
		}

		public static TimeSpan GetTokenTimeout( Difficulty difficulty )
		{
			switch (difficulty)
			{
				case Difficulty.Easy:	return TimeSpan.FromMilliseconds( 1500 );
				case Difficulty.Medium:	return TimeSpan.FromMilliseconds(  700 );
				case Difficulty.Hard:	return TimeSpan.FromMilliseconds(  300 );
				default:				return TimeSpan.FromMilliseconds(    0 );
			}
		}
	}
}
