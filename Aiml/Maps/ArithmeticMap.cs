using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.Maps {

	/// <summary>Represents a map that maps integers using an addition. It implements the <code>predecessor</code> and <code>successor</code> maps from Pandorabots.</summary>
	internal class ArithmeticMap : Map {
		public int Addend { get; }

		public ArithmeticMap(int addend) {
			this.Addend = addend;
		}

		public override string? this[string key] {
			get {
				if (int.TryParse(key, out int value)) return (value + this.Addend).ToString();
				return null;
			}
		}
	}
}
