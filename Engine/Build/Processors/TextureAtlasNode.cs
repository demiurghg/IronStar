using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Mathematics;
using Fusion.Core.Shell;
using Fusion.Engine.Imaging;
using Newtonsoft.Json;

namespace Fusion.Build.Processors {

	/*-----------------------------------------------------------------------------------------
		* 
		*	Layouting stuff :
		*	http://cgi.csc.liv.ac.uk/~epa/surveyhtml.html
		*	http://www.mrashid.info/blog/stacking-squares-problem.php
		*	
		*	http://blackpawn.com/texts/lightmaps/
		* 
	-----------------------------------------------------------------------------------------*/

	class AtlasNode {
		public AtlasNode left;
		public AtlasNode right;
		public TextureAtlasFrame tex;
		public int x;
		public int y;
		public int width;
		public int height;
		public bool in_use;
		public int padding;


		public override string ToString ()
		{
			return string.Format("{0} {1} {2} {3}", x,y, width, height);
		}



		public AtlasNode (int x, int y, int width, int height, int padding)
		{
			left = null;
			right = null;
			tex = null;
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
			in_use = false;
			this.padding = padding;
		}



		public AtlasNode Insert( TextureAtlasFrame frame )
		{
			if (left!=null) {
				AtlasNode rv;
					
				if (right==null) {
					throw new InvalidOperationException("AtlasNode(): error");
				}

				rv = left.Insert(frame);
					
				if (rv==null) {
					rv = right.Insert(frame);
				}
					
				return rv;
			}

			int img_width  = frame.Width  + padding * 2;
			int img_height = frame.Height + padding * 2;

			if (in_use || img_width > width || img_height > height) {
				return null;
			}

			if (img_width == width && img_height == height) {
				in_use = true;
				tex = frame;
				tex.Location.X = x + padding;
				tex.Location.Y = y + padding;
				return this;
			}

			if (width - img_width > height - img_height) {
				/* extend to the right */
				left = new AtlasNode(x, y, img_width, height, padding);
				right = new AtlasNode(x + img_width, y,
										width - img_width, height, padding);
			} else {
				/* extend to bottom */
				left = new AtlasNode(x, y, width, img_height, padding);
				right = new AtlasNode(x, y + img_height,
										width, height - img_height, padding);
			}

			return left.Insert(frame);
		}



		public void WriteLayout ( BinaryWriter bw ) 
		{
			if (tex!=null) {
				var name = Path.GetFileNameWithoutExtension( (string)tex.Name );
				//stringBuilder.AppendFormat("{0} {1} {2} {3} {4}\r\n", name, x + padding, y + padding, tex.Width, tex.Height );
				bw.Write( name );
				bw.Write( x + padding );
				bw.Write( y + padding );
				bw.Write( tex.Width );
				bw.Write( tex.Height );
			}

			if (left!=null)  left.WriteLayout( bw );
			if (right!=null) right.WriteLayout( bw );
		}
	}
}
