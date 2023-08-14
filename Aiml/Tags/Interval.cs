using System.Globalization;
using System.Text;
using System.Xml;

namespace Aiml;
public partial class TemplateNode {
	/// <summary>Returns the amount of time between two dates.</summary>
	/// <remarks>
	///		<para>This element has the following attributes:</para>
	///		<list type="table">
	///			<item>
	///				<term><c>style</c></term>
	///				<description>the unit to return.
	///					Allowed values specified by AIML are <c>minutes</c>, <c>hours</c>, <c>days</c>, <c>weeks</c>, <c>months</c>, and <c>years</c>.
	///					Allowed values in an extension are <c>milliseconds</c> and <c>seconds</c>.</description>
	///			</item>
	///			<item>
	///				<term><c>jformat</c></term>
	///				<description>the format of the <c>from</c> and <c>to</c> attributes using a Java Simple Date Format format string.
	///					See <see href="https://docs.oracle.com/javase/8/docs/api/java/text/SimpleDateFormat.html"/> for more information.
	///					If omitted, .NET will attempt to infer the format.</description>
	///			</item>
	///			<item>
	///				<term><c>from</c></term>
	///				<description>the start date of the interval.</description>
	///			</item>
	///			<item>
	///				<term><c>to</c></term>
	///				<description>the end date of the interval.</description>
	///			</item>
	///		</list>
	///		<para>This element has no content.</para>
	///		<para>This element is defined by the AIML 2.0 specification.</para>
	/// </remarks>
	/// <seealso cref="Date"/>
	public sealed class Interval(TemplateElementCollection jformat, TemplateElementCollection start, TemplateElementCollection end, TemplateElementCollection style) : TemplateNode {
		public TemplateElementCollection JFormat { get; set; } = jformat;
		public TemplateElementCollection Start { get; set; } = start;
		public TemplateElementCollection End { get; set; } = end;
		public TemplateElementCollection Style { get; set; } = style;

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
			var unit = this.Style.Evaluate(process);
			switch (unit) {
				case "milliseconds": return ((int) (end - start).TotalMilliseconds).ToString();
				case "seconds"     : return ((int) (end - start).TotalSeconds).ToString();
				case "minutes"     : return ((int) (end - start).TotalMinutes).ToString();
				case "hours"       : return ((int) (end - start).TotalHours).ToString();
				case "days"        : return ((int) (end - start).TotalDays).ToString();
				case "weeks"       : return ((int) (end - start).TotalDays / 7).ToString();
				case "months"      :
					var interval = (end.Year - start.Year) * 12 + end.Month - start.Month;
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
			var builder = new StringBuilder();
			var quote = false; char letter; int letterCount; var i = 0;
			while (i < format.Length) {
				var c = format[i];
				if (quote) {
					if (c == '\'') quote = false;
					builder.Append(c);
					++i;
				} else {
					if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z') {
						letter = c;
						letterCount = 0;
						for (++i; i < format.Length && format[i] == letter; ++i)
							++letterCount;
						builder.Append(letter switch {
							'G' => new string('g', letterCount),
							'y' => new string('y', letterCount),
							'M' or 'L' => new string('M', letterCount),
							'd' => letterCount == 1 ? "d" : "dd",
							'E' => letterCount < 4 ? "ddd" : new string('d', letterCount),
							'a' => letterCount < 4 ? "t" : "tt",
							'H' or 'h' or 'm' or 's' => letterCount == 1 ? c.ToString() : new string(c, letterCount),
							'S' => new string('f', letterCount),
							'z' or 'Z' or 'X' => letterCount == 1 ? "z" : "zzz",
							'Y' or 'w' or 'W' or 'D' or 'F' or 'u' or 'k' or 'K' => throw new ArgumentException($"The '{c}' format element is not supported."),
							_ => "",
						});
					} else {
						if (c == '\'')
							quote = true;
						else if (c is ':' or '/' or '\\' or '"')
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
