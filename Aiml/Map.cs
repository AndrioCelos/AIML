namespace Aiml;
/// <summary>Represents an AIML map. An AIML map maps phrases to another phrase.</summary>
/// <remarks>For the specification of maps, see 'Sets and Maps in AIML 2.0' at https://docs.google.com/document/d/1DWHiOOcda58CflDZ0Wsm1CgP3Es6dpicb4MBbbpwzEk/pub </remarks>
public abstract class Map {
	/// <summary>
	///     Returns the text that the given key maps to, if it is in the map, or null otherwise.
	/// </summary>
	/// <param name="key">The word or phrase to search for.</param>
	public abstract string? this[string key] { get; }
}
