using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Core.Extensions;


namespace Fusion.Engine.Graphics {

	public sealed class VTSegmentCollection : Dictionary<string,VTSegment> {
		
		public VTSegmentCollection() {}
		public VTSegmentCollection( int capacity ) : base(capacity) {}
		public VTSegmentCollection( IDictionary<string, VTSegment> dictionary ) : base(dictionary) {}

	}
}
