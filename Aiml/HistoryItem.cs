using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Aiml {
	/// <summary>Represents an item in the bot's request or response history, containing one or more sentences.</summary>
	public class HistoryItem {
		public string Text { get; }
		public IReadOnlyList<string> Sentences { get; }

		internal HistoryItem(string text, IList<string> sentences) {
			this.Text = text;
			this.Sentences = new ReadOnlyCollection<string>(sentences);
		}
	}
}
