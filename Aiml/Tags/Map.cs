namespace Aiml.Tags;
/// <summary>Looks up the content in the specified map and returns the string mapped to.</summary>
/// <remarks>
///		<para>This element has the following attribute:</para>
///		<list type="table">
///			<item>
///				<term><c>name</c></term>
///				<description>the name of the map to search.</description>
///			</item>
///		</list>
///		<para>The search is case-insensitive and ignores leading, trailing and repeated whitespace. If the content is not found in the map, <c>DefaultMap</c> is returned.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="Srai"/>
public sealed class Map(TemplateElementCollection name, TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public TemplateElementCollection Name { get; set; } = name;

	public override string Evaluate(RequestProcess process)
		=> process.Bot.Maps.TryGetValue(this.Name.Evaluate(process), out var map)
			? map[this.EvaluateChildren(process)] ?? process.Bot.Config.DefaultMap
			: process.Bot.Config.DefaultMap;
}
