namespace Aiml;
public class TripleCollection {
	private readonly Dictionary<int, Triple> triples = new();  // This is a hashtable and not an array, to support easy removal of arbitrary triples.
	private int lastIndex = -1;
	private readonly Dictionary<string, HashSet<int>> triplesBySubject = new(StringComparer.CurrentCultureIgnoreCase);
	private readonly Dictionary<string, HashSet<int>> triplesByPredicate = new(StringComparer.CurrentCultureIgnoreCase);
	private readonly Dictionary<string, HashSet<int>> triplesByObject = new(StringComparer.CurrentCultureIgnoreCase);

	public int Count => this.triples.Count;

	public Triple this[int index] => this.triples[index];

	public int Add(string tripleSubject, string triplePredicate, string tripleObject)
		=> this.Add(new Triple(tripleSubject, triplePredicate, tripleObject));
	public int Add(Triple triple) {
		var index = Interlocked.Increment(ref this.lastIndex);
		this.triples.Add(index, triple);

		if (!this.triplesBySubject.TryGetValue(triple.Subject, out var set)) { this.triplesBySubject[triple.Subject] = set = new HashSet<int>(); }
		set.Add(index);
		if (!this.triplesByPredicate.TryGetValue(triple.Predicate, out set)) { this.triplesByPredicate[triple.Predicate] = set = new HashSet<int>(); }
		set.Add(index);
		if (!this.triplesByObject.TryGetValue(triple.Object, out set)) { this.triplesByObject[triple.Object] = set = new HashSet<int>(); }
		set.Add(index);

		return index;
	}

	public bool Remove(int index) {
		if (!this.triples.TryGetValue(index, out var triple)) return false;
		this.triples.Remove(index);

		HashSet<int> set;

		set = this.triplesBySubject[triple.Subject];
		if (set.Count == 1) this.triplesBySubject.Remove(triple.Subject);
		else set.Remove(index);

		set = this.triplesByPredicate[triple.Predicate];
		if (set.Count == 1) this.triplesByPredicate.Remove(triple.Predicate);
		else set.Remove(index);

		set = this.triplesByObject[triple.Object];
		if (set.Count == 1) this.triplesByObject.Remove(triple.Object);
		else set.Remove(index);

		return true;
	}

	public HashSet<int> Match(Clause clause) {
		HashSet<int> subjectTriples, predicateTriples, objectTriples;

		// A variable means that the named variable will take the values matching a clause, and is not part of the assertion.
		if (clause.subj.StartsWith("?"))
			subjectTriples = new HashSet<int>(this.triples.Keys);
		else if (!this.triplesBySubject.TryGetValue(clause.subj, out subjectTriples))
			subjectTriples = new HashSet<int>();

		if (clause.pred.StartsWith("?"))
			predicateTriples = new HashSet<int>(this.triples.Keys);
		else if (!this.triplesByPredicate.TryGetValue(clause.pred, out predicateTriples))
			predicateTriples = new HashSet<int>();

		if (clause.obj.StartsWith("?"))
			objectTriples = new HashSet<int>(this.triples.Keys);
		else if (!this.triplesByObject.TryGetValue(clause.obj, out objectTriples))
			objectTriples = new HashSet<int>();

		var result = new HashSet<int>(subjectTriples);  // We must clone it to avoid modifying the index.
		result.IntersectWith(predicateTriples);
		result.IntersectWith(objectTriples);
		return result;
	}
}
