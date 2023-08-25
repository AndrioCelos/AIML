using System.Xml.Linq;

namespace Aiml.Media;
/// <summary>An inline rich media element that presents an unordered (bulleted) list.</summary>
/// <remarks>This element is defined by the AIML 2.1 specification.</remarks>
/// <seealso cref="OrderedList"/>
public class List(List<IReadOnlyList<IMediaElement>> items) : ListBase(items) {
	public static List FromXml(XElement element, Response response) => new(ParseItems(element, response));
}
