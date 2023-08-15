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
///				<description>Returns the value of a tuple variable set by a <see cref="Select"/> element.</description>
///			</item>
///		</list>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 1.1 specification. Local variables are defined by the AIML 2.0 specification. Tuples are part of an extension to AIML derived from Program AB.</para>
/// </remarks>
/// <seealso cref="Select"/><seealso cref="Set"/>
public sealed class Get(TemplateElementCollection key, TemplateElementCollection? tuple, bool local) : TemplateNode {
	public TemplateElementCollection Key { get; } = key;
	public TemplateElementCollection? TupleKey { get; } = tuple;
	public bool LocalVar { get; } = local;

	[AimlLoaderContructor]
	public Get(TemplateElementCollection? name, TemplateElementCollection? var, TemplateElementCollection? tuple)
		: this(var ?? name ?? throw new ArgumentException("<get> element must have a name or var attribute", nameof(name)), tuple, var is not null) {
		if (name is not null && var is not null)
			throw new ArgumentException("<get> element cannot have both name and var attributes.", nameof(var));
		if (name is not null && tuple is not null)
			throw new ArgumentException("<get> element cannot have both name and tuple attributes.", nameof(var));
	}

	public override string Evaluate(RequestProcess process) {
		if (this.TupleKey is not null) {
			// Get a value from a tuple.
			var value = this.TupleKey.Evaluate(process);
			if (!string.IsNullOrWhiteSpace(value)) {
				if (int.TryParse(value, out var index) && index >= 0 && index < Tuple.Tuples.Count) {
					var tuple = Tuple.Tuples[index];
					if (tuple.TryGetValue(this.Key.Evaluate(process), out value)) return value;
				}
			}
			return process.Bot.Config.DefaultPredicate;
		}

		// Get a user predicate or local variable.
		return this.LocalVar ? process.GetVariable(this.Key.Evaluate(process)) : process.User.GetPredicate(this.Key.Evaluate(process));
	}
}
