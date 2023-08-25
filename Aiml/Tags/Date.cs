using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Aiml.Tags;
/// <summary>Returns the current time.</summary>
/// <remarks>
///		<para>This element has the following attributes:</para>
///		<list type="table">
///			<item>
///				<term><c>format</c></term>
///				<description>the format of the output using a format string for the UNIX <c>strftime</c> function.
///					See <see href="https://linux.die.net/man/3/strftime"/> for more information.</description>
///			</item>
///			<item>
///				<term><c>jformat</c></term>
///				<description>the format of the output using a Java Simple Date Format format string.
///					See <see href="https://docs.oracle.com/en/java/javase/20/docs/api/java.base/java/text/SimpleDateFormat.html"/> for more information.</description>
///			</item>
///			<item>
///				<term><c>nformat</c></term>
///				<description>the format of the output using a .NET format string.
///					See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset.tostring?view=net-6.0#system-datetimeoffset-tostring(system-string-system-iformatprovider)"/> for more information.</description>
///			</item>
///			<item>
///				<term><c>locale</c></term>
///				<description>the locale that should be used to format the time as an ISO language/country code pair.
///					If omitted, the bot's configured culture is used.</description>
///			</item>
///			<item>
///				<term><c>timezone</c></term>
///				<description>the time zone offset for which the time should be returned, as an integer number of hours behind UTC.
///					If omitted, the local time zone is used.</description>
///			</item>
///		</list>
///		<para>Up to one of <c>format</c>, <c>jformat</c> or <c>nformat</c> should be specified. If none are specified, system-dependent default format is used.</para>
///		<para>This element has no other content.</para>
///		<para>This element is defined by the AIML 1.1 specification and extended by the AIML 2.0 specification. The <c>nformat</c> attribute is an extension.</para>
/// </remarks>
/// <seealso cref="Interval"/>
public sealed class Date : TemplateNode {
	public TemplateElementCollection? Format { get; set; }
	public TemplateElementCollection? JFormat { get; set; }
	public TemplateElementCollection? NFormat { get; set; }
	public TemplateElementCollection? Locale { get; set; }
	public TemplateElementCollection? Timezone { get; set; }

	internal static DateTime? mockDateTime;

	public Date(TemplateElementCollection? format, TemplateElementCollection? jformat, TemplateElementCollection? nformat, TemplateElementCollection? locale, TemplateElementCollection? timezone) {
		this.Format = format;
		this.JFormat = jformat;
		this.NFormat = nformat;
		this.Locale = locale;
		this.Timezone = timezone;
		if ((format is not null && (jformat is not null || nformat is not null)) || (jformat is not null && nformat is not null))
			throw new ArgumentException("<date> element cannot have multiple format attributes.");
	}

	public override string Evaluate(RequestProcess process) {
		var format = this.Format?.Evaluate(process);
		var jformat = this.JFormat?.Evaluate(process);
		var nformat = this.NFormat?.Evaluate(process);
		var localeString = this.Locale?.Evaluate(process);
		var timezone = this.Timezone?.Evaluate(process);

		CultureInfo locale;
		if (localeString == null)
			locale = process.Bot.Config.Locale;
		else {
			try {
				locale = CultureInfo.GetCultureInfo(localeString.Replace('_', '-'));
			} catch (CultureNotFoundException) {
				process.Log(LogLevel.Warning, $"In element <date>: Locale '{localeString}' is unknown.");
				locale = process.Bot.Config.Locale;
			}
		}

		var dateTime = mockDateTime ?? DateTime.Now;
		var offset = timezone is not null && TryParseOffset(timezone, out var offset2) ? -offset2 : TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
		var dateTimeOffset = new DateTimeOffset(dateTime).ToOffset(offset);

		return format is not null ? DateUnixFormat(dateTimeOffset, timezone is not null, format, locale)
			: jformat is not null ? DateJavaFormat(dateTimeOffset, timezone is not null, jformat, locale)
			: nformat is not null ? dateTimeOffset.ToString(nformat, locale)
			: dateTimeOffset.Date.ToString(locale);
	}

	private static readonly Regex offsetRegex = new(@"^\s*(?:([-+])\s*)?(\d+)\s*(?::\s*(\d+)\s*)?$", RegexOptions.Compiled);
	private static bool TryParseOffset(string offsetStr, out TimeSpan offset) {
		var match = offsetRegex.Match(offsetStr);
		if (!match.Success) {
			offset = default;
			return false;
		}
		var h = int.Parse(match.Groups[2].Value);
		var m = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
		if (h is < -14 or > 14 || (h is -14 or 14 && m != 0) || m is < 0 or >= 60) {
			offset = default;
			return false;
		}
		offset = TimeSpan.FromTicks((h * TimeSpan.TicksPerHour + m * TimeSpan.TicksPerMinute) * (match.Groups[1].Value == "-" ? -1 : 1));
		return true;
	}

