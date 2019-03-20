using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.Maps {
	/// <summary>Implements the <code>singular</code> map from Pandorabots, which maps English nouns to their singular forms.</summary>
	public class SingularMap : Map {
		public Inflector Inflector { get; }

		public SingularMap(Inflector inflector) {
			this.Inflector = inflector;
		}

		public override string? this[string key] => this.Inflector.Singularize(key);
	}
}
