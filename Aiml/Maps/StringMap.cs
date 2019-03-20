using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.Maps {
	/// <summary>Represents an immutable map populated from a file.</summary>
	public class StringMap : Map {
		private readonly Dictionary<string, string> dictionary;

		/// <summary>Initialises a new <see cref="StringMap"/> with elements copied from the given dictionary, and using the given comparer.</summary>
		/// <param name="dictionary">The dictionary from which to copy elements.</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{string}"/> to be used to compare phrases.</param>
		public StringMap(IDictionary<string, string> dictionary, IEqualityComparer<string> comparer) {
			this.dictionary = new Dictionary<string, string>(dictionary, comparer);
		}

		public override string? this[string key] {
			get {
				this.dictionary.TryGetValue(key, out var value);
				return value;
			}
		}
	}
}
