namespace Aiml;
public class Template(TemplateElementCollection content, string? uri, int lineNumber) {
	public TemplateElementCollection Content { get; } = content;
	public string? Uri { get; set; } = uri;
	public int LineNumber { get; set; } = lineNumber;

	public Template(TemplateElementCollection content) : this(content, null, 0) { }

	public IList<Tags.Test> GetTests() {
		var tests = new List<Tags.Test>();
		var collections = new Queue<TemplateElementCollection>();
		collections.Enqueue(this.Content);
		while (collections.Count > 0) {
			var collection = collections.Dequeue();
			foreach (var child in collection) {
				if (child is Tags.Test test)
					tests.Add(test);
				else if (child is RecursiveTemplateTag tag && tag.Children != null)
					collections.Enqueue(tag.Children);
			}
		}
		return tests;
	}
}
