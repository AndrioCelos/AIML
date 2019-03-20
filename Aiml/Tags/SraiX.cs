using System;
using System.Collections.Generic;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Sends the content to an external service and returns the response from the service.
		/// </summary>
		/// <remarks>
		///     The 'service' attribute specifies the name of the external service to use.
		///     The content is evaluated and then sent to the external service.
		///     If no service was found or the service throws an exception, a warning will be raised and the default reply will be returned.
		///     The default reply is the text in the 'default' attribute, or if this attribute does not exist, the SRAIXFAILED category is queried.
		///     This element is defined by the AIML 2.0 specification.
		/// </remarks>
		public sealed class SraiX : RecursiveTemplateTag {
			public string ServiceName { get; }
			public XmlAttributeCollection Attributes { get; }
			public TemplateElementCollection DefaultReply { get; }

			public SraiX(string serviceName, XmlAttributeCollection attributes, TemplateElementCollection defaultReply, TemplateElementCollection children) : base(children) {
				this.Attributes = attributes;
				this.ServiceName = serviceName;
				this.DefaultReply = defaultReply;
			}

			public override string Evaluate(RequestProcess process) {
				try {
					if (process.Bot.SraixServices.TryGetValue(this.ServiceName, out var service)) {
						var text = this.Children?.Evaluate(process) ?? "";
						process.Log(LogLevel.Diagnostic, "In element <sraix>: querying service '" + this.ServiceName + "' to process text '" + text + "'.");
						text = service.Process(text, this.Attributes, process);
						process.Log(LogLevel.Diagnostic, "In element <sraix>: the request returned '" + text + "'.");
						return text;
					} else {
						process.User.Predicates["SraixException"] = nameof(KeyNotFoundException);
						process.User.Predicates["SraixExceptionMessage"] = "No service named '" + this.ServiceName + "' is known.";
						process.Log(LogLevel.Warning, "In element <sraix>: no service named '" + this.ServiceName + "' is known.");
						return (this.DefaultReply ?? new TemplateElementCollection(new Srai(new TemplateElementCollection("SRAIXFAILED")))).Evaluate(process);
					}
				} catch (Exception ex) {
					process.User.Predicates["SraixException"] = ex.GetType().Name;
					process.User.Predicates["SraixExceptionMessage"] = ex.Message;
					process.Log(LogLevel.Warning, "In element <sraix>: service '" + this.ServiceName + "' threw " + ex.GetType().Name + ":\n" + ex.ToString());
					return (this.DefaultReply ?? new TemplateElementCollection(new Srai(new TemplateElementCollection("SRAIXFAILED")))).Evaluate(process);
				}
			}

			public static SraiX FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				string? serviceName = null;
				TemplateElementCollection? defaultReply = null;

				attribute = node.Attributes["service"];
				if (attribute != null) serviceName = attribute.Value;
				attribute = node.Attributes["default"];
				if (attribute != null) defaultReply = new TemplateElementCollection(attribute.Value);

				// Search for properties in elements.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Element) {
						if (node2.Name.Equals("default", StringComparison.InvariantCultureIgnoreCase))
							defaultReply = TemplateElementCollection.FromXml(node2, loader);
					}
				}

				return new SraiX(serviceName, node.Attributes, defaultReply, TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
