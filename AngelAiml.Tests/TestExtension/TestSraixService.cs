using System.Xml.Linq;

namespace AngelAiml.Tests.TestExtension;

internal class TestSraixService : ISraixService {
	public string Process(string text, XElement element, RequestProcess process) {
		Assert.Multiple(() => {
			Assert.That(text, Is.EqualTo("arguments"));
			Assert.That(element.Attribute("customattr")?.Value, Is.EqualTo("Sample"));
			Assert.That(process.GetVariable("bar"), Is.EqualTo("var"));
		});
		return "Success";
	}
}

internal class TestFaultSraixService() : ISraixService {
	public string Process(string text, XElement element, RequestProcess process) => throw new Exception("Test exception");
}
