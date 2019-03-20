using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Aiml.ResponseParts {

	public class OOBResponsePart : IResponsePart {
		public IReadOnlyList<IResponsePart> Contents { get; }

		internal OOBResponsePart(IList<IResponsePart> contents) {
			this.Contents = new ReadOnlyCollection<IResponsePart>(contents);
		}
	}
}
