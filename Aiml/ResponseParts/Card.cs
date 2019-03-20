using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class Card : IResponsePart {
		public string? ImageUrl { get; }
		public string? Title { get; }
		public string? Subtitle { get; }
		public IReadOnlyList<IResponsePart> Buttons { get; }

		public Card(string? imageUrl, string? title, string? subtitle, IReadOnlyList<IResponsePart> buttons) {
			this.ImageUrl = imageUrl;
			this.Title = title;
			this.Subtitle = subtitle;
			this.Buttons = buttons;
		}

		public override string ToString() {
			var builder = new StringBuilder();
			builder.Append("<card>");
			if (this.ImageUrl != null) {
				builder.Append("<image>");
				builder.Append(this.ImageUrl);
				builder.Append("</image>");
			}
			if (this.Title != null) {
				builder.Append("<title>");
				builder.Append(this.Title);
				builder.Append("</title>");
			}
			if (this.Subtitle != null) {
				builder.Append("<subtitle>");
				builder.Append(this.Subtitle);
				builder.Append("</subtitle>");
			}
			if (this.Buttons != null) {
				foreach (var button in this.Buttons) {
					builder.Append(button.ToString());
				}
			}
			builder.Append("</card>");
			return builder.ToString();
		}
	}
}
