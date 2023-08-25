using System.Collections;

namespace Aiml.Sets;
/// <summary>Represents an immutable set populated from a local file.</summary>
public class StringSet : Set, IReadOnlyCollection<string> {
	private readonly HashSet<string> elements;
	/// <summary>The <see cref="StringComparer"/> used to calculate hash codes for the phrases in this set.</summary>
	public IEqualityComparer<string> Comparer => this.elements.Comparer;

	public override int MaxWords { get; }

	public int Count => this.elements.Count;

	/// <summary>Creates a new <see cref="StringSet"/> with elements copied from a given collection, with the given comparer.</summary>
	/// <param name="elements">The collection from which to copy elements into this set.</param>
	/// <param name="comparer">The <see cref="IEqualityComparer{string}"/> to be used to compare phrases</param>
	public StringSet(IEnumerable<string> elements, IEqualityComparer<string> comparer) {
		this.elements = new HashSet<string>(comparer);
		foreach (var phrase in elements) {
			var words = phrase.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
			this.elements.Add(string.Join(" ", words));
			this.MaxWords = Math.Max(this.MaxWords, words.Length);
		}
	}

	public override bool Contains(string phrase) => this.elements.Contains(phrase);
	public IEnumerator<string> GetEnumerator() => this.elements.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
