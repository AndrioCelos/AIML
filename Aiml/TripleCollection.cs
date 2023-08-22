// Portions of this class were adapted from Program Y, available under license. https://github.com/keiffster/program-y/blob/master/src/programy/rdf/collection.py
// Program Y is copyright (c) 2016-2020 Keith Sterling http://www.keithsterling.com

using System.Collections;

namespace Aiml;
/// <summary>Represents a collection of RDF <see cref="Triple"/> instances and supports querying the collection.</summary>
/// <param name="comparer">An <see cref="IEqualityComparer{T}"/> that determines equality of triple values.</param>
public class TripleCollection(IEqualityComparer<string> comparer) : ICollection<Triple>, IReadOnlyCollection<Triple> {
	private readonly IEqualityComparer<string> comparer = comparer;
	private readonly Dictionary<string, Dictionary<string, List<Triple>>> bySubject = new(comparer);
	private readonly Dictionary<string, Dictionary<string, List<Triple>>> byObject = new(comparer);

	/// <summary>Returns the number of triples in this collection.</summary>
	public int Count { get; private set; }

	/// <summary>Returns an <see cref="IEnumerator"/> that yields all triples in this collection.</summary>
	public IEnumerator<Triple> GetEnumerator() {
		foreach (var subjIndex in this.bySubject.Values) {
			foreach (var list in subjIndex.Values) {
				foreach (var t in list) yield return t;
			}
		}
	}

	/// <summary>Checks whether a triple matching the specified triple is in this collection.</summary>
	public bool Contains(Triple triple) => this.bySubject.TryGetValue(triple.Subject, out var subjIndex)
		&& subjIndex.TryGetValue(triple.Predicate, out var list)
		&& list.Contains(triple);

	/// <summary>Adds a triple with the specified values to this collection if it is not already present.</summary>
	/// <returns><see langword="true"/> if the triple was added; <see langword="false"/> if the triple was already present.</returns>
	public bool Add(string subj, string pred, string obj) => this.Add(new(subj, pred, obj));
	/// <summary>Adds the specified triple to this collection if no matching triple is already present.</summary>
	/// <returns><see langword="true"/> if the triple was added; <see langword="false"/> if the triple was already present.</returns>
	public bool Add(Triple triple) {
		// Add to the subject index.
		if (!this.bySubject.TryGetValue(triple.Subject, out var subjIndex)) {
			this.bySubject[triple.Subject] = subjIndex = new();
			subjIndex[triple.Predicate] = new() { triple };
		} else {
			if (!subjIndex.TryGetValue(triple.Predicate, out var list))
				subjIndex[triple.Predicate] = list = new() { triple };
			else {
				if (list.Any(t => this.comparer.Equals(t.Object, triple.Object)))
					return false;  // Already in the collection.
				list.Add(triple);
			}
		}
		// Add to the object index.
		if (!this.byObject.TryGetValue(triple.Object, out var objIndex)) {
			this.byObject[triple.Object] = objIndex = new();
			objIndex[triple.Predicate] = new() { triple };
		} else {
			if (!objIndex.TryGetValue(triple.Predicate, out var list))
				objIndex[triple.Predicate] = list = new();
			list.Add(triple);
		}

		this.Count++;
		return true;
	}

