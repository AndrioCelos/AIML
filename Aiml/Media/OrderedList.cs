using System.Xml;

namespace Aiml.Media;
/// <summary>An inline rich media element that presents an ordered (numbered) list.</summary>
/// <remarks>This element is defined by the AIML 2.1 specification.</remarks>
/// <seealso cref="List"/>
public class OrderedList(List<IReadOnlyList<IMediaElement>> items) : ListBase(items) {
	public static OrderedList FromXml(Bot bot, XmlElement element) => new(ParseItems(bot, element));
}
