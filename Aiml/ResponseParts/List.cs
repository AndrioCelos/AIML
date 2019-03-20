using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class List : IResponsePart {
		public IReadOnlyList<IReadOnlyList<IResponsePart>> Items { get; }

		public List(IReadOnlyList<IReadOnlyList<IResponsePart>> items) {
			this.Items = items;
		}
	}
}
