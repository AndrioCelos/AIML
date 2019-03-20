using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class Carousel : IResponsePart {
		public IReadOnlyList<Card> Cards { get; }

		public Carousel(IReadOnlyList<Card> cards) {
			this.Cards = cards;
		}
	}
}
