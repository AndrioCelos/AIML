using System.Xml;

namespace Aiml.Tags;
/// <summary>Sends the content to an external service and returns the response from the service.</summary>
/// <remarks>
///		<para>This element has the following attributes:</para>
///		<list type="table">
///			<item>
///				<term><c>service</c></term>
///				<description>the name of the external service to use, from <see cref="Bot.SraixServices"/>.</description>
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
///		<para>This element is defined by the AIML 2.0 specification. This implementation is non-standard.</para>
/// </remarks>
/// <seealso cref="Srai"/>
public sealed class SraiX(TemplateElementCollection service, TemplateElementCollection? @default, XmlAttributeCollection attributes, TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public TemplateElementCollection ServiceName { get; } = service;
	public XmlAttributeCollection Attributes { get; } = attributes;
	public TemplateElementCollection? DefaultReply { get; } = @default;

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
}
