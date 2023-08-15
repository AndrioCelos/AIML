namespace Aiml.Tags;
/// <summary>Sets the value of a predicate or a local variable to the content, and returns the content.</summary>
/// <remarks>
///		<para>This element has two forms:</para>
///		<list type="bullet">
///			<item>
///				<term><c>&lt;set name='predicate'&gt;value&lt;/set&gt;</c></term>
///				<description>Sets a predicate for the current user.</description>
///			</item>
///			<item>
///				<term><c>&lt;set var='variable'&gt;value&lt;/set&gt;</c></term>
///				<description>Sets a local variable for the containing category.</description>
///			</item>
///		</list>
///		<para>This element is defined by the AIML 1.1 specification. Local variables are defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="AddTriple"/><seealso cref="Get"/>
public sealed class Set(TemplateElementCollection key, bool local, TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public TemplateElementCollection Key { get; private set; } = key;
	public bool LocalVar { get; private set; } = local;

	[AimlLoaderContructor]
	public Set(TemplateElementCollection? name, TemplateElementCollection? var, TemplateElementCollection children)
		: this(var ?? name ?? throw new ArgumentException("<set> element must have a name or var attribute", nameof(name)), var is not null, children) {
		if (name is not null && var is not null)
			throw new ArgumentException("<set> element cannot have both name and var attributes.", nameof(var));
	}

	public override string Evaluate(RequestProcess process) {
		var key = this.Key.Evaluate(process);
		var value = this.EvaluateChildren(process).Trim();

		var dictionary = this.LocalVar ? process.Variables : process.User.Predicates;
		if (process.Bot.Config.UnbindPredicatesWithDefaultValue &&
			value == (this.LocalVar ? process.Bot.Config.DefaultPredicate : process.Bot.Config.GetDefaultPredicate(key))) {
			dictionary.Remove(key);
			process.Log(LogLevel.Diagnostic, "In element <set>: Unbound " + (this.LocalVar ? "local variable" : "predicate") + " '" + key + "' with default value '" + value + "'.");
		} else {
			dictionary[key] = value;
			process.Log(LogLevel.Diagnostic, "In element <set>: Set " + (this.LocalVar ? "local variable" : "predicate") + " '" + key + "' to '" + value + "'.");
		}

		return value;
	}
}
