using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class UrlButtonResponsePart : IResponsePart {
		public string Text { get; }
		public string URL { get; }

		public UrlButtonResponsePart(string url) : this(url, url) { }
		public UrlButtonResponsePart(string text, string url) {
			this.Text = text;
			this.URL = url;
		}
	}
}
