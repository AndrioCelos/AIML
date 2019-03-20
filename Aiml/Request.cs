using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aiml {
	public class Request {
		public string Text { get; private set; }
		public User User { get; private set; }
		public Bot Bot { get; private set; }
		private Response? response;
		public Response? Response { get; internal set; }
		private RequestSentence[] sentences;
		public IReadOnlyList<RequestSentence> Sentences { get; }

		public Request(string text, User user, Bot bot) {
			this.Text = text;
			this.User = user;
			this.Bot = bot;

			var sentences = bot.SentenceSplit(text, false);
			this.sentences = new RequestSentence[sentences.Length];
			for (int i = 0; i < sentences.Length; ++i) {
				this.sentences[i] = new RequestSentence(this, sentences[i]);
			}
			this.Sentences = Array.AsReadOnly(this.sentences);
		}

		public RequestSentence GetLastSentence(int n) {
			return this.sentences[this.sentences.Length - n];
		}
	}

	public class RequestSentence {
		public Request Request { get; }
		public Bot Bot => this.Request.Bot;
		public User User => this.Request.User;
		public string Text { get; }

		public RequestSentence(Request request, string text) {
			this.Text = request.Bot.Normalize(text);
			this.Request = request;
		}
	}
}
