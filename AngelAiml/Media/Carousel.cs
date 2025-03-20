using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace AngelAiml.Media;
/// <summary>A block-level rich media element that presents a menu of one or more <see cref="Card"/> elements.</summary>
/// <remarks>This element is defined by the AIML 2.1 specification.</remarks>
public class Carousel : IMediaElement {
	public IReadOnlyList<Card> Cards { get; }

	public Carousel(List<Card> cards) => Cards = cards.AsReadOnly();
	public Carousel(params Card[] cards) => Cards = Array.AsReadOnly(cards);
	public Carousel(IList<Card> cards) => Cards = new ReadOnlyCollection<Card>(cards);

	public static Carousel FromXml(XElement element, Response response) {
		var cards = new List<Card>();
		foreach (var childElement in element.Elements()) {
			switch (childElement.Name.LocalName.ToLowerInvariant()) {
				case "card": cards.Add(Card.FromXml(childElement, response)); break;
				default: throw new AimlException($"Unknown attribute {childElement.Name}", element);
			}
		}
		return new(cards);
	}
}
