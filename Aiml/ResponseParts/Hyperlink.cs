using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class Hyperlink : IResponsePart {
		public string Text { get; }
		public string URL { get; }

		public Hyperlink(string url) : this(url, url) { }
		public Hyperlink(string text, string url) {
			this.Text = text;
			this.URL = url;
		}
	}

}
