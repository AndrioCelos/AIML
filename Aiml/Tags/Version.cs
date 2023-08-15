using System.Reflection;

namespace Aiml.Tags;
/// <summary>Returns the version of the AIML interpreter.</summary>
/// <remarks>
///		<para>This element has no content.</para>
///		<para>This element is part of an extension to AIML.</para>
/// </remarks>
public sealed class Version : TemplateNode {
	public override string Evaluate(RequestProcess process) => Assembly.GetExecutingAssembly().GetName().Version.ToString();
}
