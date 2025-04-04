﻿namespace AngelAiml.Tags;
/// <summary>Returns the part of the content after the first word, or <c>DefaultListItem</c> if the evaluated content does not have more than one word.</summary>
/// <remarks>This element is part of the Pandorabots extension of AIML.</remarks>
/// <seealso cref="First"/><seealso cref="Srai"/>
public sealed class Rest(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		var sentence = EvaluateChildren(process).Trim();
		if (sentence == "") return process.Bot.Config.DefaultListItem;

		var delimiter = sentence.IndexOf(' ');
		return delimiter >= 0 ? sentence[(delimiter + 1)..].TrimStart() : process.Bot.Config.DefaultListItem;
	}
}
