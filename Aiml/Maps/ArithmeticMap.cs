namespace Aiml.Maps;

/// <summary>Represents a map that maps integers using an addition. It implements the <code>predecessor</code> and <code>successor</code> maps from Pandorabots.</summary>
internal class ArithmeticMap(int addend) : Map {
	public int Addend { get; } = addend;
	public override string? this[string key] => int.TryParse(key, out var value) ? (value + this.Addend).ToString() : null;
}
