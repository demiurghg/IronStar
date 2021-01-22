using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Animation
{
	[Flags]
	public enum SequenceMode 
	{
		/// <summary>
		/// Run single-loop animation.
		/// </summary>
		None			=	0x0000,

		/// <summary>
		/// Run animation immediatly, do not wait to complete previouse animation.
		/// Otherwice animation will be played once current animation is complete.
		/// </summary>
		Immediate		=	0x0001,

		/// <summary>
		/// Animation will repeat until new animation is sequenced.
		/// </summary>
		Looped			=	0x0002,

		/// <summary>
		/// Animation will hold results of the last frame until new animation is sequenced.
		/// </summary>
		Hold			=	0x0004,

		/// <summary>
		/// Ignore if new animation is the same as current or pending.
		/// </summary>
		DontPlayTwice	=	0x0008,

		/// <summary>
		/// Play animation backward.
		/// </summary>
		Reverse			=	0x0010,
	}
}
