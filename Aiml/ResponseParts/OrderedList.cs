using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class OrderedList : IResponsePart {
		public IReadOnlyList<IReadOnlyList<IResponsePart>> Items { get; }

		public OrderedList(IReadOnlyList<IReadOnlyList<IResponsePart>> items) {
			this.Items = items;
		}
	}
}
