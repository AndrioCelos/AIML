using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class PlainText : IResponsePart {
		public string Text { get; }

		public PlainText(string text) {
			this.Text = text;
		}

		public override string ToString() => this.Text;
	}

}
