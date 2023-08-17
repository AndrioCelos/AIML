using System.Xml;

namespace Aiml.Media;
/// <summary>An inline rich media element that splits a message into multiple lines.</summary>
/// <remarks>
///		<para>This element has no content.</para>
///		<para>This element is part of the Pandorabots extension of AIML.</para>
///	</remarks>
public class LineBreak : IMediaElement {
	public static LineBreak FromXml(Bot bot, XmlElement element) => new();
}
