﻿namespace AngelAiml;
/// <summary>Represents an RDF triple: a directed relationship between two entities (the <see cref="Subject"/> and <see cref="Object"/>) that is notated with a <see cref="Predicate"/>.</summary>
/// <seealso href="https://www.w3.org/TR/2004/REC-rdf-concepts-20040210/"/>
public class Triple(string subj, string pred, string obj) {
	public string Subject { get; } = subj;
	public string Predicate { get; } = pred;
	public string Object { get; } = obj;

	public void Deconstruct(out string subj, out string pred, out string obj) {
		subj = Subject;
		pred = Predicate;
		obj = Object;
	}

	public override string ToString() => $"{{ Subject = {Subject}, Predicate = {Predicate}, Object = {Object} }}";
}
