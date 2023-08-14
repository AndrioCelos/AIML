using System.Collections;
using System.Collections.ObjectModel;

namespace Aiml;
/// <summary>
/// Represents a set of variables and values that can be returned by a select tag.
/// </summary>
public class Tuple : Dictionary<string, string> {
	public int Index { get; } = tuples.Count;

	private readonly HashSet<string> visibleVars;
	/// <summary>
	///     The set of variables that participate in comparison and hash functions for this tuple.
	/// </summary>
	public ReadOnlySet<string> VisibleVars;

	private static readonly List<Tuple> tuples = new();
	public static ReadOnlyCollection<Tuple> Tuples { get; } = tuples.AsReadOnly();

	public Tuple(HashSet<string> visibleVars) : base(visibleVars.Comparer) {
		this.visibleVars = visibleVars;
		this.VisibleVars = new ReadOnlySet<string>(this.visibleVars);
		tuples.Add(this);
	}
	public Tuple(Tuple source) : base(source, source.Comparer) {
		this.visibleVars = source.visibleVars;
		this.VisibleVars = new ReadOnlySet<string>(this.visibleVars);
		tuples.Add(this);
	}

	public override bool Equals(object other) {
		if (other is not Tuple) return false;
		var otherTuple = (Tuple) other;

		if (!this.visibleVars.SetEquals(otherTuple.visibleVars)) return false;

		foreach (var name in this.visibleVars) {
			if (!this.Comparer.Equals(this[name], otherTuple[name])) return false;
		}

		return true;
	}

	public override int GetHashCode() {
		var hashCode = new HashCode();
		foreach (var name in this.visibleVars) {
			hashCode.Add(name, null);
			if (this.TryGetValue(name, out var value))
				hashCode.Add(value, null);
		}
		return hashCode.ToHashCode();
	}
}

public class ReadOnlySet<T>(ISet<T> set) : ISet<T>, IEnumerable<T> {
	protected ISet<T> Set { get; } = set;

	public int Count => this.Set.Count;
	public bool IsReadOnly => true;

	public IEnumerator<T> GetEnumerator() => this.Set.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => this.Set.GetEnumerator();

	public bool Add(T item) => throw new NotSupportedException();
	void ICollection<T>.Add(T item) => throw new NotSupportedException();
	public bool Remove(T item) => throw new NotSupportedException();
	public void Clear() => throw new NotSupportedException();

	public bool Contains(T item) => this.Set.Contains(item);
	public void CopyTo(T[] array, int arrayIndex) => this.Set.CopyTo(array, arrayIndex);

	public void UnionWith(IEnumerable<T> other) => throw new NotSupportedException();
	public void IntersectWith(IEnumerable<T> other) => throw new NotSupportedException();
	public void ExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
	public void SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException();

	public bool IsSubsetOf(IEnumerable<T> other) => this.Set.IsSubsetOf(other);
	public bool IsSupersetOf(IEnumerable<T> other) => this.Set.IsSupersetOf(other);
	public bool IsProperSupersetOf(IEnumerable<T> other) => this.Set.IsProperSupersetOf(other);
	public bool IsProperSubsetOf(IEnumerable<T> other) => this.Set.IsProperSubsetOf(other);
	public bool Overlaps(IEnumerable<T> other) => this.Set.Overlaps(other);
	public bool SetEquals(IEnumerable<T> other) => this.Set.SetEquals(other);
}
