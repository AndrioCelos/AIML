using System.Xml.Linq;

namespace Aiml.Media;
/// <summary>A block-level rich media element that links to an image.</summary>
/// <remarks>This element is defined by the AIML 2.1 specification.</remarks>
/// <seealso cref="Video"/>
public class Image(string url) : IMediaElement {
	public string Url { get; } = url;

	public static Image FromXml(XElement element, Response response) => new(element.Value);
}
