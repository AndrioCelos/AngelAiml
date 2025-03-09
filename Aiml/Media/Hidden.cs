using System.Xml.Linq;

namespace Aiml.Media;
/// <summary>An inline rich media element that is hidden from display.</summary>
/// <remarks>This element is not part of the AIML specification.</remarks>
public class Hidden(string text) : IMediaElement {
	public string Text { get; } = text;

	public static Hidden FromXml(XElement element, Response response) {
		var text = string.Join(null, from node in element.Nodes().OfType<XText>() select node.Value);
		return new(text);
	}
}
