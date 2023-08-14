namespace Aiml.Sets;
/// <summary>Implements the <code>number</code> set from Pandorabots, which includes all non-negative decimal integers.</summary>
public class NumberSet : Set {
	public override int MaxWords => 1;
	public override bool Contains(string phrase) => phrase.All(char.IsDigit);
}
