using System.Text;

namespace Aiml;
public class Response(Request request, string text) {
	public Request Request { get; } = request;
	public Bot Bot => this.Request.Bot;
	public User User => this.Request.User;
	internal List<StringBuilder> messageBuilders = new();

	public IReadOnlyList<string> Sentences { get; private set; } = Array.AsReadOnly(request.Bot.SentenceSplit(text, true));

	public TimeSpan Duration { get; }

	/// <summary>Returns the last sentence of the response text.</summary>
	public string GetLastSentence() => this.GetLastSentence(1);
	/// <summary>Returns the <paramref name="n"/>th last sentence of the response text.</summary>
	public string GetLastSentence(int n) {
		return this.Sentences != null
			? this.Sentences[this.Sentences.Count - n]
			: throw new InvalidOperationException("Response is not finished.");
	}

	/// <summary>Returns the response text, excluding rich media elements. Messages are separated with newlines.</summary>
	public override string ToString() => string.Join(" ", this.Sentences);
}
