using System.Collections;

namespace Aiml;
/// <summary>Represents an immutable collection of <see cref="IResponsePart"/> instances.</summary>
public class ResponseContent : IReadOnlyList<IResponsePart> {
	private readonly IResponsePart[] parts;

	internal ResponseContent(params IResponsePart[] parts) => this.parts = parts;
	public ResponseContent(IEnumerable<IResponsePart> parts) : this(parts.ToArray()) { }

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

	public void CopyTo(IResponsePart[] target, int index) => this.parts.CopyTo(target, index);
	public IResponsePart this[int index] => this.parts[index];
	public int Count => this.parts.Length;
	public IEnumerator<IResponsePart> GetEnumerator() => ((IReadOnlyList<IResponsePart>) this.parts).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => this.parts.GetEnumerator();
}
