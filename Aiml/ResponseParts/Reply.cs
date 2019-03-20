using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class Reply : IResponsePart {
		public string Text { get; }
		public string Postback { get; }

		public Reply(string text) : this(text, text) { }
		public Reply(string text, string postback) {
			this.Text = text;
			this.Postback = postback;
		}
	}
}
