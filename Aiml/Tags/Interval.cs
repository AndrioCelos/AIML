using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Aiml.Tags;
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
///					See <see href="https://docs.oracle.com/en/java/javase/20/docs/api/java.base/java/text/SimpleDateFormat.html"/> for more information.
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
///		<para>Daylight saving time and leap seconds are ignored.</para>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="Date"/>
public sealed class Interval(TemplateElementCollection? jformat, TemplateElementCollection start, TemplateElementCollection end, TemplateElementCollection style) : TemplateNode {
	public TemplateElementCollection? JFormat { get; set; } = jformat;
	public TemplateElementCollection Start { get; set; } = start;
	public TemplateElementCollection End { get; set; } = end;
	public TemplateElementCollection Style { get; set; } = style;

	public override string Evaluate(RequestProcess process) {
		var jformat = this.JFormat?.Evaluate(process);

		// Parse the dates.
		if (!TryParseDate(process, this.Start.Evaluate(process), jformat, out var start)
			|| !TryParseDate(process, this.End.Evaluate(process), jformat, out var end))
			return "unknown";

		// Output the result.
		var unit = this.Style.Evaluate(process);
		switch (unit) {
			case "milliseconds": return ((long) (end - start).TotalMilliseconds).ToString();
			case "seconds"     : return ((long) (end - start).TotalSeconds).ToString();
			case "minutes"     : return ((long) (end - start).TotalMinutes).ToString();
			case "hours"       : return ((long) (end - start).TotalHours).ToString();
			case "days"        : return ((long) (end - start).TotalDays).ToString();
			case "weeks"       : return ((long) (end - start).TotalDays / 7).ToString();
			case "months"      : {
				var interval = (end.Year - start.Year) * 12 + end.Month - start.Month;
				if (end >= start) {
					if (IsEndEarlierInMonth(start.DateTime, end.ToOffset(start.Offset).DateTime)) interval--;
				} else {
					if (IsEndEarlierInMonth(end.DateTime, start.ToOffset(end.Offset).DateTime)) interval++;
				}
				return interval.ToString();
			}
			case "years"       : {
				var interval = end.Year - start.Year;
				if (end >= start) {
					if (IsEndEarlierInYear(start.DateTime, end.ToOffset(start.Offset).DateTime)) interval--;
				} else {
					if (IsEndEarlierInYear(end.DateTime, start.ToOffset(end.Offset).DateTime)) interval++;
				}
				return interval.ToString();
			}
			default:
				process.Log(LogLevel.Warning, $"In element <interval>: 'style' attribute was invalid: {unit}");
				return "unknown";
		}
	}

	private static bool IsEndEarlierInMonth(DateTime start, DateTime end)
		=> end.Day < start.Day || (end.Day == start.Day && (end.Ticks % TimeSpan.TicksPerDay) < (start.Ticks % TimeSpan.TicksPerDay));
	private static bool IsEndEarlierInYear(DateTime start, DateTime end)
		=> end.Month < start.Month
			|| (end.Month == start.Month && (end.Day < start.Day
				|| (end.Day == start.Day && (end.Ticks % TimeSpan.TicksPerDay) < (start.Ticks % TimeSpan.TicksPerDay))));

	private static bool TryParseDate(RequestProcess process, string s, string? jformat, out DateTimeOffset dateTimeOffset) {
		try {
			if (jformat is not null) {
				var format = ConvertJavaFormat(jformat);
				dateTimeOffset = DateTime.ParseExact(s, jformat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
			} else
				dateTimeOffset = DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal);
			return true;
		} catch (FormatException) {
			if (jformat is not null)
				process.Log(LogLevel.Warning, $"In element <interval>: Could not parse '{s}' as a date with format '{jformat}'.");
			else
				process.Log(LogLevel.Warning, $"In element <interval>: Could not parse '{s}' as a date.");
			dateTimeOffset = default;
			return false;
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
}
