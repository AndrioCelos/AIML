using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml {
	public class PathToken {
		/// <summary>The text of this token, or the name of a set.</summary>
		public string Text { get; }
		/// <summary>Specifies whether this <see cref="PathToken"/> is a <c>set</c> tag.</summary>
		public bool IsSet { get; }

		public static PathToken ThatSeparator { get; } = new PathToken("<that>", false);
		public static PathToken TopicSeparator { get; } = new PathToken("<topic>", false);

		public PathToken(string text) : this(text, false) { }
		public PathToken(string text, bool isSet) {
			this.Text = string.Intern(text);
			this.IsSet = isSet;
		}

		public static PathToken Parse(string s) {
			s = s.Trim();
			if (s.StartsWith("<set>") && s.EndsWith("</set>"))
				return new PathToken(s.Substring(5, s.Length - 11).Trim(), true);
			return new PathToken(s, false);
		}
	}
}
