using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;

namespace Fusion.Build.Processors.Ubershaders
{
	class IncludeHandler : Include 
	{
		readonly IBuildContext buildContext;
		readonly List<string> includes;
		
		public IncludeHandler ( IBuildContext buildContext, List<string> includes )
		{
			this.includes		=	includes;
			this.buildContext	=	buildContext;
		}


		public Stream Open( IncludeType type, string fileName, Stream parentStream )
		{
			lock (includes) 
			{
				includes.Add( fileName );

				try 
				{
					return new MemoryStream( File.ReadAllBytes( buildContext.ResolveContentPath( fileName ) ) );
				}
				catch (Exception e)
				{
					Log.Error( e.Message );
					return null;
				}
			}
		}


		public void Close( Stream stream )
		{
			stream.Close();
		}


		IDisposable ICallbackable.Shadow 
		{
			get; set;
		}


		public void Dispose ()
		{
		}
	}
}
