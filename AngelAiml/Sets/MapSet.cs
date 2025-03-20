namespace AngelAiml.Sets;
/// <summary>Represents the set of phrases that are keys in an AIML map.</summary>
public class MapSet : Set {
	public Map Map { get; }
	public Bot Bot { get; }
	public override int MaxWords { get; }

	public MapSet(string mapName, Bot bot) {
		Bot = bot ?? throw new ArgumentNullException(nameof(bot));
		Map = Bot.Maps[mapName];
		MaxWords = Map is Maps.StringMap stringMap
			? stringMap.Keys.Max(s => s.Split([' '], StringSplitOptions.RemoveEmptyEntries).Length)
			: int.MaxValue;
	}

	public override bool Contains(string phrase) => Map is Maps.StringMap stringMap
		? stringMap.ContainsKey(phrase)
		: Map[phrase] is not null;
}
