namespace Aiml.Sets;
/// <summary>Implements the <c>number</c> set, which includes the decimal representations of all non-negative integers.</summary>
public class NumberSet : Set {
	public override int MaxWords => 1;
	public override bool Contains(string phrase) => phrase.All(char.IsDigit);
}
