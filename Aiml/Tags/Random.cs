namespace Aiml.Tags;
/// <summary>Randomly selects and returns one of its child elements.</summary>
/// <remarks>
///		<para>This element can only contain <c>li</c> elements as direct children.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="Condition"/>
public sealed class Random : TemplateNode {
	public Li[] Items { get; set; }

	public Random(Li[] items) {
		if (items.Length == 0) throw new ArgumentException("<random> element must contain at least one item.", nameof(items));
		this.Items = items;
	}

	public Li Pick(RequestProcess process) => this.Items[process.Bot.Random.Next(this.Items.Length)];

	public override string Evaluate(RequestProcess process) => this.Pick(process).Evaluate(process);

	public class Li(TemplateElementCollection children) : RecursiveTemplateTag(children) {
		public override string Evaluate(RequestProcess process) => this.Children.Evaluate(process);
	}
}
