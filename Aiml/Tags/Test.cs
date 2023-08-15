using System.Text.RegularExpressions;

namespace Aiml.Tags;
/// <summary>Runs an AIML unit test and returns the element's content.</summary>
/// <remarks>
///		<para>A unit test consists of processing the content as chat input to the bot, and checking the response against the specified expected response.
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
public sealed class Test(string name, TemplateElementCollection expectedResponse, bool regex, TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public string Name { get; } = name;
	public bool UseRegex { get; } = regex;
	public TemplateElementCollection ExpectedResponse { get; } = expectedResponse;

	[AimlLoaderContructor]
	public Test(TemplateElementCollection name, TemplateElementCollection? expected, TemplateElementCollection? regex, TemplateElementCollection children)
		: this(name.Single() is TemplateText text ? text.Text : throw new ArgumentException("<test> attribute name must be a constant."),
			  expected ?? regex ?? throw new ArgumentException("<test> element must have an expected or regex attribute."), regex is not null, children) {
		if (expected is not null && regex is not null)
			throw new ArgumentException("<test> element cannot have both expected and regex attributes.", nameof(regex));
	}

	public override string Evaluate(RequestProcess process) {
		process.Log(LogLevel.Info, $"In element <test>: running test {this.Name}");
		var text = this.EvaluateChildren(process);
		process.Log(LogLevel.Diagnostic, "In element <test>: processing text '" + text + "'.");
		var newRequest = new Aiml.Request(text, process.User, process.Bot);
		text = process.Bot.ProcessRequest(newRequest, false, false, process.RecursionDepth + 1, out var duration).ToString().Trim();
		process.Log(LogLevel.Diagnostic, "In element <test>: the request returned '" + text + "'.");

		if (process.testResults != null) {
			var expectedResponse = this.ExpectedResponse.Evaluate(process).Trim();
			var result = this.UseRegex
				? Regex.IsMatch(text.Trim(), "^" + Regex.Replace(expectedResponse, @"\s+", @"\s+") + "$")
					? TestResult.Pass(duration)
					: TestResult.Failure($"Expected regex: {expectedResponse}\nActual response: {text}", duration)
				: process.Bot.Config.CaseSensitiveStringComparer.Equals(text, expectedResponse)
					? TestResult.Pass(duration)
					: TestResult.Failure($"Expected response: {expectedResponse}\nActual response: {text}", duration);
			process.testResults[this.Name] = result;
		} else
			process.Log(LogLevel.Warning, "In element <test>: Tests are not being used.");

		return text;
	}
}
