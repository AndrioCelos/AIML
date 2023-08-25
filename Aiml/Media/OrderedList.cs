using System.Xml.Linq;

namespace Aiml.Media;
/// <summary>An inline rich media element that presents an ordered (numbered) list.</summary>
/// <remarks>This element is defined by the AIML 2.1 specification.</remarks>
/// <seealso cref="List"/>
public class OrderedList(List<IReadOnlyList<IMediaElement>> items) : ListBase(items) {
	public static OrderedList FromXml(XElement element, Response response) => new(ParseItems(element, response));
}
