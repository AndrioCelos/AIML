namespace Aiml;
public class TripleCollection {
	private readonly Dictionary<int, Triple> triples = new();  // This is a hashtable and not an array, to support easy removal of arbitrary triples.
	private int lastIndex = -1;
	private readonly Dictionary<string, HashSet<int>> triplesBySubject = new(StringComparer.CurrentCultureIgnoreCase);
	private readonly Dictionary<string, HashSet<int>> triplesByPredicate = new(StringComparer.CurrentCultureIgnoreCase);
	private readonly Dictionary<string, HashSet<int>> triplesByObject = new(StringComparer.CurrentCultureIgnoreCase);

	public int Count => this.triples.Count;

	public Triple this[int index] => this.triples[index];

	public bool Add(string subj, string pred, string obj, out int key) => this.Add(new Triple(subj, pred, obj), out key);
	public bool Add(Triple triple, out int key) {
		var match = this.Match(triple.Subject, triple.Predicate, triple.Object);
		if (match.Count > 0) {
			key = match.First();
			return false;
		}

		key = Interlocked.Increment(ref this.lastIndex);
		this.triples.Add(key, triple);

		if (!this.triplesBySubject.TryGetValue(triple.Subject, out var set)) this.triplesBySubject[triple.Subject] = set = new HashSet<int>();
		set.Add(key);
		if (!this.triplesByPredicate.TryGetValue(triple.Predicate, out set)) this.triplesByPredicate[triple.Predicate] = set = new HashSet<int>();
		set.Add(key);
		if (!this.triplesByObject.TryGetValue(triple.Object, out set)) this.triplesByObject[triple.Object] = set = new HashSet<int>();
		set.Add(key);

		return true;
	}

	public bool Remove(int key) {
		if (!this.triples.TryGetValue(key, out var triple)) return false;
		this.triples.Remove(key);

		var set = this.triplesBySubject[triple.Subject];
		if (set.Count == 1) this.triplesBySubject.Remove(triple.Subject);
		else set.Remove(key);

		set = this.triplesByPredicate[triple.Predicate];
		if (set.Count == 1) this.triplesByPredicate.Remove(triple.Predicate);
		else set.Remove(key);

		set = this.triplesByObject[triple.Object];
		if (set.Count == 1) this.triplesByObject.Remove(triple.Object);
		else set.Remove(key);

		return true;
	}

	public HashSet<int> Match(string subj, string pred, string obj) {
		HashSet<int>? triples = null;

		// A variable means that the named variable will take the values matching a clause, and is not part of the assertion.
		if (!subj.StartsWith("?")) {
			if (this.triplesBySubject.TryGetValue(subj, out var triples2))
				triples = new(triples2);  // We must clone it to avoid modifying the index.
			else
				return new();
		}
		if (!pred.StartsWith("?")) {
			if (this.triplesByPredicate.TryGetValue(subj, out var triples2)) {
				if (triples is not null) triples.IntersectWith(triples2);
				else triples = new(triples2);
			} else
				return new();
		}
		if (!obj.StartsWith("?")) {
			if (this.triplesByObject.TryGetValue(subj, out var triples2)) {
				if (triples is not null) triples.IntersectWith(triples2);
				else triples = new(triples2);
			} else
				return new();
		}

		return triples ?? new(this.triples.Keys);
	}
}