	/// <summary>Removes the triple with the specified values from this collection if one is present.</summary>
	/// <returns><see langword="true"/> if the triple was removed; <see langword="false"/> if the triple was not present.</returns>
	public bool Remove(string subj, string pred, string obj) {
		// Remove from the subject index.
		if (this.bySubject.TryGetValue(subj, out var subjIndex) && subjIndex.TryGetValue(pred, out var list)) {
			var i = list.FindIndex(t => this.comparer.Equals(t.Object, obj));
			if (i < 0) return false;
			if (list.Count == 1) {
				if (subjIndex.Count == 1) this.bySubject.Remove(subj);
				else subjIndex.Remove(pred);
			} else
				list.RemoveAt(i);
		} else
			return false;

		this.RemoveFromObjectIndex(subj, pred, obj);

		this.Count--;
		return true;
	}
	/// <summary>Removes the triple with the specified values from the <see cref="byObject"/> index.</summary>
	private void RemoveFromObjectIndex(string subj, string pred, string obj) {
		if (this.byObject.TryGetValue(obj, out var objIndex) && objIndex.TryGetValue(pred, out var list)) {
			var i = list.FindIndex(t => this.comparer.Equals(t.Subject, subj));
			if (i >= 0) {
				if (list.Count == 1) {
					if (objIndex.Count == 1) this.byObject.Remove(subj);
					else objIndex.Remove(pred);
				} else
					list.RemoveAt(i);
			}
		}
	}
	/// <summary>Removes the triple with the specified values from this collection if one is present.</summary>
	/// <returns><see langword="true"/> if the triple was removed; <see langword="false"/> if the triple was not present.</returns>
	public bool Remove(Triple triple) => this.Remove(triple.Subject, triple.Predicate, triple.Object);
	/// <summary>Removes all relations with the specified subject and predicate from this collection.</summary>
	/// <returns>The number of triples that were removed.</returns>
	public int RemoveAll(string subj, string pred) {
		if (!this.bySubject.TryGetValue(subj, out var subjIndex) || !subjIndex.TryGetValue(pred, out var list))
			return 0;
		foreach (var t in list)
			this.RemoveFromObjectIndex(t.Subject, t.Predicate, t.Object);
		if (subjIndex.Count == 1) this.bySubject.Remove(subj);
		else subjIndex.Remove(pred);
		return list.Count;
	}
	/// <summary>Removes the specified subject and all of its relations from this collection.</summary>
	/// <returns>The number of triples that were removed.</returns>
	public int RemoveAll(string subj) {
		if (!this.bySubject.TryGetValue(subj, out var subjIndex))
			return 0;
		var count = 0;
		foreach (var list in subjIndex.Values) {
			foreach (var t in list)
				this.RemoveFromObjectIndex(t.Subject, t.Predicate, t.Object);
			count += list.Count;
		}
		this.bySubject.Remove(subj);
		return count;
	}

	/// <summary>Removes all triples from this collection.</summary>
	public void Clear() {
		this.bySubject.Clear();
		this.byObject.Clear();
		this.Count = 0;
	}

	/// <summary>Yields all triples in this collection that match all specified non-<see langword="null"/> values.</summary>
	public IEnumerable<Triple> Match(string? subj, string? pred, string? obj) {
		if (subj is null) {
			if (pred is null) {
				if (obj is null)
					return this;  // No conditions were specified; return every triple.
				else
					return this.byObject.TryGetValue(obj, out var objIndex)
						? from list in objIndex.Values from t in list select t
						: Enumerable.Empty<Triple>();
			} else {
				if (obj is null)
					return from subjIndex in this.bySubject.Values
						   select subjIndex.GetValueOrDefault(pred, null) into list
						   where list is not null
						   from t in list select t;
				else
					return this.byObject.TryGetValue(obj, out var objIndex) && objIndex.TryGetValue(pred, out var list)
						? list
						: Enumerable.Empty<Triple>();
			}
		} else if (this.bySubject.TryGetValue(subj, out var subjIndex)) {
			if (pred is null) {
				if (obj is null)
					return from list in subjIndex.Values from t in list select t;
				else
					return from list in subjIndex.Values from t in list where this.comparer.Equals(t.Object, obj) select t;
			} else {
				if (obj is null)
					return subjIndex.TryGetValue(pred, out var list)
						? list
						: Enumerable.Empty<Triple>();
				else
					return subjIndex.TryGetValue(pred, out var list)
						&& list.FirstOrDefault(t => this.comparer.Equals(t.Object, obj)) is Triple t
						? new[] { t }
						: Enumerable.Empty<Triple>();
			}
		} else
			return Enumerable.Empty<Triple>();
	}

	bool ICollection<Triple>.IsReadOnly => false;
	void ICollection<Triple>.Add(Triple triple) => this.Add(triple);
	void ICollection<Triple>.CopyTo(Triple[] array, int arrayIndex) {
		foreach (var triple in this) array[arrayIndex++] = triple;
	}
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
