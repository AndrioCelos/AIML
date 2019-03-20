using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Aiml {
	public class Response {
		public Request Request { get; }
		public Bot Bot => this.Request.Bot;
		public User User => this.Request.User;
		internal List<StringBuilder> messageBuilders = new List<StringBuilder>();

		public IReadOnlyList<string> Sentences { get; private set; }

		public TimeSpan Duration { get; }

		public Response(Request request, string text) {
			this.Request = request;
			this.Sentences = Array.AsReadOnly(request.Bot.SentenceSplit(text, true));
		}

		/// <summary>Returns the last sentence of the response text.</summary>
		public string GetLastSentence() => this.GetLastSentence(1);
		/// <summary>Returns the <paramref name="n"/>th last sentence of the response text.</summary>
		public string GetLastSentence(int n) {
			if (this.Sentences == null) throw new InvalidOperationException("Response is not finished.");
			return this.Sentences[this.Sentences.Count - n];
		}

		/// <summary>Returns the response text, excluding rich media elements. Messages are separated with newlines.</summary>
		public override string ToString() => string.Join(" ", this.Sentences);
	}
}
