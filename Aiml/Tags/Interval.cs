using System;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns the amount of time between two dates.
		/// </summary>
		/// <remarks>
		///     This element is defined by the AIML 2.0 specification.
		/// </remarks>
		public sealed class Interval : TemplateNode {
			public TemplateElementCollection JFormat { get; set; }
			public TemplateElementCollection Start { get; set; }
			public TemplateElementCollection End { get; set; }
			public TemplateElementCollection Style { get; set; }

			public Interval(TemplateElementCollection jformat, TemplateElementCollection start, TemplateElementCollection end, TemplateElementCollection style) {
				this.JFormat = jformat;
				this.Start = start;
				this.End = end;
				this.Style = style;
			}

			public override string Evaluate(RequestProcess process) {
				string jformat = null; string format = null;
				if (this.JFormat != null) jformat = this.JFormat.Evaluate(process);
				DateTime start; DateTime end;
				string startString, endString;

				// Parse the dates.
				startString = this.Start.Evaluate(process);
				try {
					if (jformat == null) {
						start = DateTime.Parse(startString);
					} else {
						format = ConvertJavaFormat(jformat);
						start = DateTime.ParseExact(startString, jformat, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal);
					}
				} catch (FormatException) {
					if (format == null) process.Log(LogLevel.Warning, "In element <interval>: Could not parse '" + startString + "' as a date.");
					else process.Log(LogLevel.Warning, "In element <interval>: Could not parse '" + startString + "' as a date with format '" + jformat + "'.");
					return "unknown";
				}

				endString = this.End.Evaluate(process);
				try {
					if (jformat == null) {
						end = DateTime.Parse(endString);
					} else {
						format = ConvertJavaFormat(jformat);
						end = DateTime.ParseExact(endString, jformat, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal);
					}
				} catch (FormatException) {
					if (format == null) process.Log(LogLevel.Warning, "In element <interval>: Could not parse '" + endString + "' as a date.");
					else process.Log(LogLevel.Warning, "In element <interval>: Could not parse '" + endString + "' as a date with format '" + jformat + "'.");
					return "unknown";
				}

				// Output the result.
				string unit = this.Style.Evaluate(process);
				switch (unit) {
					case "milliseconds": return ((int) (end - start).TotalMilliseconds).ToString();
					case "seconds"     : return ((int) (end - start).TotalSeconds).ToString();
					case "minutes"     : return ((int) (end - start).TotalMinutes).ToString();
					case "hours"       : return ((int) (end - start).TotalHours).ToString();
					case "days"        : return ((int) (end - start).TotalDays).ToString();
					case "weeks"       : return (((int) (end - start).TotalDays) / 7).ToString();
					case "months"      :
						int interval = (end.Year - start.Year) * 12 + end.Month - start.Month;
						if (end - new DateTime(end.Year, end.Month, 1, 0, 0, 0, end.Kind) <
							start - new DateTime(start.Year, start.Month, 1, 0, 0, 0, start.Kind))
							// end falls earlier in the month than start.
							--interval;
						return interval.ToString();
					case "years"       :
						interval = end.Year - start.Year;
						if (end - new DateTime(end.Year, 1, 1, 0, 0, 0, end.Kind) <
							start - new DateTime(start.Year, 1, 1, 0, 0, 0, start.Kind))
							// end falls earlier in the year than start.
							--interval;
						return interval.ToString();
					default:
						process.Log(LogLevel.Warning, "In element <interval>: The style parameter evaluated to an invalid unit: " + unit);
						return "unknown";
				}
			}

			public static string ConvertJavaFormat(string format) {
				StringBuilder builder = new StringBuilder();
				bool quote = false; char letter; int letterCount; int i = 0;
				while (i < format.Length) {
					char c = format[i];
					if (quote) {
						if (c == '\'') quote = false;
						builder.Append(c);
						++i;
					} else {
						if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) {
							letter = c;
							letterCount = 0;
							for (++i; i < format.Length && format[i] == letter; ++i)
								++letterCount;

							string part;
							switch (letter) {
								case 'G':
									part = new string('g', letterCount);
									break;
								case 'y':
									part = new string('y', letterCount);
									break;
								case 'M': case 'L':
									part = new string('M', letterCount);
									break;
								case 'd':
									if (letterCount == 1) part = "d";
									else part = "dd";
									break;
								case 'E':
									if (letterCount < 4) part = "ddd";
									else part = new string('d', letterCount);
									break;
								case 'a':
									if (letterCount < 4) part = "t";
									else part = "tt";
									break;
								case 'H': case 'h': case 'm': case 's':
									if (letterCount == 1) part = c.ToString();
									else part = new string(c, letterCount);
									break;
								case 'S':
									part = new string('f', letterCount);
									break;
								case 'z': case 'Z': case 'X':
									if (letterCount == 1) part = "z";
									else part = "zzz";
									break;
								case 'Y': case 'w': case 'W': case 'D': case 'F': case 'u': case 'k': case 'K':
									throw new ArgumentException("The '" + c + "' format element is not supported.");
								default: part = ""; break;
							}
							builder.Append(part);
						} else {
							if (c == '\'')
								quote = true;
							else if (c == ':' || c == '/' || c == '\\' || c == '"')
								builder.Append('\\');

							builder.Append(c);
							++i;
						}
					}
				}

				return builder.ToString();
			}

			public static Interval FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection style = null;
				TemplateElementCollection jformat = null;
				TemplateElementCollection start = null;
				TemplateElementCollection end = null;

				attribute = node.Attributes["style"];
				if (attribute != null) style = new TemplateElementCollection(attribute.Value);
				attribute = node.Attributes["jformat"];
				if (attribute != null) jformat = new TemplateElementCollection(attribute.Value);
				attribute = node.Attributes["from"];
				if (attribute != null) start = new TemplateElementCollection(attribute.Value);
				attribute = node.Attributes["to"];
				if (attribute != null) end = new TemplateElementCollection(attribute.Value);

				// Search for properties in elements.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Element) {
						if (node2.Name.Equals("style", StringComparison.InvariantCultureIgnoreCase)) {
							style = TemplateElementCollection.FromXml(node2, loader);
						} else if (node2.Name.Equals("jformat", StringComparison.InvariantCultureIgnoreCase)) {
							jformat = TemplateElementCollection.FromXml(node2, loader);
						} else if (node2.Name.Equals("from", StringComparison.InvariantCultureIgnoreCase)) {
							start = TemplateElementCollection.FromXml(node2, loader);
						} else if (node2.Name.Equals("to", StringComparison.InvariantCultureIgnoreCase)) {
							end = TemplateElementCollection.FromXml(node2, loader);
						}
					}
				}

				return new Interval(jformat, start, end, style);
			}
		}
	}
}
