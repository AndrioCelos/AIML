using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     When used in a <c>li</c> element, causes the <see cref="Condition"/> or <see cref="Random"/> check to loop if evaluated, concatenating the outputs.
		/// </summary>
		/// <remarks>
		///     <para>This element has no content.</para>
		///     <para>This element is defined by the AIML 2.0 specification.</para>
		/// </remarks>
		/// <seealso cref="Srai"/>
		public sealed class Loop : TemplateNode {
			public Loop() { }

			public override string Evaluate(RequestProcess process) {
				return string.Empty;
			}

			public static Loop FromXml(XmlNode node, AimlLoader loader) {
				return new Loop();
			}
		}
	}
}
