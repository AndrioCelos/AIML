using System.Collections.ObjectModel;
using System.Text;
using System.Xml;

namespace Aiml.Tags;
/// <summary>Returns up to one of a choice of child elements depending on the results of matching a predicate against a pattern.</summary>
/// <remarks>
///		<para>This element has three forms:</para>
///		<list type="bullet">
///			<item>
///				<term><c><![CDATA[<condition name='predicate' value='v'>]]></c> or <c><![CDATA[<condition var='variable' value='v'>]]></c></term>
///				<description>
///					<para>If the value of the specified predicate or local variable matches the specified value, it returns its contents; otherwise it returns the empty string.</para>	
///				</description>
///			</item>
///			<item>
///				<term><c><![CDATA[<condition name='predicate'>]]></c> or <c><![CDATA[<condition var='variable'>]]></c></term>
///				<description>
///					<para>This form can only contain <c>li</c> elements as direct children.</para>
///					<para>The first <c>li</c> element that matches the value of the specified predicate or variable is returned.</para>	
///					<para>The last <c>li</c> element may lack a <c>value</c> attribute, in which case it will match by default if no earlier item matches.</para>
///				</description>
///			</item>
///			<item>
///				<term><c><![CDATA[<condition>]]></c></term>
///				<description>
///					<para>This form can only contain <c>li</c> elements as direct children.</para>
///					<para>The first <c>li</c> element whose specified predicate or variable matches its specified value is returned.</para>	
///					<para>The last <c>li</c> element may lack attributes, in which case it will match by default if no earlier item matches.</para>
///				</description>
///			</item>
///		</list>
///		<para>In each case, if the value is <c>*</c>, it instead checks whether the predicate or variable is bound to any value.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="Get"/><seealso cref="Random"/>
public sealed class Condition : TemplateNode {
	private readonly Li[] items;
	public ReadOnlyCollection<Li> Items { get; }

	[AimlLoaderContructor]
	public Condition(TemplateElementCollection? name, TemplateElementCollection? var, TemplateElementCollection? value, Li[] items, TemplateElementCollection children)
		: this(name ?? var, var is not null, items.Length > 0 ? items : new[] { new Li(value ?? throw new ArgumentException("<condition> element without <li> items must have a value attribute.", nameof(value)), children) }) {
		if (name is not null && var is not null)
			throw new ArgumentException("<condition> element cannot have both name and var attributes.", nameof(var));
		if (name is null && var is null && items.Length == 0)
			throw new ArgumentException("<condition> element without <li> children must have a name or var attribute.", nameof(var));
	}
	public Condition(TemplateElementCollection? key, bool localVar, Li[] items) {
		var hasDefaultItem = false;
		if (items.Length == 0) throw new AimlException("<condition> element must contain at least one item.");
		foreach (var item in items) {
			if (hasDefaultItem) throw new AimlException("<condition> element default <li> item must be last.");
			if (item.Key == null) {
				if (item.Value != null) {
					item.Key = key ?? throw new AimlException("<condition> element or a non-default <li> item must have a name or var attribute.");
					item.LocalVar = localVar;
				} else
					hasDefaultItem = true;
			} else if (item.Value == null)
				throw new AimlException("<condition> element or a non-default <li> item must have a value attribute.");
		}
		this.items = items;
		this.Items = new ReadOnlyCollection<Li>(items);
	}
	public Condition(Li[] items) : this(null, false, items) { }

	public Li? Pick(RequestProcess process) {
		foreach (var item in this.items) {
			var key = item.Key?.Evaluate(process);
			var checkValue = item.Value?.Evaluate(process);

			if (key != null && checkValue != null) {
				if (checkValue == "*") {
					// '*' is a match if the predicate is bound at all.
					if (item.LocalVar) {
						if (process.Variables.ContainsKey(key)) {
							process.Log(LogLevel.Diagnostic, $"In element <condition>: Local variable {key} matches *.");
							return item;
						}
					} else {
						if (process.User.Predicates.ContainsKey(key)) {
							process.Log(LogLevel.Diagnostic, $"In element <condition>: Predicate {key} matches *.");
							return item;
						}
					}
				} else {
					if (item.LocalVar) {
						if (process.Bot.Config.StringComparer.Equals(process.GetVariable(key), checkValue)) {
							process.Log(LogLevel.Diagnostic, $"In element <condition>: Local variable {key} matches {checkValue}.");
							return item;
						}
					} else {
						if (process.Bot.Config.StringComparer.Equals(checkValue, process.User.GetPredicate(key))) {
							process.Log(LogLevel.Diagnostic, $"In element <condition>: {(item.LocalVar ? "Local variable" : "Predicate")} {key} matches {checkValue}.");
							return item;
						}
					}
				}
				// No match; keep looking.
				process.Log(LogLevel.Diagnostic, $"In element <condition>: {(item.LocalVar ? "Local variable" : "Predicate")} {key} does not match {checkValue}.");
			} else if (key == null && checkValue == null) {
				// Default case.
				return item;
			} else {
				process.Log(LogLevel.Warning, "In element <condition>: Missing name, var or value attribute in <li>.");
			}
		}

		return null;
	}

	public override string Evaluate(RequestProcess process) {
		var builder = new StringBuilder();

		Li item; var loops = 0;
		do {
			++loops;
			if (loops > process.Bot.Config.LoopLimit) {
				process.Log(LogLevel.Warning, "Loop limit exceeded. User: " + process.User.ID + "; path: \"" + process.Path + "\"");
				throw new LoopLimitException();
			}

			item = this.Pick(process);
			if (item is null) break;
			builder.Append(item.Evaluate(process));
		} while (item.Children != null && item.Children.Loop);

		return builder.ToString();
	}

	public class Li(TemplateElementCollection? key, bool localVar, TemplateElementCollection? value, TemplateElementCollection children) : RecursiveTemplateTag(children) {
		public TemplateElementCollection? Key { get; internal set; } = key;
		public TemplateElementCollection? Value { get; } = value;
		public bool LocalVar { get; internal set; } = localVar;

		[AimlLoaderContructor]
		public Li(TemplateElementCollection? name, TemplateElementCollection? var, TemplateElementCollection? value, TemplateElementCollection children) : this(name ?? var, var is not null, value, children) {
			if (name is not null && var is not null)
				throw new ArgumentException("<li> element cannot have both name and var attributes.", nameof(var));
		}
		public Li(TemplateElementCollection value, TemplateElementCollection children) : this(null, false, value, children) { }

		public override string Evaluate(RequestProcess process) => this.EvaluateChildren(process);
	}
}
