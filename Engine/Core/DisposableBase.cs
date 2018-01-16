﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core {

	/// <summary>
	/// For better implementation: http://codereview.stackexchange.com/questions/32380/dispose-pattern-disposableobject
	/// </summary>
	public class DisposableBase : IDisposable {
		
		/// <summary>
		/// Flag: Has object was completly disposed?
		/// </summary>
		public bool IsDisposed { get; private set; }


		/// <summary>
		/// 
		/// </summary>
		public void Dispose ()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose ( bool disposing )
		{
			if (IsDisposed) {
				return;
			}

			if (disposing) {
				//	dispose managed stuff
			}

			//	dispose unmanaged stuff

			//	mark as disposed
			IsDisposed	=	true;
		}



		~DisposableBase()
		{
			Dispose( false );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		public static void SafeDispose<T> ( ref T obj ) where T: IDisposable
		{
			if ( obj != null ) {
				obj.Dispose();
				obj = default(T);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		protected void ThrowIfDisposed()
		{
			if (IsDisposed) {
				throw new ObjectDisposedException("Object is already disposed.");
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		public static void SafeDispose<T> ( ref T[] objArray ) where T: IDisposable
		{
			if ( objArray==null ) {
				return;
			}

			foreach ( var obj in objArray ) {
				obj?.Dispose();
			}

			objArray	=	null;
		}


		public static void SafeDispose<T>( ref T[,] objArray ) where T: IDisposable
		{
			if ( objArray==null ) {
				return;
			}

			foreach ( var obj in objArray ) {
				obj?.Dispose();
			}

			objArray = null;
		}
	}
}
