using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics {

	public enum LightStyle {
		Default		,
		Flicker		,
		FastPulse	,
		SlowPulse	,
		FastStrobe	,
		SlowStrobe	,
		Candle1		,
		Candle2		,
		Candle3		,
		Fire1		,
		Fire2		,
		Fire3		,
		Flourescent1,
		Flourescent2,
		Bulb1		,
		Bulb2		,
		Emergency1	,
		Emergency2	,
	}





	public static class LightStyleController {

		public static float RunLightStyle ( int msec, LightStyle lightStyle )
		{
			switch (lightStyle) {
				case LightStyle.Default		: return 1;
				case LightStyle.Flicker		: return PulseString( msec,  50, "mmnmmommommnonmmonqnmmo" );
				case LightStyle.FastPulse	: return SineWave( msec, 1, 0.5f, 2.0f );
				case LightStyle.SlowPulse	: return SineWave( msec, 1, 0.5f, 0.5f );
				case LightStyle.FastStrobe	: return PulseString( msec, 100, "mamamamamama" );
				case LightStyle.SlowStrobe	: return PulseString( msec, 100, "aaaaaaaazzzzzzzz" );
				case LightStyle.Candle1		: return PulseString( msec, 100, "mmmmmaaaaammmmmaaaaaabcdefgabcdefg" );
				case LightStyle.Candle2		: return PulseString( msec, 100, "mmmaaaabcdefgmmmmaaaammmaamm" );
				case LightStyle.Candle3		: return PulseString( msec, 100, "mmmaaammmaaammmabcdefaaaammmmabcdefmmmaaaa" );
				case LightStyle.Fire1		: return PulseString( msec, 100, "mmmmmffeghjklhkgjlfjgfffmmmmmfffff" );
				case LightStyle.Fire2		: return PulseString( msec, 100, "popqmneffhgjgmmpqrilkjfuppcm" );
				case LightStyle.Fire3		: return PulseString( msec, 100, "mmmfffmmmfffmmmfghjkfffffmmmmghkghfjkdjfhg" );
				case LightStyle.Flourescent1: return PulseString( msec, 100, "mmkmkmmmmkmmkmkmkmkmkmmmk" );
				case LightStyle.Flourescent2: return PulseString( msec, 100, "mmamammmmammamamaaamammma" );
				case LightStyle.Bulb1		: return PulseString( msec,  50, "kmkmkmkmkmkmkmkmkmkmkmkmkmkm" );
				case LightStyle.Bulb2		: return PulseString( msec,  50, "amamamamamamamamamamamamamam" );
				case LightStyle.Emergency1	: return PulseString( msec,  50, "zazazzzzaaaaaaaazazazzzzaaaaaaaa" );
				case LightStyle.Emergency2	: return PulseString( msec,  50, "aaaaaaaazazazzzzaaaaaaaazazazzzz" );
			}
			return 1;
		}


		static float SineWave ( int msec, float average, float amp, float freq )
		{
			return average + amp * (float)Math.Sin(freq * MathUtil.TwoPi * msec/1000.0f);
		}


		static float PulseString ( int msec, int divider, string pulse )
		{
			var index0	= (msec/divider+0) % pulse.Length;
			var index1	= (msec/divider+1) % pulse.Length;
			var factor  = (msec%divider)/(float)divider;
			var value0	= (pulse[index0] - 'a') / 26.0f;
			var value1	= (pulse[index1] - 'a') / 26.0f;
			return MathUtil.Lerp( value0, value1, factor );
		}
	}
}
