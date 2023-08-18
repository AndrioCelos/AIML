using System.Xml;

namespace Aiml.Media;
/// <summary>A separator rich media element that introduces a delay between messages.</summary>
/// <remarks>
///		<para>A <see cref="Delay"/> is used to break up a response for easier reading, or to simulate the time a human would take to type the response.</para>
///		<para>In an application that respects delays, further requests should be queued until the delayed messages have been shown.</para>
///		<para>This element is defined by the AIML 2.1 specification.</para>
///	</remarks>
/// <seealso cref="Split"/>
public class Delay(TimeSpan duration) : IMediaElement {
	public TimeSpan Duration { get; } = duration;

	public static Delay FromXml(XmlElement element) => new(TimeSpan.FromSeconds(double.TryParse(element.InnerText, out var d) && d >= 0 ? d : throw new AimlException("Invalid duration in <delay> element.")));
}
