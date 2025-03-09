using System.Collections;

namespace Aiml;
/// <summary>Represents an immutable collection of <see cref="IResponsePart"/> instances.</summary>
public class ResponseContent : IReadOnlyList<IResponsePart> {
	private readonly IResponsePart[] parts;

	internal ResponseContent(params IResponsePart[] parts) => this.parts = parts;
	public ResponseContent(IEnumerable<IResponsePart> parts) : this([.. parts]) { }

	public static ResponseContent Concat(params ResponseContent[] responses) {
		var parts = new IResponsePart[responses.Sum(r => r.Count)];
		var i = 0;
		foreach (var response in responses) {
			response.CopyTo(parts, i);
			i += response.Count;
		}
		return new ResponseContent(parts);
	}

	public override string ToString() => string.Join("", this);

	public void CopyTo(IResponsePart[] target, int index) => parts.CopyTo(target, index);
	public IResponsePart this[int index] => parts[index];
	public int Count => parts.Length;
	public IEnumerator<IResponsePart> GetEnumerator() => ((IReadOnlyList<IResponsePart>) parts).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => parts.GetEnumerator();
}
