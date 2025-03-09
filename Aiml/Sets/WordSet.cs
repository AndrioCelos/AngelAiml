namespace Aiml.Sets;
/// <summary>Implements the <c>word</c> set, which includes all single words.</summary>
/// <remarks>This set is not part of the AIML specification.</remarks>
public class WordSet : Set {
	public override int MaxWords => 1;
	public override bool Contains(string phrase) => !string.IsNullOrEmpty(phrase) && !phrase.Any(char.IsWhiteSpace);
}
