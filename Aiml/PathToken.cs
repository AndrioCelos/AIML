namespace Aiml;
public class PathToken(string text, bool isSet) {
	/// <summary>The text of this token, or the name of a set.</summary>
	public string Text { get; } = string.Intern(text);
	/// <summary>Specifies whether this <see cref="PathToken"/> is a <c>set</c> tag.</summary>
	public bool IsSet { get; } = isSet;

	public static PathToken ThatSeparator { get; } = new PathToken("<that>", false);
	public static PathToken TopicSeparator { get; } = new PathToken("<topic>", false);

	public PathToken(string text) : this(text, false) { }

	public static PathToken Parse(string s) {
		s = s.Trim();
		return s.StartsWith("<set>") && s.EndsWith("</set>") ? new PathToken(s[5..^6].Trim(), true) : new PathToken(s, false);
	}
}
