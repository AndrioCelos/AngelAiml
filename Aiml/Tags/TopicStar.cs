using System.Xml;

namespace Aiml;
public partial class TemplateNode {
	/// <summary>Returns the text matched by a wildcard or <c>set</c> tag in a <c>topic</c> pattern.</summary>
	/// <remarks>
	///		<para>This element has the following attribute:</para>
	///		<list type="table">
	///			<item>
	///				<term><c>index</c></term>
	///				<description>the one-based index of the wildcard or set tag to check. If omitted, 1 is used.</description>
	///			</item>
	///		</list>
	///		<para>This element has no content.</para>
	///		<para>This element is defined by the AIML 1.1 specification.</para>
	/// </remarks>
	/// <seealso cref="Star"/><seealso cref="ThatStar"/>
	public sealed class TopicStar(TemplateElementCollection index) : TemplateNode {
		public TemplateElementCollection Index { get; private set; } = index;

		public TopicStar() : this(new TemplateElementCollection("1")) { }

		public override string Evaluate(RequestProcess process) {
			var index = int.Parse(this.Index.Evaluate(process));

			if (process.topicstar.Count < index) return process.Bot.Config.DefaultWildcard;
			var match = process.topicstar[index - 1];
			return match == "" ? process.Bot.Config.DefaultWildcard : match;
		}

		public static TopicStar FromXml(XmlNode node, AimlLoader loader) {
			// Search for XML attributes.
			XmlAttribute attribute;

			var index = new TemplateElementCollection("1");

			attribute = node.Attributes["index"];
			if (attribute != null) index = new TemplateElementCollection(attribute.Value);

			// Search for properties in elements.
			foreach (XmlNode node2 in node.ChildNodes) {
				if (node2.NodeType == XmlNodeType.Element) {
					if (node2.Name.Equals("index", StringComparison.InvariantCultureIgnoreCase))
						index = TemplateElementCollection.FromXml(node2, loader);
				}
			}

			return new TopicStar(index);
		}
	}
}
