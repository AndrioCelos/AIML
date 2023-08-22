namespace Aiml.Tags;
/// <summary>Returns the value of a predicate for the current user, local variable or tuple variable.</summary>
/// <remarks>
///		<para>This element has three forms:</para>
///		<list type="bullet">
///			<item>
///				<term><c>&lt;get name='predicate'/&gt;</c></term>
///				<description>Returns the value of the specified predicate for the current user, or <c>DefaultPredicate</c> if it is not bound.</description>
///			</item>
///			<item>
///				<term><c>&lt;get var='variable'/&gt;</c></term>
///				<description>Returns the value of a local variable for the containing category, or <c>DefaultPredicate</c> if it is not bound.</description>
///			</item>
///			<item>
///				<term><c><![CDATA[<get var='?variable'><tuple>tuple</tuple></get>]]></c></term>
///				<description>Returns the value of a tuple variable binding set by a <see cref="Select"/> element.</description>
///			</item>
///		</list>
///		<para>This element has no other content.</para>
///		<para>This element is defined by the AIML 1.1 specification. Local variables are defined by the AIML 2.0 specification. Tuples are part of an extension to AIML derived from Program AB and Program Y.</para>
/// </remarks>
/// <seealso cref="Select"/><seealso cref="Set"/>
public sealed class Get(TemplateElementCollection key, TemplateElementCollection? tuple, bool local) : TemplateNode {
	public TemplateElementCollection Key { get; } = key;
	public TemplateElementCollection? TupleString { get; } = tuple;
	public bool LocalVar { get; } = local;

	[AimlLoaderContructor]
	public Get(TemplateElementCollection? name, TemplateElementCollection? var, TemplateElementCollection? tuple)
		: this(var ?? name ?? throw new ArgumentException("<get> element must have a name or var attribute", nameof(name)), tuple, var is not null) {
		if (name is not null && var is not null)
			throw new ArgumentException("<get> element cannot have both name and var attributes.", nameof(var));
		if (name is not null && tuple is not null)
			throw new ArgumentException("<get> element with tuple attribute must have a var attribute instead of name.", nameof(var));
	}

	public override string Evaluate(RequestProcess process) {
		if (this.TupleString is not null) {
			// Get a value from a tuple.
			var variable = this.Key.Evaluate(process);
			if (!variable.IsClauseVariable()) {
				process.Log(LogLevel.Warning, $"In element <get>: var was not a valid tuple query variable: {variable}");
				return process.Bot.Config.DefaultPredicate;
			}
			var tupleString = this.TupleString.Evaluate(process);
			return Tuple.GetFromEncoded(tupleString, variable) ?? process.Bot.Config.DefaultPredicate;
		}

		// Get a user predicate or local variable.
		return this.LocalVar ? process.GetVariable(this.Key.Evaluate(process)) : process.User.GetPredicate(this.Key.Evaluate(process));
	}
}
