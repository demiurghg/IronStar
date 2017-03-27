using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Core {
	public interface IStorable {
		void Write ( BinaryWriter writer );
		void Read  ( BinaryReader reader, float lerpFactor );
	}
}
