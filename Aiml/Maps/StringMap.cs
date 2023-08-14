namespace Aiml.Maps;
/// <summary>Represents an immutable map populated from a dictionary read from a file.</summary>
/// <param name="dictionary">The dictionary from which to copy elements.</param>
/// <param name="comparer">The <see cref="IEqualityComparer{string}"/> to be used to compare phrases.</param>
public class StringMap(IDictionary<string, string> dictionary, IEqualityComparer<string> comparer) : Map {
	private readonly Dictionary<string, string> dictionary = new(dictionary, comparer);

	public override string? this[string key] {
		get {
			this.dictionary.TryGetValue(key, out var value);
			return value;
		}
	}
}
