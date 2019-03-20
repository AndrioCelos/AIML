using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Aiml {
	/// <summary>Represents an AIML template-side node.</summary>
	public abstract partial class TemplateNode {
		/// <summary>When overridden, returns the value of this tag.</summary>
		/// <param name="subRequest">The sub-request for which this tag is being evaluated.</param>
		/// <param name="response">The response being built.</param>
		/// <param name="thinking">Indicates whether we are inside a <code>think</code> template element.</param>
		/// <returns>The text that should replace this tag.</returns>
		public abstract string Evaluate(RequestProcess process);
	}

	/// <summary>Represents a template-side tag that can recursively contain other nodes.</summary>
	public abstract class RecursiveTemplateTag : TemplateNode {
		public TemplateElementCollection? Children { get; }

		public RecursiveTemplateTag(TemplateElementCollection? children) {
			this.Children = children;
		}

		public string EvaluateChildren(RequestProcess process) {
			if (this.Children == null) return "";
			return this.Children.Evaluate(process);
		}
	}

	/// <summary>Represents constant text in place of a template-side AIML tag.</summary>
	public sealed class TemplateText : TemplateNode {
		private static readonly Regex whitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

		public string Text { get; private set; }

		/// <summary>Initialises a new <see cref="TemplateText"/> instance with the specified text.</summary>
		public TemplateText(string text) : this(text, true) { }
		/// <summary>Initialises a new <see cref="TemplateText"/> instance with the specified text.</summary>
		/// <param name="reduceWhitespace">If set, consecuetive whitespace will be reduced to a single space, as per HTML.</param>
		public TemplateText(string text, bool reduceWhitespace) {
			// Pandorabots reduces consecutive whitespace in text nodes to a single space character (like HTML).
			// Pandorabots also trims leading and trailing whitespace in text nodes, but we will not do that here.
			if (reduceWhitespace) text = whitespaceRegex.Replace(text, " ");
			this.Text = text;
		}

		/// <summary>Returns this node's text.</summary>
		public override string Evaluate(RequestProcess process) {
			return this.Text;
		}

		public override string ToString() {
			return this.Text;
		}
	}

	/// <summary>Represents a collection of Tags, as contained by a RecursiveTag or property element.</summary>
	public class TemplateElementCollection : IReadOnlyList<TemplateNode>, IList<TemplateNode>, IList, IEnumerable<TemplateNode>, IEnumerable {
		private TemplateNode[] tags;
		private object? syncRoot;
		/// <summary>Indicates whether this TagCollection contains a loop tag.</summary>
		public bool Loop { get; private set; }

		public int Count { get { return this.tags.Length; } }

		public TemplateElementCollection(params TemplateNode[] tags) {
			this.tags = tags;
			foreach (TemplateNode tag in tags) {
				if (tag is TemplateNode.Loop) {
					this.Loop = true;
					break;
				}
			}
		}
		public TemplateElementCollection(TemplateNode[] tags, bool loop) {
			this.tags = tags;
			this.Loop = loop;
		}
		public TemplateElementCollection(IEnumerable<TemplateNode> tags) : this(tags.ToArray()) { }
		public TemplateElementCollection(string text) : this(new TemplateNode[] { new TemplateText(text) }) { }

		public TemplateNode this[int index] => this.tags[index];

		#region Interface implementations
		TemplateNode IList<TemplateNode>.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }
		object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }

		public int IndexOf(TemplateNode tag) => Array.IndexOf(this.tags, tag);
		public bool Contains(TemplateNode tag) => this.IndexOf(tag) != -1;

		public void CopyTo(TemplateNode[] target, int startIndex) {
			for (int i = 0; i < this.tags.Length; ++i)
				target[startIndex + i] = this.tags[i];
		}
		void ICollection.CopyTo(Array target, int startIndex) => this.tags.CopyTo(target, startIndex);

		public IEnumerator<TemplateNode> GetEnumerator() => ((IEnumerable<TemplateNode>) this.tags).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		public bool IsFixedSize => true;
		public bool IsSynchronized => false;
		object ICollection.SyncRoot => this.syncRoot ?? (this.syncRoot = new object());
		public bool IsReadOnly => true;

		bool ICollection<TemplateNode>.Remove(TemplateNode tag) { throw new NotSupportedException(); }
		void ICollection<TemplateNode>.Clear() { throw new NotSupportedException(); }
		void ICollection<TemplateNode>.Add(TemplateNode tag) { throw new NotSupportedException(); }
		void IList<TemplateNode>.RemoveAt(int index) { throw new NotSupportedException(); }
		void IList<TemplateNode>.Insert(int index, TemplateNode tag) { throw new NotSupportedException(); }
		void IList.Remove(object tag) { throw new NotSupportedException(); }
		void IList.RemoveAt(int index) { throw new NotSupportedException(); }
		void IList.Insert(int index, object tag) { throw new NotSupportedException(); }
		void IList.Clear() { throw new NotSupportedException(); }
		int IList.IndexOf(object tag) { if (!(tag is TemplateNode)) return -1; return this.IndexOf((TemplateNode) tag); }
		bool IList.Contains(object tag) { if (!(tag is TemplateNode)) return false; return this.Contains((TemplateNode) tag); }
		int IList.Add(object tag) { throw new NotSupportedException(); }
		#endregion

		/// <summary>Evaluates the contained tags and returns the result.</summary>
		/// <param name="subRequest">The sub-request for which this tag collection is being evaluated.</param>
		/// <param name="response">The response being built.</param>
		/// <param name="thinking">Indicates whether we are inside a <code>think</code> template element.</param>
		/// <returns>The concatenated results of evaluating all the contained tags.</returns>
		public string Evaluate(RequestProcess process) {
			if (this.tags == null || this.tags.Length == 0) return string.Empty;
			StringBuilder builder = new StringBuilder();
			foreach (TemplateNode tag in this.tags)
				builder.Append(tag.Evaluate(process));
			return builder.ToString();
		}

		/// <summary>Returns a new TagCollection containing all nodes contained in a given XML node.</summary>
		/// <param name="node">The XML node whose children should be parsed.</param>
		/// <returns>A new TagCollection containing the results of calling Tag.Parse to construct child nodes from the XML node's children.</returns>
		public static TemplateElementCollection FromXml(XmlNode node, AimlLoader loader) {
			var tagList = new List<TemplateNode>();
			foreach (XmlNode node2 in node.ChildNodes) {
				switch (node2.NodeType) {
					case XmlNodeType.Whitespace:
						tagList.Add(new TemplateText(" "));
						break;
					case XmlNodeType.Text:
						tagList.Add(new TemplateText(node2.InnerText));
						break;
					case XmlNodeType.SignificantWhitespace:
						tagList.Add(new TemplateText(node2.InnerText, false));
						break;
					case XmlNodeType.Element:
						tagList.Add(loader.ParseElement(node2));
						break;
				}
			}
			return new TemplateElementCollection(tagList.ToArray());
		}

		public override string ToString() {
			StringBuilder builder = new StringBuilder();
			foreach (TemplateNode tag in this)
				builder.Append(tag.ToString());
			return builder.ToString();
		}
	}
}
