using System.Xml.Linq;

namespace Aiml.Tests.TestExtension;
public class TestCustomTag(XElement element, TemplateElementCollection value1, TemplateElementCollection? value2) : TemplateNode
{
    public XElement Element { get; } = element;
    public TemplateElementCollection Value1 { get; } = value1;
    public TemplateElementCollection? Value2 { get; } = value2;

    public override string Evaluate(RequestProcess process) => $"{Value1.Evaluate(process)} {Value2?.Evaluate(process)}";
}
