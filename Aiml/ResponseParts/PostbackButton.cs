using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class PostbackButtonResponsePart : IResponsePart {
		public string Text { get; }
		public string Postback { get; }

		public PostbackButtonResponsePart(string text) : this(text, text) { }
		public PostbackButtonResponsePart(string text, string postback) {
			this.Text = text;
			this.Postback = postback;
		}
	}
}
