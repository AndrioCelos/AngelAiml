﻿namespace AngelAiml.Tags;
/// <summary>When used in a <c>li</c> element, causes the <see cref="Condition"/> or <see cref="Random"/> check to loop if evaluated, concatenating the outputs.</summary>
/// <remarks>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="Srai"/>
public sealed class Loop : TemplateNode {
	public override string Evaluate(RequestProcess process) => "";
}
