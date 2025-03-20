using System.Xml;
using System.Xml.Linq;

namespace AngelAiml;
[Serializable]
public class AimlException : XmlException {
	public AimlException(string message, XElement element) : base(GetAugmentMessage(message, element)) { }
	public AimlException(string message, XElement element, Exception innerException) : base(GetAugmentMessage(message, element), innerException) { }

	private static string GetAugmentMessage(string message, XElement element)
		=> ((IXmlLineInfo) element).HasLineInfo()
			? $"In element <{element.Name}>: {message}, {(element.BaseUri != "" ? element.BaseUri : "<no URI>")} line {((IXmlLineInfo) element).LineNumber} column {((IXmlLineInfo) element).LinePosition}"
			: element.BaseUri != ""
			? $"In element <{element.Name}>: {message}, {element.BaseUri}"
			: $"In element <{element.Name}>: {message}";
}

[Serializable]
public class RecursionLimitException(string message) : Exception(message) {
	public RecursionLimitException() : this("The request exceeded the AIML recursion limit.") { }
}

[Serializable]
public class LoopLimitException(string message) : Exception(message) {
	public LoopLimitException() : this("The request exceeded the AIML loop limit.") { }
}
