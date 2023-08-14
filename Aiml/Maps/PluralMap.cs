namespace Aiml.Maps;
/// <summary>Implements the <code>singular</code> map from Pandorabots, which maps English nouns to their plural forms.</summary>
public class PluralMap(Inflector inflector) : Map {
	public Inflector Inflector { get; } = inflector;
	public override string? this[string key] => this.Inflector.Pluralize(key);
}
