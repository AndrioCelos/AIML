using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace Aiml {
	/// <summary>Contains data that is used during request processing, but is not stored after the request completes.</summary>
	public class RequestProcess {
		RequestSentence Sentence { get; }
		public int RecursionDepth { get; }
		internal Template? template;
		internal List<string> patternPathTokens = new List<string>();
		internal List<string> star = new List<string>();
		internal List<string> thatstar = new List<string>();
		internal List<string> topicstar = new List<string>();
		internal Dictionary<string, TestResult>? testResults;
		public ReadOnlyDictionary<string, TestResult>? TestResults;
		public TimeSpan Duration => this.stopwatch.Elapsed;

		internal Stopwatch stopwatch = Stopwatch.StartNew();

		public Bot Bot => this.Sentence.Bot;
		public User User => this.Sentence.User;

		public string Path => string.Join(" ", this.patternPathTokens);

		public Dictionary<string, string> Variables { get; }

		public RequestProcess(RequestSentence sentence, int recursionDepth, bool useTests) {
			this.Sentence = sentence;
			this.RecursionDepth = recursionDepth;
			this.Variables = new Dictionary<string, string>(sentence.Bot.Config.StringComparer);
			if (useTests) {
				this.testResults = new Dictionary<string, TestResult>(sentence.Bot.Config.StringComparer);
				this.TestResults = new ReadOnlyDictionary<string, TestResult>(this.testResults);
			}
		}

		internal void Finish() => this.stopwatch.Stop();

		public bool CheckTimeout() => this.stopwatch.ElapsedMilliseconds >= this.Bot.Config.Timeout;

		internal List<string> GetStar(MatchState matchState) {
			switch (matchState) {
				case MatchState.Message: return this.star;
				case MatchState.That: return this.thatstar;
				case MatchState.Topic: return this.topicstar;
				default: throw new ArgumentException($"Invalid {nameof(MatchState)} value", nameof(matchState));
			}
		}

		public string GetVariable(string name) {
			string value;
			if (this.Variables.TryGetValue(name, out value)) return value;
			if (this.Bot.Config.DefaultPredicates.TryGetValue(name, out value)) return value;
			return this.Bot.Config.DefaultPredicate;
		}

		public void Log(LogLevel level, string message) {
			if (level > LogLevel.Diagnostic || this.RecursionDepth < this.Bot.Config.LogRecursionLimit)
				this.Bot.Log(level, "[" + this.RecursionDepth + "] " + message);
		}
	}
}
