namespace Aiml;
/// <summary>Represents an AIML set. An AIML set is a set of phrases that can be searched as part of a pattern using the <c>set</c> pattern tag.</summary>
/// <remarks>
///     For the specification of sets, see 'Sets and Maps in AIML 2.0' at https://docs.google.com/document/d/1DWHiOOcda58CflDZ0Wsm1CgP3Es6dpicb4MBbbpwzEk/pub.
/// </remarks>
/// <example>
///     The following AIML pattern will allow the bot to recognise whether the user is mentioning a color.
///     <code>
///         <pattern>IS <set>color</set> A COLOR</pattern>
///     </code>
/// </example>
public abstract class Set {
	/// <summary>The maximum number of words in any phrase in the set.</summary>
	public abstract int MaxWords { get; }

	/// <summary>Determines whether the set contains a given phrase.</summary>
	public abstract bool Contains(string phrase);
}
