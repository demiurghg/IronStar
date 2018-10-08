using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Scripting;
using KopiLua;

namespace IronStar.SFX {

	public enum AnimationSequenceMode {
		Wrap,
		Hold,
		Stop,
	}

	public class AnimationTrack : AnimationSource {

		public float TimeScale { get; set; } = 1;

		public bool Busy { 
			get {
				return currentAnim!=null;
			}
		}


		TimeSpan trackTime;

		Animation currentAnim;
		Animation pendingAnim;

		class Animation {
			readonly TimeMode timeMode;
			public readonly AnimationTake Take;
			public readonly bool Looped;
			public readonly bool Hold;
			public readonly TimeSpan Start;
			public readonly TimeSpan Length;
			public readonly TimeSpan End;

			public Animation ( TimeSpan startTime, AnimationTake take, TimeMode timeMode, bool looped, bool hold ) 
			{
				this.timeMode	=	timeMode;
				this.Start		=	startTime;
				this.Take		=	take;
				this.Looped		=	looped;
				this.Hold		=	hold;
				this.Length		=	Scene.ComputeFrameLength( take.FrameCount, timeMode );
				this.End		=	Start + Length;
			}

			public void GetKey ( int node, TimeSpan time, bool useDelta, out Matrix transform )
			{
				int prev, next;
				float weight;

				Scene.TimeToFrames( time - Start, timeMode, out prev, out next, out weight );

				if (Looped) {
					prev = MathUtil.Wrap( prev + Take.FirstFrame, Take.FirstFrame, Take.LastFrame );
					next = MathUtil.Wrap( next + Take.FirstFrame, Take.FirstFrame, Take.LastFrame );
				} else {
					prev = MathUtil.Clamp( prev + Take.FirstFrame, Take.FirstFrame, Take.LastFrame );
					next = MathUtil.Clamp( next + Take.FirstFrame, Take.FirstFrame, Take.LastFrame );
				}
				
				Matrix prevT, nextT;

				if (useDelta) {
					Take.GetDeltaKey( prev, node, out prevT );
					Take.GetDeltaKey( next, node, out nextT );
				} else {
					Take.GetKey( prev, node, out prevT );
					Take.GetKey( next, node, out nextT );
				}

				transform = AnimBlendUtils.Lerp( prevT, nextT, weight );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="blendMode"></param>
		public AnimationTrack( Scene scene, string channel, AnimationBlendMode blendMode ) : base(scene, channel, blendMode)
		{
			trackTime			=	new TimeSpan(0);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		/// <param name="destination"></param>
		public override bool Evaluate ( GameTime gameTime, Matrix[] destination )
		{
			//	update sequence twice 
			//	to avoid stucking 
			//	on extremely short clips.
			UpdateSequence();
			UpdateSequence();

			//	apply transofrms :
			bool r = ApplyTransforms( destination );

			//	advance time :
			trackTime += gameTime.Elapsed;

			return r;
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="clip"></param>
		/// <param name="immediate"></param>
		/// <param name="looped"></param>
		/// <param name="crossfade"></param>
		public void Sequence ( string takeName, bool immediate, bool looped, bool hold = false )
		{
			var take = scene.Takes[ takeName ];

			Log.Verbose(" seq : {0} {1} {2}", takeName, immediate ? "immediate":"", looped ? "looped":"" );

			if (take==null) {
				Log.Warning("Take '{0}' does not exist", takeName );
				return;
			}


			if (currentAnim==null || immediate) {

				var anim = new Animation ( trackTime, take, scene.TimeMode, looped, hold );
	
				currentAnim		=	anim;
				pendingAnim		=	null;

			} else {

				var anim = new Animation ( currentAnim.End, take, scene.TimeMode, looped, hold );

				pendingAnim		=	anim;
			}
		}



		/*-----------------------------------------------------------------------------------------
		 * 
		 *  Internal stuff :
		 *  
		-----------------------------------------------------------------------------------------*/

		bool ApplyTransforms ( Matrix[] destination )
		{
			if ( currentAnim==null || Weight==0 ) {
				return false; // bypass track
			}

			bool additive = blendMode==AnimationBlendMode.Additive;


			for ( int chIdx = 0; chIdx < channelIndices.Length; chIdx++ ) {
				
				int nodeIndex	= channelIndices[ chIdx ];
				var weight		= Weight;

				Matrix dst		=	destination[nodeIndex];
				Matrix src;

				if (additive) {

					currentAnim.GetKey( nodeIndex, trackTime, true, out src );
					dst = AnimBlendUtils.Lerp( dst, dst * src, weight );

				} else {

					currentAnim.GetKey( nodeIndex, trackTime, false, out src );
					dst = AnimBlendUtils.Lerp( dst, src, weight );

				}

				destination[nodeIndex] = dst;
			}

			return true;
		}



		/// <summary>
		/// Update sequenced animations
		/// </summary>
		void UpdateSequence ()
		{
			if (currentAnim!=null) {

				int frameCount = currentAnim.Take.FrameCount;
			
				if ( trackTime >= currentAnim.End ) {

					if ( pendingAnim!=null ) {

						currentAnim = pendingAnim;
						pendingAnim = null;

					} else {

						if ( !currentAnim.Looped && !currentAnim.Hold ) {
							currentAnim = null;
						}
					}
				} //*/
			}
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *  Lua API :
		 *  
		-----------------------------------------------------------------------------------------*/

		[LuaApi("sequence")]
		int Sequence ( LuaState L )
		{
			using ( new LuaStackGuard( L ) ) {

				// get take
				Lua.LuaPushString(L, "take");
				Lua.LuaGetTable(L, 1);
				var take = Lua.LuaToString(L, -1).ToString();
				Lua.LuaPop(L, 1);

				// get loop
				Lua.LuaPushString(L, "loop");
				Lua.LuaGetTable(L, 1);
				var loop = Lua.LuaToBoolean(L, -1)!=0;
				Lua.LuaPop(L, 1);

				//	get hold
				Lua.LuaPushString(L, "hold");
				Lua.LuaGetTable(L, 1);
				var hold = Lua.LuaToBoolean(L, -1)!=0;
				Lua.LuaPop(L, 1);

				//	get crossfade, negative value means sequencing
				Lua.LuaPushString(L, "crossfade");
				Lua.LuaGetTable(L, 1);
				var crossfade = (float)Lua.LuaToNumber(L, -1);
				if (Lua.LuaIsNil(L,-1)) {
					crossfade = -1;
				}
				Lua.LuaPop(L, 1);
				
				Sequence( take, crossfade==0, loop, hold );
			}

			return 0;
		}

	}
}