	public static string DateUnixFormat(DateTimeOffset date, bool specifiedOffset, string format, CultureInfo locale) {
		var formatInfo = locale.DateTimeFormat;

		var builder = new StringBuilder();
		var percent = false; string part;
		for (var i = 0; i < format.Length; i++) {
			var c = format[i];

			if (percent) {
				part = c switch {
					'a' => formatInfo.GetAbbreviatedDayName(date.DayOfWeek),
					'A' => formatInfo.GetDayName(date.DayOfWeek),
					'b' or 'h' => formatInfo.GetAbbreviatedMonthName(date.Month),
					'B' => formatInfo.GetMonthName(date.Month),
					'c' => date.ToString("g", locale),
					'C' => (date.Year / 100).ToString(),
					'd' => date.Day.ToString("00"),
					'D' => date.ToString("MM\\/dd\\/yy"),
					'e' => date.Day.ToString().PadLeft(2),
					'F' => date.ToString("yyyy-MM-dd"),
					'G' => GetWeekBasedYear(date.DateTime).ToString("0000"),
					'g' => (GetWeekBasedYear(date.DateTime) % 100).ToString("00"),
					'H' => date.Hour.ToString("00"),
					'I' => (date.Hour % 12 == 0 ? 12 : date.Hour % 12).ToString("00"),
					'j' => date.DayOfYear.ToString("000"),
					'k' => date.Hour.ToString().PadLeft(2),
					'l' => (date.Hour % 12 == 0 ? 12 : date.Hour % 12).ToString().PadLeft(2),
					'm' => date.Month.ToString("00"),
					'M' => date.Minute.ToString("00"),
					'n' => "\n",
					'p' => date.Hour >= 12 ? formatInfo.PMDesignator : formatInfo.AMDesignator,
					'P' => (date.Hour >= 12 ? formatInfo.PMDesignator : formatInfo.AMDesignator).ToLower(locale),
					'r' => date.ToString("hh:mm:ss tt"),
					'R' => date.ToString("HH:mm"),
					's' => ((int) (date.UtcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString(),
					'S' => date.Second.ToString("00"),
					't' => "\t",
					'T' => date.ToString("HH:mm:ss"),
					'u' => date.DayOfWeek == DayOfWeek.Sunday ? "7" : ((int) date.DayOfWeek).ToString(),
					'U' => GetWeekOfCurrentYear(date.DateTime, DayOfWeek.Sunday).ToString("00"),
					'V' => new GregorianCalendar().GetWeekOfYear(date.DateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString("00"),
					// This may consider a date in the first few days of January as week 53 of the previous year,
					// if the date falls on a week with fewer than four days falling in the current year.
					// %g will also reflect this and return the previous year in this
					'w' => ((int) date.DayOfWeek).ToString(),
					'W' => GetWeekOfCurrentYear(date.DateTime, DayOfWeek.Monday).ToString("00"),
					'x' => date.ToString(formatInfo.ShortDatePattern, locale),
					'X' => date.ToString(formatInfo.LongTimePattern, locale),
					'y' => (date.Year % 100).ToString("00"),
					'Y' => date.Year.ToString("0000"),
					'z' => GetOffsetString(date.Offset, "hhmm"),
					'Z' => date.Offset == TimeSpan.Zero ? "UTC" : specifiedOffset ? GetOffsetString(date.Offset, "hhmm") : GetCurrentTimeZoneName(),
					'+' => date.ToString($"ddd MMM d HH:mm:ss {(date.Offset == TimeSpan.Zero ? "'UTC'" : "zzz")} yyyy"),
					'%' => "%",
					_ => $"%{c}"
				};
				percent = false;
				builder.Append(part);
			} else if (c == '%')
				percent = true;
			else
				builder.Append(c);
		}
		return builder.ToString();
	}

	private static string GetCurrentTimeZoneName() {
		var zone = TimeZoneInfo.Local;
		return zone.SupportsDaylightSavingTime && zone.IsDaylightSavingTime(DateTime.UtcNow) ? zone.DaylightName : zone.StandardName;
	}

	public static string DateJavaFormat(DateTimeOffset date, bool specifiedOffset, string format, CultureInfo locale) {
		var formatInfo = locale.DateTimeFormat;

		var builder = new StringBuilder();
		var quote = false; char letter; int letterCount; var i = 0;
		while (i < format.Length) {
			var c = format[i];
			if (quote) {
				if (c == '\'')
					quote = false;
				else
					builder.Append(c);
				i++;
			} else {
				if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z') {
					letter = c;
					letterCount = 1;
					for (++i; i < format.Length && format[i] == letter; i++)
						letterCount++;

					var part = letter switch {
						'G' => letterCount < 4
							? formatInfo.GetAbbreviatedEraName(formatInfo.Calendar.GetEra(date.DateTime))
							: formatInfo.GetEraName(formatInfo.Calendar.GetEra(date.DateTime)),
						'y' => letterCount == 2
							? (date.Year % 100).ToString("00")
							: date.Year.ToString().PadLeft(letterCount, '0'),
						'Y' => letterCount == 2
							? (GetWeekBasedYear(date.DateTime) % 100).ToString("00")
							: GetWeekBasedYear(date.DateTime).ToString().PadLeft(letterCount, '0'),
						'M' or 'L' => letterCount switch {
							>= 4 => formatInfo.GetMonthName(date.Month),
							3 => formatInfo.GetAbbreviatedMonthName(date.Month),
							_ => date.Month.ToString().PadLeft(letterCount, '0')
						},
						'w' => formatInfo.Calendar.GetWeekOfYear(date.DateTime, CalendarWeekRule.FirstDay, formatInfo.FirstDayOfWeek).ToString().PadLeft(letterCount, '0'),
						'W' =>
							// 'W' gives the calendar week number within the month, where the week containing the first day of the month is week 1.
							(formatInfo.Calendar.GetWeekOfYear(date.DateTime, CalendarWeekRule.FirstDay, formatInfo.FirstDayOfWeek)
								- formatInfo.Calendar.GetWeekOfYear(new DateTime(date.Year, date.Month, 1), CalendarWeekRule.FirstDay, formatInfo.FirstDayOfWeek)
								+ 1).ToString().PadLeft(letterCount, '0'),
						'D' => date.DayOfYear.ToString().PadLeft(letterCount, '0'),
						'd' => date.Day.ToString().PadLeft(letterCount, '0'),
						'F' => ((date.Day + 6) / 7).ToString().PadLeft(letterCount, '0'),
						// The documentation of 'F' is misleading. 'F' actually returns the number of the instance of the day of the week within the month.
						// For example, 'F' would be 2 for the second Thursday in the month.
						'E' => letterCount >= 4 ? formatInfo.GetDayName(date.DayOfWeek) : formatInfo.GetAbbreviatedDayName(date.DayOfWeek),
						'u' => (date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int) date.DayOfWeek).ToString().PadLeft(letterCount, '0'),
						'a' => date.Hour >= 12 ? formatInfo.PMDesignator : formatInfo.AMDesignator,
						'H' => date.Hour.ToString().PadLeft(letterCount, '0'),
						'k' => (date.Hour == 0 ? 24 : date.Hour).ToString().PadLeft(letterCount, '0'),
						'K' => (date.Hour % 12).ToString().PadLeft(letterCount, '0'),
						'h' => (date.Hour % 12 == 0 ? 12 : date.Hour % 12).ToString().PadLeft(letterCount, '0'),
						'm' => date.Minute.ToString().PadLeft(letterCount, '0'),
						's' => date.Second.ToString().PadLeft(letterCount, '0'),
						'S' => date.Millisecond.ToString().PadLeft(letterCount, '0'),
						'z' => date.Offset == TimeSpan.Zero ? "UTC" : specifiedOffset ? GetOffsetString(date.Offset, "hhmm") : GetCurrentTimeZoneName(),
						'X' => letterCount switch {
							>= 3 => GetOffsetString(date.Offset, @"hh\:mm"),
							2 => GetOffsetString(date.Offset, "hhmm"),
							_ => GetOffsetString(date.Offset, "hh")
						},
						'Z' => GetOffsetString(date.Offset, "hhmm"),
						_ => ""
					};
					builder.Append(part);
				} else {
					if (c == '\'') {
						if (format.ElementAtOrDefault(i + 1) == '\'') {
							builder.Append('\'');
							i++;
						} else
							quote = true;
					} else
						builder.Append(c);
					i++;
				}
			}
		}

		return builder.ToString();
	}

	/// <summary>Returns the week-based year according to ISO 8601.</summary>
	/// <remarks>
	/// ISO 8601 defines the first week in a year as the first calendar week with four or more days falling in that year.
	/// A date occurring before the first week in the year (such as 2 January 2016) is considered in week 53 of the previous year.
	/// A date occurring after the last week in the year (such as 28 December 2014) is considered in week 1 of the next year.
	/// </remarks>
	private static int GetWeekBasedYear(DateTime date) {
		var week = new GregorianCalendar().GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
		if (week == 53 && date.Month == 1)
			// Last week of the previous year.
			return date.Year - 1;
		else if (week == 1 && date.Month != 1)
			// First week of the next year.
			return date.Year + 1;
		else
			return date.Year;
	}

	/// <summary>Returns the week number within the current year, with week 1 starting at the first specified day of the week in the year.</summary>
	/// <remarks>A date falling before the first specified day of the week (such as 1 January 2016 if firstDayOfWeek is not Friday) is considered in week 0.</remarks>
	private static int GetWeekOfCurrentYear(DateTime date, DayOfWeek firstDayOfWeek) {
		var week = new GregorianCalendar().GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, firstDayOfWeek);
		if (week >= 52 && date.Month == 1) return 0;  // A date before the first full week of the year.
		return week;
	}

	private static string GetOffsetString(TimeSpan offset, string format) => $"{(offset < TimeSpan.Zero ? '-' : '+')}{offset.ToString(format)}";
}
