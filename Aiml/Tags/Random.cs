using System.Text;
using System.Xml;

namespace Aiml;
public partial class TemplateNode {
	/// <summary>Randomly selects and returns one of its child elements.</summary>
	/// <remarks>
	///		<para>This element can only contain <c>li</c> elements as direct children.</para>
	///		<para>This element is defined by the AIML 1.1 specification.</para>
	/// </remarks>
	/// <seealso cref="Condition"/>
	public sealed class Random : TemplateNode {
		private readonly Li[] items;

		public Random(Li[] items) {
			if (items.Length == 0) throw new AimlException("Random element must contain at least one item.");
			this.items = items;
		}

		public Li Pick(RequestProcess process) => this.items[process.Bot.Random.Next(this.items.Length)];

		public override string Evaluate(RequestProcess process) {
			var builder = new StringBuilder();
			Li item;

			do {
				item = this.Pick(process);
				if (builder.Length != 0) builder.Append(' ');
				builder.Append(item.Evaluate(process));
			} while (item.Children.Loop);

			return builder.ToString();
		}

		public static Random FromXml(XmlNode node, AimlLoader loader) {
			var items = new List<Li>();

			// Search for items.
			foreach (XmlNode node2 in node.ChildNodes) {
				if (node2.NodeType == XmlNodeType.Element) {
					if (node2.Name.Equals("li", StringComparison.InvariantCultureIgnoreCase))
						items.Add(Li.Parse(node2, loader));
				}
			}

			if (items.Count == 0)
				return new Random(items.ToArray());

			foreach (var item in items) {
				if (!item.Children.Loop)
					return new Random(items.ToArray());
			}
			throw new AimlException("Infinite loop: every <li> has a loop.");
		}

		public class Li(TemplateElementCollection children) : RecursiveTemplateTag(children) {
			public override string Evaluate(RequestProcess process) => this.Children?.Evaluate(process) ?? "";
			public static Li Parse(XmlNode node, AimlLoader loader) => new(TemplateElementCollection.FromXml(node, loader));
		}
	}
}
