using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using IronStar.Entities;
using IronStar.SFX;

namespace IronStar {

	public class PlayerState : IStorable{

		public readonly static PlayerState NullState = new PlayerState();

		public short		Health		;
		public short		Armor		;

		public WeaponType	Weapon1		;
		public WeaponType	Weapon2		;

		public short		WeaponAmmo1	;
		public short		WeaponAmmo2	;


		/// <summary>
		/// View space model
		/// </summary>
		public short ViewModel {
			get { return model; }
			set { 
				modelDirty = model != value; 
				model = value; 
			}
		}
		private short model = -1;
		private bool modelDirty = true;

		/// <summary>
		/// View space special effect
		/// </summary>
		public short ViewSfx {
			get { return sfx; }
			set { 
				sfxDirty = sfx != value; 
				sfx = value; 
			}
		}
		private short sfx = -1;
		private bool sfxDirty = true;

		public FXInstance FXInstance { get; private set; }
		public ModelInstance ModelInstance { get; private set; }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public void Write ( BinaryWriter writer )
		{
			writer.Write( Health		);
			writer.Write( Armor			);
			writer.Write( ViewModel		);
			writer.Write( (byte)Weapon1	);
			writer.Write( (byte)Weapon2	);
			writer.Write( WeaponAmmo1	);
			writer.Write( WeaponAmmo2	);
			writer.Write( ViewModel	);
			writer.Write( ViewSfx	);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		public void Read ( BinaryReader reader, float lerpFactor )
		{
			Health		=	reader.ReadInt16();
			Armor		=	reader.ReadInt16();
			ViewModel	=	reader.ReadInt16();
			Weapon1		=	(WeaponType)reader.ReadByte();
			Weapon2		=	(WeaponType)reader.ReadByte();
			WeaponAmmo1	=	reader.ReadInt16();
			WeaponAmmo2	=	reader.ReadInt16();
			ViewModel	=	reader.ReadInt16();
			ViewSfx		=	reader.ReadInt16();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxPlayback"></param>
		public void UpdateRenderState ( Entity playerEntity, FXPlayback fxPlayback, ModelManager modelManager )
		{
			/*if (sfxDirty) {
				sfxDirty = false;

				FXInstance?.Kill();
				FXInstance = null;

				if (sfx>0) {
					var fxe = new FXEvent( sfx, ID, Position, LinearVelocity, Rotation );
					FXInstance = fxPlayback.RunFX( fxe, true );
				}
			}

			if (modelDirty) {
				modelDirty = false;

				ModelInstance?.Kill();
				ModelInstance	=	null;

				if (model>0) {
					ModelInstance	=	modelManager.AddModel( model, playerEntity );
				}
			} */
		}
		
	}
}
