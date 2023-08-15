using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Aiml;
/// <summary>Represents an AIML template-side node.</summary>
public abstract class TemplateNode {
	/// <summary>When overridden, returns the evaluated content of this node.</summary>
	public abstract string Evaluate(RequestProcess process);
}

/// <summary>Represents a template-side tag that can recursively contain other nodes.</summary>
public abstract class RecursiveTemplateTag(TemplateElementCollection children) : TemplateNode {
	public TemplateElementCollection Children { get; } = children;

	public string EvaluateChildren(RequestProcess process) => this.Children?.Evaluate(process) ?? "";
	public string EvaluateChildrenOrStar(RequestProcess process)
		=> this.Children is not null && !this.Children.IsEmpty ? this.Children.Evaluate(process)
			: process.star.Count > 0 ? process.star[0] : process.Bot.Config.DefaultWildcard;

	public override string ToString() => $"<{this.GetType().Name.ToLowerInvariant()}>{this.Children}</{this.GetType().Name.ToLowerInvariant()}>";
}

/// <summary>Represents constant text in place of a template-side AIML tag.</summary>
public sealed class TemplateText : TemplateNode {
	private static readonly Regex whitespaceRegex = new(@"\s+", RegexOptions.Compiled);

	internal static TemplateText Space { get; } = new(" ");

	public string Text { get; private set; }

	/// <summary>Initialises a new <see cref="TemplateText"/> instance with the specified text.</summary>
	public TemplateText(string text) : this(text, true) { }
	/// <summary>Initialises a new <see cref="TemplateText"/> instance with the specified text.</summary>
	/// <param name="reduceWhitespace">If set, consecutive whitespace will be reduced to a single space, as per HTML.</param>
	public TemplateText(string text, bool reduceWhitespace) {
		// Pandorabots reduces consecutive whitespace in text nodes to a single space character (like HTML).
		if (reduceWhitespace) text = whitespaceRegex.Replace(text, " ");
		this.Text = text;
	}

	/// <summary>Returns this node's text.</summary>
	public override string Evaluate(RequestProcess process) => this.Text;

	public override string ToString() => this.Text;
}

/// <summary>Represents a collection of <see cref="TemplateNode"/> instances, as contained by a <see cref="RecursiveTemplateTag"/> or attribute subtag.</summary>
public class TemplateElementCollection(params TemplateNode[] tags) : IReadOnlyList<TemplateNode>, IList<TemplateNode>, IList {
	private readonly TemplateNode[] tags = tags;

	public static TemplateElementCollection Empty { get; } = new();

	/// <summary>Indicates whether this <see cref="TemplateElementCollection"/> contains a <see cref="Tags.Loop"/> tag.</summary>
	public bool Loop { get; } = tags.OfType<Tags.Loop>().Any();
	public int Count => this.tags.Length;
	public bool IsEmpty => this.tags.All(t => t is TemplateText text && string.IsNullOrEmpty(text.Text));
	public bool IsWhitespace => this.tags.All(t => t is TemplateText text && string.IsNullOrWhiteSpace(text.Text));

	public TemplateElementCollection(IEnumerable<TemplateNode> tags) : this(tags.ToArray()) { }
	public TemplateElementCollection(string text) : this(new TemplateNode[] { new TemplateText(text) }) { }

	public TemplateNode this[int index] => this.tags[index];

	/// <summary>Evaluates the contained tags and returns the result.</summary>
	public string Evaluate(RequestProcess process) {
		if (this.tags == null || this.tags.Length == 0) return string.Empty;
		var builder = new StringBuilder();
		foreach (var tag in this.tags) {
			var output = tag.Evaluate(process);

			// Condense consecutive spaces.
			if (builder.Length > 0 && char.IsWhiteSpace(builder[^1]))
				output = output.TrimStart();

			builder.Append(output);
		}
		return builder.ToString();
	}

	/// <summary>Returns a new TagCollection containing all nodes contained in a given XML node.</summary>
	/// <param name="el">The XML node whose children should be parsed.</param>
	/// <returns>A new TagCollection containing the results of calling Tag.Parse to construct child nodes from the XML node's children.</returns>
	public static TemplateElementCollection FromXml(XmlElement el, AimlLoader loader) {
		var tagList = new List<TemplateNode>();
		foreach (XmlNode node2 in el.ChildNodes) {
			switch (node2.NodeType) {
				case XmlNodeType.Whitespace:
					tagList.Add(TemplateText.Space);
					break;
				case XmlNodeType.Text:
					tagList.Add(new TemplateText(node2.InnerText));
					break;
				case XmlNodeType.CDATA:
				case XmlNodeType.SignificantWhitespace:
					tagList.Add(new TemplateText(node2.InnerText, false));
					break;
				default:
					if (node2 is XmlElement childElement)
						tagList.Add(loader.ParseElement(childElement));
					break;
			}
		}
		return new TemplateElementCollection(tagList.ToArray());
	}

	public override string ToString() => string.Join(null, this);

	#region Interface implementations

	bool IList.IsFixedSize => true;
	bool IList.IsReadOnly => true;
	bool ICollection<TemplateNode>.IsReadOnly => true;
	bool ICollection.IsSynchronized => false;
	object ICollection.SyncRoot => this;

	TemplateNode IList<TemplateNode>.this[int index] { get => this[index]; set => throw new NotSupportedException(); }
	object? IList.this[int index] { get => this[index]; set => throw new NotSupportedException(); }

	public int IndexOf(TemplateNode tag) => Array.IndexOf(this.tags, tag);
	public bool Contains(TemplateNode tag) => this.IndexOf(tag) >= 0;

	public void CopyTo(TemplateNode[] target, int startIndex) {
		for (var i = 0; i < this.tags.Length; ++i)
			target[startIndex + i] = this.tags[i];
	}
	void ICollection.CopyTo(Array target, int startIndex) => this.tags.CopyTo(target, startIndex);

	public IEnumerator<TemplateNode> GetEnumerator() => ((IEnumerable<TemplateNode>) this.tags).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

	bool ICollection<TemplateNode>.Remove(TemplateNode tag) => throw new NotSupportedException();
	void ICollection<TemplateNode>.Clear() => throw new NotSupportedException();
	void ICollection<TemplateNode>.Add(TemplateNode tag) => throw new NotSupportedException();
	void IList<TemplateNode>.RemoveAt(int index) => throw new NotSupportedException();
	void IList<TemplateNode>.Insert(int index, TemplateNode tag) => throw new NotSupportedException();
	void IList.Remove(object? tag) => throw new NotSupportedException();
	void IList.RemoveAt(int index) => throw new NotSupportedException();
	void IList.Insert(int index, object? tag) => throw new NotSupportedException();
	void IList.Clear() => throw new NotSupportedException();
	int IList.IndexOf(object? tag) => tag is TemplateNode node ? this.IndexOf(node) : -1;
	bool IList.Contains(object? tag) => tag is TemplateNode node && this.Contains(node);
	int IList.Add(object? tag) => throw new NotSupportedException();

	#endregion
}
