using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Aiml;
public class SubstitutionList : IList<Substitution> {
	private readonly List<Substitution> substitutions;
	private Regex? regex;
	private static readonly Regex substitutionRegex = new(@"\$(?:(\d+)|\{([^}]*)\}|([$&`'+_]))", RegexOptions.Compiled);

	public SubstitutionList() => this.substitutions = new List<Substitution>();
	public SubstitutionList(int capacity) => this.substitutions = new List<Substitution>(capacity);

	public Substitution this[int index] {
		get => this.substitutions[index];
		set {
			this.substitutions[index] = value;
			this.regex = null;
		}
	}

#if NET5_0_OR_GREATER
	[MemberNotNull(nameof(regex))]
#endif
	public void CompileRegex() {
		var groupIndex = 1;
		var builder = new StringBuilder("(");
		foreach (var item in this.substitutions) {
			item.groupIndex = groupIndex;
			if (builder.Length != 1) builder.Append(")|(");
			builder.Append(item.Pattern);
			if (item.IsRegex) {
				// To work out the number of capturing groups in the pattern, run it against the empty string.
				// The '|' ensures we will get a successful match; otherwise we would not get group information.
				var match = Regex.Match("", "|" + item.Pattern);
				groupIndex += match.Groups.Count;  // Deliberately counting group 0.
			} else {
				++groupIndex;
			}
		}
		builder.Append(')');
		this.regex = new Regex(builder.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase);
	}

	public string Apply(string text) {
		if (this.substitutions.Count == 0) return text;
		if (this.regex == null) this.CompileRegex();

		return this.regex!.Replace(" " + text + " ", delegate (Match match) {
			foreach (var substitution in this.substitutions) {
				if (match.Groups[substitution.groupIndex].Success) {
					var replacement = substitution.Replacement;

					if (substitution.IsRegex)
						// Process substitution tokens in the replacement.
						replacement = substitutionRegex.Replace(replacement, m =>
							m.Groups[1].Success ? match.Groups[substitution.groupIndex + int.Parse(m.Groups[1].Value)].Value :
							m.Groups[2].Success ? (int.TryParse(m.Groups[2].Value, out var n) ? match.Groups[substitution.groupIndex + n].Value : match.Groups[m.Groups[2].Value].Value) :
							m.Groups[3].Value[0] switch {
								'$' => "$",
								'&' => match.Value,
								'`' => text[..(match.Index - 1)],
								'\'' => text[(match.Index + match.Length - 1)..],
								'+' => match.Groups[^1].Value,
								'_' => text,
								_ => m.Value
							});

					if (substitution.startSpace && !match.Value.StartsWith(" ")) replacement = replacement.TrimStart();
					if (substitution.endSpace   && !match.Value.EndsWith(" ")  ) replacement = replacement.TrimEnd();
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
		var result = this.substitutions.Remove(item);
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

[JsonArray, JsonConverter(typeof(Config.SubstitutionConverter))]
public class Substitution {
	public bool IsRegex { get; }
	public string Pattern { get; }
	public string Replacement { get; }
	internal int groupIndex;
	internal bool startSpace;
	internal bool endSpace;

	public Substitution(string original, string replacement, bool regex) {
		this.IsRegex = regex;
		if (regex) {
			this.Pattern = original.Trim();
			this.Replacement = replacement;
		} else {
			this.Pattern = Regex.Escape(original.Trim());
			this.Replacement = replacement.Replace("$", "$$");
		}
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
	public Substitution(string original, string replacement) : this(original, replacement, false) { }
}
