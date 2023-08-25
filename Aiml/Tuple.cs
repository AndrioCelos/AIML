using System.Collections;

namespace Aiml;
/// <summary>Represents a set of variable bindings in the process of resolving a <see cref="Tags.Select"/> query.</summary>
/// <remarks>This class is represented as a singly-linked list.</remarks>
public class Tuple : IEnumerable<KeyValuePair<string, string>> {
	public string Key { get; }
	public string Value { get; }
	public Tuple? Next { get; }

	public Tuple(string key, string value, Tuple? next) {
		this.Key = key;
		this.Value = value;
		this.Next = next;
	}
	public Tuple(string key, string value) : this(key, value, null) { }

	public string? this[string key] {
		get {
			var tuple = this;
			do {
				if (key.Equals(tuple.Key, StringComparison.OrdinalIgnoreCase)) return tuple.Value;
				tuple = tuple.Next;
			} while (tuple is not null);
			return null;
		}
	}

	/// <summary>Returns an encoded string containing the contents of the specified variables. The encoded string shall not contain spaces.</summary>
	public string Encode(IReadOnlyCollection<string>? visibleVars) {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);
		foreach (var e in this) {
			if (visibleVars is not null && !visibleVars.Contains(e.Key)) continue;
			writer.Write(e.Key);
			writer.Write(e.Value);
		}
		return Convert.ToBase64String(ms.GetBuffer(), 0, (int) ms.Position);
	}

	/// <summary>Returns the value of the specified variable from an encoded string, or <see langword="null"/> if the variable is not bound in the encoded string.</summary>
	public static string? GetFromEncoded(string encoded, string key) {
		var array = Convert.FromBase64String(encoded);
		using var ms = new MemoryStream(array);
		using var reader = new BinaryReader(ms);
		while (ms.Position < ms.Length) {
			var key2 = reader.ReadString();
			var value = reader.ReadString();
			if (key.Equals(key2, StringComparison.OrdinalIgnoreCase)) return value;
		}
		return null;
	}

	public override string ToString() => $"[ {string.Join(", ", from p in this select $"({p.Key}, {p.Value})")} ]";

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
		var tuple = this;
		do {
			yield return new(tuple.Key, tuple.Value);
			tuple = tuple.Next;
		} while (tuple != null);
	}
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
