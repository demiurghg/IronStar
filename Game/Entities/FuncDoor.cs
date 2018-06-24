﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Extensions;
using IronStar.Core;
using IronStar.SFX;
using System.ComponentModel;
using Fusion.Development;
using System.Drawing.Design;
using Fusion;
using IronStar.Physics;
using Fusion.Core.Shell;

namespace IronStar.Entities {

	public class FuncDoor : EntityController {
		
		static Random rand = new Random();

		readonly string fxStart;
		readonly string fxMove;
		readonly string fxStop;
		readonly FuncDoorMode doorMode;
		readonly bool once;
		readonly GameWorld world;
		DoorState doorState = DoorState.Closed;

		readonly short model;

		KinematicModel kinematic;

		readonly int framesPerSecond	;
		readonly int openingStartFrame	;
		readonly int openingEndFrame	;
		readonly int closingStartFrame	;
		readonly int closingEndFrame	;
		readonly int waitingDelay;

		int activationCount = 0;
		float frame = 0;
		float waiting = 0;

		enum DoorState {
			Closed,
			Opening,
			Waiting,
			Closing,
		}


		public FuncDoor( Entity entity, GameWorld world, FuncDoorFactory factory ) : base(entity, world)
		{
			this.world	=	world;

			fxStart	=	factory.FXStart;
			fxMove	=	factory.FXMove;
			fxStop	=	factory.FXStop;

			model	=	world.Atoms[ factory.Model ];

			kinematic			=	world.Physics.AddKinematicModel( model, entity );

			once				=	factory.Once;
			doorMode			=	factory.DoorMode;

			framesPerSecond		=	factory.FramesPerSecond;
			openingStartFrame	=	factory.OpeningStartFrame;
			openingEndFrame		=	factory.OpeningEndFrame;
			closingStartFrame	=	factory.ClosingStartFrame;
			closingEndFrame		=	factory.ClosingEndFrame;
			waitingDelay		=	factory.WaitingDelay;

			frame				=	openingStartFrame;

			Reset();
		}



		public override void Killed()
		{
			world.Physics.Remove( kinematic );
		}


		public override void Activate( Entity activator )
		{
			if (once && activationCount>0) {
				return;
			}



		}


		public override bool Use( Entity user )
		{
			if (AllowUse) {
				doorState = DoorState.Opening;
				return true;
			} else {
				return false;
			}
		}


		public override bool AllowUse {
			get {
				return doorState==DoorState.Closed;
			}
		}


		public override void Reset()
		{
			activationCount	= 0;
		}


		public override void Update( float elapsedTime )
		{
			switch (doorState) {
				case DoorState.Closed:
					frame	=	openingStartFrame;
					break;

				case DoorState.Opening: 
					frame += elapsedTime * framesPerSecond;

					if (frame>openingEndFrame) {
						doorState	= DoorState.Waiting;
						frame		= openingEndFrame;
						waiting		= 0;
					}
					
					break;

				case DoorState.Waiting: 

					waiting += elapsedTime;

					if (waiting>waitingDelay/1000.0f) {
						doorState	= DoorState.Closing;
						frame		= closingStartFrame;
					}

					break;

				case DoorState.Closing: 
					frame += elapsedTime * framesPerSecond;

					if (frame>closingEndFrame) {
						doorState	= DoorState.Closed;
						frame		= closingEndFrame;
						waiting		= 0;
					}
					
					break;
			}

			Entity.AnimFrame	= frame;
			Entity.Model		= model;
		}
	}



	public enum FuncDoorMode {
		OpenAndCloseAfterDelay,
		ToggleOpenAndClose,
	}



	/// <summary>
	/// 
	/// </summary>
	public class FuncDoorFactory : EntityFactory {

		[AECategory("Appearance")]
		[Description("Name of the model")]
		[AEClassname("models")]
		public string Model  { get; set; } = "";


		[AECategory("Effects")]
		[Description("FX to play when door starts moving")]
		[AEClassname("fx")]
		public string FXStart { get; set; } = "";

		[AECategory("Effects")]
		[Description("FX to play when door is moving")]
		[AEClassname("fx")]
		public string FXMove { get; set; } = "";

		[AECategory("Effects")]
		[Description("FX to play when door stops moving")]
		[AEClassname("fx")]
		public string FXStop { get; set; } = "";


		[AECategory("Movement")]
		[Description("Indicated that door could be trigerred only once")]
		public bool Once { get; set; }

		[AECategory("Movement")]
		[Description("Door operation mode")]
		public FuncDoorMode DoorMode { get; set; } = FuncDoorMode.OpenAndCloseAfterDelay;

		[AECategory("Movement")]
		[Description("Min interval (msec) before door closes")]
		public int WaitingDelay { get; set; } = 500;


		[AECategory("Animation")]
		[Description("Animation frame rate")]
		public int FramesPerSecond { get; set; } = 30;

		[AECategory("Animation")]
		[Description("Opening animation start inclusive frame")]
		public int OpeningStartFrame { get; set; } = 0;

		[AECategory("Animation")]
		[Description("Opening animation end inclusive frame")]
		public int OpeningEndFrame { get; set; } = 15;

		[AECategory("Animation")]
		[Description("Closing animation start inclusive frame")]
		public int ClosingStartFrame { get; set; } = 15;

		[AECategory("Animation")]
		[Description("Closing animation end inclusive frame")]
		public int ClosingEndFrame { get; set; } = 30;


		public override EntityController Spawn( Entity entity, GameWorld world )
		{
			return new FuncDoor( entity, world, this );
		}
	}
}
