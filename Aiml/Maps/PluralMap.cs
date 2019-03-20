using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.Maps {
	/// <summary>Implements the <code>singular</code> map from Pandorabots, which maps English nouns to their plural forms.</summary>
	public class PluralMap : Map {
		public Inflector Inflector { get; }

		public PluralMap(Inflector inflector) {
			this.Inflector = inflector;
		}

		public override string? this[string key] => this.Inflector.Pluralize(key);
	}
}
