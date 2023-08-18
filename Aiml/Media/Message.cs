namespace Aiml.Media;
/// <summary>A fragment of a response that should be presented as a single message, represented as a collection of rich media elements.</summary>
public class Message(IMediaElement[] inlineElements, IMediaElement[] blockElements, IMediaElement? separator) {
	public IReadOnlyList<IMediaElement> InlineElements { get; } = Array.AsReadOnly(inlineElements);
	public IReadOnlyList<IMediaElement> BlockElements { get; } = Array.AsReadOnly(blockElements);
	public IMediaElement? Separator { get; } = separator;
}
