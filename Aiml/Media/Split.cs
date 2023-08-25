using System.Xml.Linq;

namespace Aiml.Media;
/// <summary>A separator rich media element that splits a response into multiple messages, without any further semantics.</summary>
/// <remarks>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 2.1 specification.</para>
///	</remarks>
///	<seealso cref="Delay"/> <seealso cref="LineBreak"/>
public class Split : IMediaElement {
	public static Split FromXml(XElement element, Response response) => new();
}
