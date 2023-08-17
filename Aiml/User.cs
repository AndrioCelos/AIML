namespace Aiml;
public class User {
	public string ID { get; }
	public Bot Bot { get; }
	public History<Request> Requests { get; }
	public History<Response> Responses { get; }
	public Dictionary<string, string> Predicates { get; }
	public PatternNode Graphmaster { get; }

	public string That { get; private set; }

	public string Topic {
		get => this.GetPredicate("topic");
		set => this.Predicates["topic"] = value;
	}

	public User(string ID, Bot bot) {
		if (string.IsNullOrEmpty(ID)) throw new ArgumentException("The user ID cannot be empty", nameof(ID));
		this.ID = ID;
		this.Bot = bot;
		this.That = bot.Config.DefaultHistory;
		this.Requests = new History<Request>(bot.Config.HistorySize);
		this.Responses = new History<Response>(bot.Config.HistorySize);
		this.Predicates = new Dictionary<string, string>(StringComparer.Create(bot.Config.Locale, true));
		this.Graphmaster = new PatternNode(null, bot.Config.StringComparer);
	}

	/// <summary>Returns the last sentence output from the bot to this user.</summary>
	public string GetThat() => this.That;
	/// <summary>Returns the last sentence in the <paramref name='n'/>th last message from the bot to this user.</summary>
	public string GetThat(int n) => this.GetThat(n, 1);
	/// <summary>Returns the <paramref name='n'/>th last sentence in the <paramref name='n'/>th last message from the bot to this user.</summary>
	public string GetThat(int n, int sentence)
		=> n >= 1 && n <= this.Responses.Count && sentence >= 1 && this.Responses[n - 1] is var response && sentence <= this.Responses[n - 1].Sentences.Count
			? response.GetLastSentence(sentence)
			: this.Bot.Config.DefaultHistory;

	public string GetInput() => this.GetInput(1, 1);
	public string GetInput(int n) => this.GetInput(n, 1);
	public string GetInput(int n, int sentence)
		=> n >= 1 && n <= this.Requests.Count && sentence >= 1 && this.Requests[n - 1] is var response && sentence <= response.Sentences.Count
			? response.GetLastSentence(sentence).Text
			: this.Bot.Config.DefaultHistory;

	public string GetRequest() => this.GetRequest(1);
	public string GetRequest(int n) => n >= 1 & n <= this.Requests.Count ? this.Requests[n - 2].Text : this.Bot.Config.DefaultHistory;
	// Unlike <input>, the <request> tag does not count the request currently being processed.

	public string GetResponse() => this.GetResponse(1);
	public string GetResponse(int n) => n >= 1 & n <= this.Responses.Count
		? this.Responses[n - 1].ToString()
		: this.Bot.Config.DefaultHistory;

	public void AddResponse(Response response) {
		this.Responses.Add(response);
		if (!(this.Bot.Config.ThatExcludeEmptyResponse && string.IsNullOrWhiteSpace(response.Text)))
			this.That = response.Text;
	}
	public void AddRequest(Request request) => this.Requests.Add(request);

	public string GetPredicate(string key)
		=> this.Predicates.TryGetValue(key, out var value) || this.Bot.Config.DefaultPredicates.TryGetValue(key, out value)
			? value
			: this.Bot.Config.DefaultPredicate;
}
