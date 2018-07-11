using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Fusion.Core.Mathematics;
using IronStar.Core;
using Fusion;

namespace IronStar {
	
	/// <summary>
	/// Represents an instant user action and intention.
	/// </summary>
	public class UserCommand {

		public float Yaw;
		public float Pitch;
		public float Roll;

		public float MoveForward;
		public float MoveRight;
		public float MoveUp;

		public UserAction Action;

		public byte	 Weapon;

		public float DYaw;
		public float DPitch;


		public void SetAnglesFromQuaternion ( Quaternion q )
		{
			Matrix.RotationQuaternion( q ).ToAngles( out Yaw, out Pitch, out Roll );
		}

		
		/// <summary>
		/// Gets user command's bytes.
		/// </summary>
		/// <param name="userCmd"></param>
		/// <returns></returns>
		static public byte[] GetBytes(UserCommand userCmd) 
		{
			var array = new byte[12+12+1];

			using ( var writer = new BinaryWriter( new MemoryStream( array ) ) ) {

				writer.Write( userCmd.Yaw	);
				writer.Write( userCmd.Pitch );
				writer.Write( userCmd.Roll	);

				writer.Write( userCmd.MoveForward	);
				writer.Write( userCmd.MoveRight		);
				writer.Write( userCmd.MoveUp		);

				writer.Write( (byte)userCmd.Action	);
				writer.Write( (byte)userCmd.Weapon	);
			}

			return array;
		}


		/// <summary>
		/// Gets user command from bytes
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		static public UserCommand FromBytes(byte[] array) 
		{
			using ( var reader = new BinaryReader( new MemoryStream( array ) ) ) {

				var userCmd = new UserCommand();

				userCmd.Yaw			=	reader.ReadSingle();
				userCmd.Pitch		=	reader.ReadSingle();
				userCmd.Roll		=	reader.ReadSingle();

				userCmd.MoveForward	=	reader.ReadSingle();
				userCmd.MoveRight	=	reader.ReadSingle();
				userCmd.MoveUp		=	reader.ReadSingle();

				userCmd.Action		=	(UserAction)reader.ReadByte();
				userCmd.Weapon		=	reader.ReadByte();

				return userCmd;
			}
		}



		public static void FireUserCommandAction ( UserCommand oldCmd, UserCommand newCmd, Action<UserAction> beginAction, Action<UserAction> endAction )
		{
			var values = Enum.GetValues( typeof(UserAction) ).Cast<UserAction>().ToArray();

			foreach ( var flag in values ) {
				if ( newCmd.Action.HasFlag(flag) && !oldCmd.Action.HasFlag(flag) ) {
					beginAction(flag);
				}
				if ( !newCmd.Action.HasFlag(flag) && oldCmd.Action.HasFlag(flag) ) {
					endAction(flag);
				}
			}
		}



		public override string ToString ()
		{
			return string.Format("Angles:[{0} {1} {2}] Ctrl:[{3}]", Yaw, Pitch, Roll, Action );
		}
	}
}
