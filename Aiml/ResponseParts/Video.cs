using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class Video : IResponsePart {
		public string URL { get; }

		public Video(string url) {
			this.URL = url;
		}
	}

}
