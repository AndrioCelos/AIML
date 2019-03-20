using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class Image : IResponsePart {
		public string URL { get; }

		public Image(string url) {
			this.URL = url;
		}
	}

}
