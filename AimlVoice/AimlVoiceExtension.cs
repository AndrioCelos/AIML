using System.Xml;
using System.Xml.Linq;
using Aiml;

namespace AimlVoice;
internal class AimlVoiceExtension : IAimlExtension {
	public void Initialise() {
		AimlLoader.AddCustomOobHandler("setgrammar", OobSetGrammar);
		AimlLoader.AddCustomOobHandler("enablegrammar", OobEnableGrammar);
		AimlLoader.AddCustomOobHandler("disablegrammar", OobDisableGrammar);
		AimlLoader.AddCustomOobHandler("setpartialinput", OobPartialInput);

		AimlLoader.AddCustomMediaElement("speak", MediaElementType.Inline, SpeakElement.FromXml, "s", "alt");
		AimlLoader.AddCustomMediaElement("priority", MediaElementType.Block, (_, _) => new PriorityElement());
		AimlLoader.AddCustomMediaElement("queue", MediaElementType.Block, (_, _) => new PriorityElement());
	}

	private static void OobPartialInput(XElement element) {
		Program.SetPartialInput(element.Value.ToLowerInvariant() switch {
			"off" or "false" or "0" => PartialInputMode.Off,
			"on" or "true" or "1" => PartialInputMode.On,
			"continuous" or "2" => PartialInputMode.Continuous,
			_ => throw new ArgumentException($"Invalid partial input setting '{element.Value}'.")
		});
	}

	private static void OobSetGrammar(XElement element) {
		Program.TrySwitchGrammar(element.Value);
	}
	private static void OobDisableGrammar(XElement element) {
		Program.TryDisableGrammar(element.Value);
	}
	private static void OobEnableGrammar(XElement element) {
		Program.TryEnableGrammar(element.Value);
	}
}
