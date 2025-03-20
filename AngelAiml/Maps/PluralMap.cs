namespace AngelAiml.Maps;
/// <summary>Implements the <c>singular</c> map from Pandorabots, which maps English nouns to their plural forms.</summary>
public class PluralMap(Inflector inflector) : Map {
	public Inflector Inflector { get; } = inflector;
	public override string? this[string key] => Inflector.Pluralize(key);
}
