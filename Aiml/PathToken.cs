namespace Aiml;
public class PathToken(string text, bool isSet) {
	/// <summary>The text of this token, or the name of a set.</summary>
	public string Text { get; } = string.Intern(text);
	/// <summary>Specifies whether this <see cref="PathToken"/> is a <c>set</c> tag.</summary>
	public bool IsSet { get; } = isSet;

	public static readonly PathToken Star = new("*", false);
	public static readonly PathToken ThatSeparator = new("<that>", false);
	public static readonly PathToken TopicSeparator = new("<topic>", false);

	public PathToken(string text) : this(text, false) { }

	public override string ToString() => this.IsSet ? $"<set>{this.Text}</set>" : this.Text;
}
