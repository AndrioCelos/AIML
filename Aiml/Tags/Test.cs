using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///		Runs an AIML unit test and returns the element's content.
		///	</summary>
		/// <remarks>
		///		<para>A unit test consists of processing the content as chat input to the but, and checking the response against the specified expected response.
		///			The test is case-sensitive, but leading and trailing whitespace is ignored.</para>
		///		<para>This element has the following attributes:</para>
		///		<list type="table">
		///			<item>
		///				<term><c>name</c></term>
		///				<description>the name of the test. May not be an XML subtag.</description>
		///			</item>
		///			<item>
		///				<term><c>expected</c></term>
		///				<description>the expected response message from the test.</description>
		///			</item>
		///			<item>
		///				<term><c>regex</c></term>
		///				<description>a regular expression that must match the response.</description>
		///			</item>
		///		</list>
		///		<para>This element is part of an extension to AIML.</para>
		/// </remarks>
		public sealed class Test : RecursiveTemplateTag {
			public string Name { get; }
			public bool UseRegex { get; }
			public TemplateElementCollection ExpectedResponse { get; }

			public Test(string name, TemplateElementCollection expectedResponse, bool regex, TemplateElementCollection children) : base(children) {
				this.Name = name;
				this.UseRegex = regex;
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
					if (this.UseRegex) {
						if (Regex.IsMatch(text.Trim(), "^" + Regex.Replace(expectedResponse, @"\s+", @"\s+") + "$"))
							result = TestResult.Pass(duration);
						else
							result = TestResult.Failure($"Expected regex: {expectedResponse}\nActual response: {text}", duration);
					} else {
						if (process.Bot.Config.CaseSensitiveStringComparer.Equals(text, expectedResponse))
							result = TestResult.Pass(duration);
						else
							result = TestResult.Failure($"Expected response: {expectedResponse}\nActual response: {text}", duration);
					}
					process.testResults[this.Name] = result;
				} else
					process.Log(LogLevel.Warning, "In element <test>: Tests are not being used.");

				return text;
			}

			public static Test FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection? expected = null;
				bool regex = false;
				List<TemplateNode> children = new List<TemplateNode>();

				attribute = node.Attributes["name"];
				if (attribute == null) throw new AimlException("<test> tag must have a 'name' attribute.");
				var name = attribute.Value;

				attribute = node.Attributes["expected"];
				if (attribute != null) expected = new TemplateElementCollection(attribute.Value);
				else {
					attribute = node.Attributes["regex"];
					if (attribute != null) {
						regex = true;
						expected = new TemplateElementCollection(attribute.Value);
					}
				}

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
						else if (node2.Name.Equals("regex", StringComparison.InvariantCultureIgnoreCase)) {
							regex = true;
							expected = TemplateElementCollection.FromXml(node2, loader);
						} else
							children.Add(loader.ParseElement(node2));
					}
				}

				if (expected == null) throw new AimlException("<test> tag must have an 'expected' or 'regex' property.");

				return new Test(name, expected, regex, new TemplateElementCollection(children.ToArray()));
			}
		}
	}
}
