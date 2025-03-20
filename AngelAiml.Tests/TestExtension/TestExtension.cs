namespace AngelAiml.Tests.TestExtension;
public class TestExtension : IAimlExtension {
	internal static List<TestExtension> instances = [];

	internal int Initialised { get; private set; }

	public TestExtension() => instances.Add(this);

	public void Initialise() => Initialised++;
}
