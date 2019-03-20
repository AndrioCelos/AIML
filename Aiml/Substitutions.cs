using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Aiml {
	public class SubstitutionList : IList<Substitution> {
		private List<Substitution> substitutions;
		private Regex regex;

		public SubstitutionList() {
			this.substitutions = new List<Substitution>();
		}
		public SubstitutionList(int capacity) {
			this.substitutions = new List<Substitution>(capacity);
		}

		public Substitution this[int index] {
			get { return this.substitutions[index]; }
			set {
				this.substitutions[index] = value;
				this.regex = null;
			}
		}

		public void CompileRegex() {
			StringBuilder builder = new StringBuilder("(");
			foreach (var item in this.substitutions) {
				if (builder.Length != 1) builder.Append(")|(");
				builder.Append(item.Pattern);
			}
			builder.Append(")");
			this.regex = new Regex(builder.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase);
		}

		public string Apply(string text) {
			if (this.substitutions.Count == 0) return text;
			if (this.regex == null) this.CompileRegex();

			return this.regex.Replace(" " + text + " ", delegate (Match match) {
				for (int i = 0; i < this.substitutions.Count; ++i) {
					if (match.Groups[i + 1].Success) {
						var replacement = this.substitutions[i].Replacement;
						if (this.substitutions[i].startSpace && !match.Value.StartsWith(" ")) replacement = replacement.TrimStart();
						if (this.substitutions[i].endSpace   && !match.Value.EndsWith(" ")  ) replacement = replacement.TrimEnd();
						return replacement;
					}
				}
				return "";
			}).Trim();
		}

		public int Count => this.substitutions.Count;
		public bool IsReadOnly => false;


		public void Add(Substitution item) {
			this.substitutions.Add(item);
			this.regex = null;
		}
		public void AddRange(IEnumerable<Substitution> items) {
			this.substitutions.AddRange(items);
			this.regex = null;
		}
		public void Insert(int index, Substitution item) {
			this.substitutions.Insert(index, item);
			this.regex = null;
		}
		public bool Remove(Substitution item) {
			bool result = this.substitutions.Remove(item);
			if (result) this.regex = null;
			return result;
		}
		public void RemoveAt(int index) {
			this.substitutions.RemoveAt(index);
			this.regex = null;
		}
		public void Clear() {
			this.substitutions.Clear();
			this.regex = null;
		}

		public bool Contains(Substitution item) => this.substitutions.Contains(item);
		public void CopyTo(Substitution[] array, int arrayIndex) => this.substitutions.CopyTo(array, arrayIndex);
		public IEnumerator<Substitution> GetEnumerator() => this.substitutions.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public int IndexOf(Substitution item) => this.substitutions.IndexOf(item);
	}

	[JsonArray] [JsonConverter(typeof(Config.SubstitutionConverter))]
	public class Substitution {
		public string Pattern { get; }
		public string Replacement { get; }
		internal bool startSpace;
		internal bool endSpace;

		public Substitution(string original, string replacement) {
			this.Pattern = Regex.Escape(original.Trim());
			this.Replacement = replacement;
			// Spaces surrounding the pattern indicate word boundaries.
			if (original.StartsWith(" ")) {
				// If there's a space there, it will match the space. If there isn't a space there, such as if it overlaps a previous substitution, it will still match.
				this.Pattern = @"(?: |(?<!\S))" + this.Pattern;
				if (replacement.StartsWith(" ")) this.startSpace = true;
			}
			if (original.EndsWith(" ")) {
				this.Pattern += @"(?: |(?!\S))";
				if (replacement.EndsWith(" ")) this.endSpace = true;
			}
		}
	}
}
