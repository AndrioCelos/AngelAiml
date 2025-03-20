using System.Xml.Linq;
using AngelAiml;

namespace AngelAimlVoiceConsole;
internal class SpeakElement(XElement ssml, string altText) : IMediaElement {
	public XElement SSML { get; } = ssml;
	public string AltText { get; } = altText;

	public static SpeakElement FromXml(XElement element, Response response) {
		if (element.Attribute("version") is null)
			element.SetAttributeValue("version", "1.0");

		var langName = XNamespace.Xml + "lang";
		if (element.Attribute(langName) is null)
			element.SetAttributeValue(langName, response.Bot.Config.Locale.Name.ToLowerInvariant());

		var node = element.Elements().FirstOrDefault(el => el.Name.LocalName.Equals("alt", StringComparison.OrdinalIgnoreCase));
		string? altText;
		if (node is not null) {
			altText = node.Value;
			node.Remove();
		} else
			altText = element.Value;

		return new(element, altText);
	}
}
