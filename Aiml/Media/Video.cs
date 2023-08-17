using System.Xml;

namespace Aiml.Media;
/// <summary>A block-level rich media element that links to a video.</summary>
/// <remarks>This element is defined by the AIML 2.1 specification.</remarks>
/// <seealso cref="Image"/>
public class Video(string url) : IMediaElement {
	public string Url { get; } = url;

	public static Video FromXml(Bot bot, XmlElement element) => new(element.InnerText);
}
