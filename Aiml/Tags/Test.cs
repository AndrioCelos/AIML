using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>Represents an AIML unit test.</summary>
		/// <remarks>
		///		A unit test consists of processing the contents as a chat message, and checking the response against the specified expected response.
		///		The test is case-sensitive, but leading and trailing whitespace is ignored.
		///		The tag returns the response to the test message, similar to the <c>srai</c> tag.
		///     This tag supports the following properties:
		///			name
		///				Specifies a name used to refer to the test. This property must be specified in an attribute.
		///			expected
		///				Specifies the expected response message from the test.
		///     This tag is not part of the AIML specification.
		/// </remarks>
		public sealed class Test : RecursiveTemplateTag {
			public string Name { get; }
			public TemplateElementCollection ExpectedResponse { get; }

			public Test(string name, TemplateElementCollection expectedResponse, TemplateElementCollection children) : base(children) {
				this.Name = name;
				this.ExpectedResponse = expectedResponse;
			}

			public override string Evaluate(RequestProcess process) {
				process.Log(LogLevel.Info, $"In element <test>: running test {this.Name}");
				string text = this.Children?.Evaluate(process) ?? "";
				process.Log(LogLevel.Diagnostic, "In element <test>: processing text '" + text + "'.");
				var newRequest = new Aiml.Request(text, process.User, process.Bot);
				text = process.Bot.ProcessRequest(newRequest, false, false, process.RecursionDepth + 1, out var duration).ToString().Trim();
				process.Log(LogLevel.Diagnostic, "In element <test>: the request returned '" + text + "'.");

				if (process.testResults != null) {
					var expectedResponse = this.ExpectedResponse.Evaluate(process).Trim();
					TestResult result;
					if (process.Bot.Config.CaseSensitiveStringComparer.Equals(text, expectedResponse))
						result = TestResult.Pass(duration);
					else
						result = TestResult.Failure($"Expected response: {expectedResponse}\nActual response: {text}", duration);
					process.testResults[this.Name] = result;
				} else
					process.Log(LogLevel.Warning, "In element <test>: Tests are not being used.");

				return text;
			}

			public static Test FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection? expected = null;
				List<TemplateNode> children = new List<TemplateNode>();

				attribute = node.Attributes["name"];
				if (attribute == null) throw new AimlException("<test> tag must have a 'name' attribute.");
				var name = attribute.Value;

				attribute = node.Attributes["expected"];
				if (attribute != null) expected = new TemplateElementCollection(attribute.Value);

				// Search for properties in elements.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Whitespace) {
						children.Add(new TemplateText(" "));
					} else if (node2.NodeType == XmlNodeType.Text || node2.NodeType == XmlNodeType.SignificantWhitespace) {
						children.Add(new TemplateText(node2.InnerText));
					} else if (node2.NodeType == XmlNodeType.Element) {
							if (node2.Name.Equals("name", StringComparison.InvariantCultureIgnoreCase))
								throw new AimlException("<test> name may not be specified in a subtag.");
							else if (node2.Name.Equals("expected", StringComparison.InvariantCultureIgnoreCase))
								expected = TemplateElementCollection.FromXml(node2, loader);
							else
								children.Add(loader.ParseElement(node2));
					}
				}

				if (expected == null) throw new AimlException("<test> tag must have an 'expected' property.");

				return new Test(name, expected, new TemplateElementCollection(children.ToArray()));
			}
		}
	}
}
