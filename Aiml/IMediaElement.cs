using System.Xml;

namespace Aiml;
public interface IMediaElement { }

public class MediaText(string text) : IMediaElement {
	public string Text { get; } = text;
	public override string ToString() => this.Text;

	internal static MediaText Empty { get; } = new("");
	internal static MediaText Space { get; } = new(" ");
}
