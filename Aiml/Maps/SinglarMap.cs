namespace Aiml.Maps;
/// <summary>Implements the <c>singular</c> map from Pandorabots, which maps English nouns to their singular forms.</summary>
public class SingularMap(Inflector inflector) : Map {
	public Inflector Inflector { get; } = inflector;
	public override string? this[string key] => Inflector.Singularize(key);
}
