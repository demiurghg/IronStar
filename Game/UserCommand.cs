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
	public struct UserCommand {

		public float Yaw;
		public float Pitch;
		public float Roll;

		public float MoveForward;
		public float MoveRight;
		public float MoveUp;

		public UserAction Action;



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
			int size = Marshal.SizeOf(userCmd);
			byte[] array = new byte[size];

			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(userCmd, ptr, true);
			Marshal.Copy(ptr, array, 0, size);
			Marshal.FreeHGlobal(ptr);
			return array;
		}


		/// <summary>
		/// Gets user command from bytes
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		static public UserCommand FromBytes(byte[] array) 
		{
			var userCmd = new UserCommand();

			int size = Marshal.SizeOf(userCmd);
			IntPtr ptr = Marshal.AllocHGlobal(size);

			Marshal.Copy(array, 0, ptr, size);

			userCmd = (UserCommand)Marshal.PtrToStructure(ptr, userCmd.GetType());
			Marshal.FreeHGlobal(ptr);

			return userCmd;
		}



		public static void FireUserCommandAction ( UserCommand oldCmd, UserCommand newCmd, Action<UserAction> ctrlAction )
		{
			var values = Enum.GetValues( typeof(UserAction) ).Cast<UserAction>().ToArray();

			foreach ( var flag in values ) {
				if ( newCmd.Action.HasFlag(flag) && !oldCmd.Action.HasFlag(flag) ) {
					ctrlAction(flag);
				}
			}
		}



		public override string ToString ()
		{
			return string.Format("Angles:[{0} {1} {2}] Ctrl:[{3}]", Yaw, Pitch, Roll, Action );
		}
	}
}
