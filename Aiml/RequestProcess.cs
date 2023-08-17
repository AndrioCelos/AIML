using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Aiml; 
/// <summary>Contains data that is used during request processing, but is not stored after the request completes.</summary>
public class RequestProcess {
	RequestSentence Sentence { get; }
	public int RecursionDepth { get; }
	internal Template? template;
	internal readonly List<string> patternPathTokens = new();
	internal readonly List<string> star = new();
	internal readonly List<string> thatstar = new();
	internal readonly List<string> topicstar = new();
	internal Dictionary<string, TestResult>? testResults;
	public ReadOnlyDictionary<string, TestResult>? TestResults;
	public TimeSpan Duration => this.stopwatch.Elapsed;

	internal Stopwatch stopwatch = Stopwatch.StartNew();

	public Bot Bot => this.Sentence.Bot;
	public User User => this.Sentence.User;
	/// <summary>Returns a zero-indexed list of phrases matched by pattern wildcards.</summary>
	public IReadOnlyList<string> Star { get; }
	/// <summary>Returns a zero-indexed list of phrases matched by that pattern wildcards.</summary>
	public IReadOnlyList<string> ThatStar { get; }
	/// <summary>Returns a zero-indexed list of phrases matched by topic pattern wildcards.</summary>
	public IReadOnlyList<string> TopicStar { get; }

	/// <summary>Returns the full normalised path used to search the graph.</summary>
	public string Path => string.Join(" ", this.patternPathTokens);

	/// <summary>Returns the dictionary of local variables in this request.</summary>
	public Dictionary<string, string> Variables { get; }

	public RequestProcess(RequestSentence sentence, int recursionDepth, bool useTests) {
		this.Sentence = sentence;
		this.RecursionDepth = recursionDepth;
		this.Variables = new Dictionary<string, string>(sentence.Bot.Config.StringComparer);
		this.Star = this.star.AsReadOnly();
		this.ThatStar = this.thatstar.AsReadOnly();
		this.TopicStar = this.topicstar.AsReadOnly();
		if (useTests) {
			this.testResults = new Dictionary<string, TestResult>(sentence.Bot.Config.StringComparer);
			this.TestResults = new ReadOnlyDictionary<string, TestResult>(this.testResults);
		}
	}

	/// <summary>Returns the text matched by the pattern wildcard with the specified one-based index, or <see cref="Config.DefaultWildcard"/> if no such wildcard exists.</summary>
	public string GetStar(int num) => --num >= 0 && num < this.star.Count ? this.star[num] : this.Bot.Config.DefaultWildcard;
	/// <summary>Returns the text matched by the that pattern wildcard with the specified one-based index, or <see cref="Config.DefaultWildcard"/> if no such wildcard exists.</summary>
	public string GetThatStar(int num) => --num >= 0 && num < this.thatstar.Count ? this.thatstar[num] : this.Bot.Config.DefaultWildcard;
	/// <summary>Returns the text matched by the topic pattern wildcard with the specified one-based index, or <see cref="Config.DefaultWildcard"/> if no such wildcard exists.</summary>
	public string GetTopicStar(int num) => --num >= 0 && num < this.topicstar.Count ? this.topicstar[num] : this.Bot.Config.DefaultWildcard;

	internal void Finish() => this.stopwatch.Stop();

	internal bool CheckTimeout() => this.stopwatch.ElapsedMilliseconds >= this.Bot.Config.Timeout;

	internal List<string> GetStarList(MatchState matchState) => matchState switch {
		MatchState.Message => this.star,
		MatchState.That => this.thatstar,
		MatchState.Topic => this.topicstar,
		_ => throw new ArgumentException($"Invalid {nameof(MatchState)} value", nameof(matchState)),
	};

	/// <summary>Returns the value of the specified local variable for this request, or <see cref="Config.DefaultPredicate"/> if it is not bound.</summary>
	public string GetVariable(string name) => this.Variables.TryGetValue(name, out var value) ? value : this.Bot.Config.DefaultPredicate;

	/// <summary>Writes a message to the bot's loggers.</summary>
	public void Log(LogLevel level, string message) {
		if (level > LogLevel.Diagnostic || this.RecursionDepth < this.Bot.Config.LogRecursionLimit)
			this.Bot.Log(level, $"[{this.RecursionDepth}] {message}");
	}

	/// <summary>Processes the specified text as a sub-request of the current request and returns the response.</summary>
	public string Srai(string request) {
		var newRequest = new Request(request, this.User, this.Bot);
		return this.Bot.ProcessRequest(newRequest, false, false, this.RecursionDepth + 1, out _).ToString().Trim();
	}
}
