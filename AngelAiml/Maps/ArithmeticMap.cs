namespace AngelAiml.Maps;

/// <summary>Represents a map that maps integers using an addition. It implements the <c>predecessor</c> and <c>successor</c> maps from Pandorabots.</summary>
internal class ArithmeticMap(int addend) : Map {
	public int Addend { get; } = addend;
	public override string? this[string key] => int.TryParse(key, out var value) ? (value + Addend).ToString() : null;
}
