using System;
using System.Collections.Generic;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Sends the content to an external service and returns the response from the service.
		/// </summary>
		/// <remarks>
		///		<para>This element has the following attributes:</para>
		///		<list type="table">
		///			<item>
		///				<term><c>service</c></term>
		///				<description>the name of the external service to use, from <see cref="Aiml.Bot.SraixServices"/>.</description>
		///			</item>
		///			<item>
		///				<term><c>default</c></term>
		///				<description>returned if the service call fails.
		///					If omitted, the <c>SRAIXFAILED</c> category is queried and two predicates are set:
		///					<list type="bullet">
		///						<item><term>SraixException</term><description>the exception type name.</description></item>
		///						<item><term>SraixExceptionMessage</term><description>the exception message.</description></item>
		///					</list>
		///				</description>
		///			</item>
		///		</list>
		///     <para>This element is defined by the AIML 2.0 specification. This implementation is non-standard.</para>
		/// </remarks>
		/// <seealso cref="Srai"/>
		public sealed class SraiX : RecursiveTemplateTag {
			public TemplateElementCollection ServiceName { get; }
			public XmlAttributeCollection Attributes { get; }
			public TemplateElementCollection DefaultReply { get; }

			public SraiX(TemplateElementCollection serviceName, XmlAttributeCollection attributes, TemplateElementCollection defaultReply, TemplateElementCollection children) : base(children) {
				this.Attributes = attributes;
				this.ServiceName = serviceName;
				this.DefaultReply = defaultReply;
			}

			public override string Evaluate(RequestProcess process) {
				var serviceName = this.ServiceName.Evaluate(process);
				try {
					if (process.Bot.SraixServices.TryGetValue(serviceName, out var service)) {
						var text = this.Children?.Evaluate(process) ?? "";
						process.Log(LogLevel.Diagnostic, "In element <sraix>: querying service '" + serviceName + "' to process text '" + text + "'.");
						text = service.Process(text, this.Attributes, process);
						process.Log(LogLevel.Diagnostic, "In element <sraix>: the request returned '" + text + "'.");
						return text;
					} else {
						process.User.Predicates["SraixException"] = nameof(KeyNotFoundException);
						process.User.Predicates["SraixExceptionMessage"] = "No service named '" + serviceName + "' is known.";
						process.Log(LogLevel.Warning, "In element <sraix>: no service named '" + serviceName + "' is known.");
						return (this.DefaultReply ?? new TemplateElementCollection(new Srai(new TemplateElementCollection("SRAIXFAILED")))).Evaluate(process);
					}
				} catch (Exception ex) {
					process.User.Predicates["SraixException"] = ex.GetType().Name;
					process.User.Predicates["SraixExceptionMessage"] = ex.Message;
					process.Log(LogLevel.Warning, "In element <sraix>: service '" + serviceName + "' threw " + ex.GetType().Name + ":\n" + ex.ToString());
					return (this.DefaultReply ?? new TemplateElementCollection(new Srai(new TemplateElementCollection("SRAIXFAILED")))).Evaluate(process);
				}
			}

			public static SraiX FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection? serviceName = null;
				TemplateElementCollection? defaultReply = null;
				var children = new List<TemplateNode>();

				attribute = node.Attributes["service"];
				if (attribute != null) serviceName = new TemplateElementCollection(attribute.Value);
				attribute = node.Attributes["default"];
				if (attribute != null) defaultReply = new TemplateElementCollection(attribute.Value);

				// Search for properties in elements.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Whitespace) {
						children.Add(new TemplateText(" "));
					} else if (node2.NodeType == XmlNodeType.Text || node2.NodeType == XmlNodeType.SignificantWhitespace) {
						children.Add(new TemplateText(node2.InnerText));
					} else if (node2.NodeType == XmlNodeType.Element) {
						if (node2.Name.Equals("default", StringComparison.InvariantCultureIgnoreCase))
							defaultReply = TemplateElementCollection.FromXml(node2, loader);
						else if (node2.Name.Equals("service", StringComparison.InvariantCultureIgnoreCase))
							serviceName = TemplateElementCollection.FromXml(node2, loader);
						else
							children.Add(loader.ParseElement(node2));
					}
				}

				return new SraiX(serviceName, node.Attributes, defaultReply, new TemplateElementCollection(children.ToArray()));
			}
		}
	}
}
