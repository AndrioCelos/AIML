using System.Xml;
using Aiml;

namespace AimlVoice;
internal class AimlVoiceExtension : IAimlExtension {
	public void Initialise() {
		AimlLoader.AddCustomOobHandler("setgrammar", OobSetGrammar);
		AimlLoader.AddCustomOobHandler("enablegrammar", OobEnableGrammar);
		AimlLoader.AddCustomOobHandler("disablegrammar", OobDisableGrammar);
		AimlLoader.AddCustomOobHandler("setpartialinput", OobPartialInput);

		AimlLoader.AddCustomMediaElement("speak", MediaElementType.Inline, SpeakElement.FromXml, "s", "alt");
		AimlLoader.AddCustomMediaElement("priority", MediaElementType.Block, _ => new PriorityElement());
		AimlLoader.AddCustomMediaElement("queue", MediaElementType.Block, _ => new PriorityElement());
	}

	private static void OobPartialInput(XmlElement element) {
		Program.SetPartialInput(element.InnerText.ToLowerInvariant() switch {
			"off" or "false" or "0" => PartialInputMode.Off,
			"on" or "true" or "1" => PartialInputMode.On,
			"continuous" or "2" => PartialInputMode.Continuous,
			_ => throw new ArgumentException($"Invalid partial input setting '{element.InnerText}'.")
		});
	}

	private static void OobSetGrammar(XmlElement element) {
		Program.TrySwitchGrammar(element.InnerText);
	}
	private static void OobDisableGrammar(XmlElement element) {
		Program.TryDisableGrammar(element.InnerText);
	}
	private static void OobEnableGrammar(XmlElement element) {
		Program.TryEnableGrammar(element.InnerText);
	}
}
