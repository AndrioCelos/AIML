using System;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns the current time.
		/// </summary>
		/// <remarks>
		///     If no format is specified, a system-dependent default format is used.
		///     This element supports four properties:
		///         format
		///             Specifies the format of the output using a format string for the UNIX strftime function.
		///             See http://linux.die.net/man/3/strftime for more information.
		///         jformat
		///             Specifies the format of the output using a format string according to the Java Simple Date Format specification.
		///             See http://docs.oracle.com/javase/1.4.2/docs/api/java/text/SimpleDateFormat.html for more information.
		///             Only one of format and jformat should be present.
		///         locale
		///             Specifies the locale that should be used to format the time as an ISO language/country code pair. The default is en_US.
		///         timezone
		///             Specifies the timezone for which the time should be returned, as a number of hours ahead of UTC.
		///     This element has no content.
		///     This element is defined by the AIML 1.1 specification and extended by the AIML 2.0 specification.
		/// </remarks>
		public sealed class Date : TemplateNode {
			public TemplateElementCollection Format { get; set; }
			public TemplateElementCollection JFormat { get; set; }
			public TemplateElementCollection Locale { get; set; }
			public TemplateElementCollection Timezone { get; set; }

			public Date(TemplateElementCollection format, TemplateElementCollection jformat) : this(format, jformat, null, null) { }
			public Date(TemplateElementCollection format, TemplateElementCollection jformat, TemplateElementCollection locale, TemplateElementCollection timezone) {
				this.Format = format;
				this.JFormat = jformat;
				this.Locale = locale;
				this.Timezone = timezone;
			}

			public override string Evaluate(RequestProcess process) {
				string format = null;
				if (this.Format != null) format = this.Format.Evaluate(process);
				string jformat = null;
				if (this.JFormat != null) jformat = this.JFormat.Evaluate(process);
				string localeString = null;
				if (this.Locale != null) localeString = this.Locale.Evaluate(process);
				string timezone = null;
				if (this.Timezone != null) timezone = this.Timezone.Evaluate(process);

				CultureInfo locale;
				if (localeString == null)
					locale = process.Bot.Config.Locale;
				else {
					try {
						locale = CultureInfo.CreateSpecificCulture(localeString.Replace('_', '-'));
					} catch (CultureNotFoundException) {
						process.Log(LogLevel.Warning, "In element <date>: Locale '" + localeString + "' is unknown.");
						locale = process.Bot.Config.Locale;
					}
				}

				var date = DateTime.Now;
				TimeSpan offset;
				if (timezone != null && TimeSpan.TryParse(timezone, out offset))
					offset = -offset;
				else
					offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);

				if (format != null) {
					if (jformat != null) process.Log(LogLevel.Warning, "In element <date>: format and jformat are both specified. format will be used.");
					return DateUnixFormat(new DateTimeOffset(date, offset), format, locale);
				}

				if (jformat != null)
					return DateJavaFormat(new DateTimeOffset(date, offset), jformat, locale);

				return date.Date.ToString(locale);
			}

			public static string DateUnixFormat(DateTimeOffset date, string format, CultureInfo locale) {
				var formatInfo = locale.DateTimeFormat;

				StringBuilder builder = new StringBuilder();
				bool percent = false; string part;
				for (int i = 0; i < format.Length; ++i) {
					char c = format[i];

					if (percent) {
						switch (c) {
							case 'a': part = formatInfo.GetAbbreviatedDayName(date.DayOfWeek); break;
							case 'A': part = formatInfo.GetDayName(date.DayOfWeek); break;
							case 'b':
							case 'h':
								part = formatInfo.GetAbbreviatedMonthName(date.Month); break;
							case 'B': part = formatInfo.GetMonthName(date.Month); break;
							case 'c': part = date.ToString("g", locale); break;
							case 'C': part = (date.Year / 100).ToString(); break;
							case 'd': part = date.Day.ToString("00"); break;
							case 'D': part = date.ToString("MM\\/dd\\/yy"); break;
							case 'e': part = date.Day.ToString().PadLeft(2); break;
							case 'f': part = date.ToString("yyyy-MM-dd"); break;
							case 'G': part = GetWeekBasedYear(date.DateTime).ToString("0000"); break;
							case 'g': part = (GetWeekBasedYear(date.DateTime) % 100).ToString("00"); break;
							case 'H': part = date.Hour.ToString("00"); break;
							case 'I':
								var hour = date.Hour % 12;
								if (hour == 0) hour = 12;
								part = hour.ToString("00");
								break;
							case 'j': part = date.DayOfYear.ToString("000"); break;
							case 'k': part = date.Hour.ToString().PadLeft(2); break;
							case 'l':
								hour = date.Hour % 12;
								if (hour == 0) hour = 12;
								part = hour.ToString().PadLeft(2);
								break;
							case 'm': part = date.Month.ToString("00"); break;
							case 'M': part = date.Minute.ToString("00"); break;
							case 'n': part = Environment.NewLine; break;
							case 'p': part = (date.Hour >= 12 ? formatInfo.PMDesignator : formatInfo.AMDesignator); break;
							case 'P': part = (date.Hour >= 12 ? formatInfo.PMDesignator : formatInfo.AMDesignator).ToLower(locale); break;
							case 'r': part = date.ToString("hh:mm:ss tt"); break;
							case 'R': part = date.ToString("HH:mm"); break;
							case 's': part = ((int) (date - new DateTime(1970, 1, 1)).TotalSeconds).ToString(); break;
							case 'S': part = date.Second.ToString("00"); break;
							case 't': part = "\t"; break;
							case 'T': part = date.ToString("HH:mm:ss"); break;
							case 'u':
								var day = (int) date.DayOfWeek;
								if (day == 0) day = 7;  // Sunday is 7.
								part = day.ToString();
								break;
							case 'U': part = GetWeekOfCurrentYear(date.DateTime, DayOfWeek.Sunday).ToString("00"); break;
							case 'V': part = new GregorianCalendar().GetWeekOfYear(date.DateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString("00"); break;
								// This may consider a date in the first few days of January as week 53 of the previous year,
								// if the date falls on a week with fewer than four days falling in the current year.
								// %g will also reflect this and return the previous year in this case.
							case 'w': part = ((int) date.DayOfWeek).ToString(); break;
							case 'W': part = GetWeekOfCurrentYear(date.DateTime, DayOfWeek.Monday).ToString("00"); break;
							case 'x': part = date.ToString(formatInfo.ShortDatePattern, locale); break;
							case 'X': part = date.ToString(formatInfo.ShortTimePattern, locale); break;
							case 'y': part = (date.Year % 100).ToString("00"); break;
							case 'Y': part = date.Year.ToString("0000"); break;
							case 'z': part = GetOffsetString(date.Offset, "hhmm"); break;
							case 'Z': part = GetOffsetString(date.Offset, "hhmm"); break;
							case '%': part = "%"; break;
							default: part = "%" + c.ToString(); break;
						}
						percent = false;
						builder.Append(part);
					} else if (c == '%')
						percent = true;
					else
						builder.Append(c);
				}
				return builder.ToString();
			}

			public static string DateJavaFormat(DateTimeOffset date, string format, CultureInfo locale) {
				var formatInfo = locale.DateTimeFormat;

				StringBuilder builder = new StringBuilder();
				bool quote = false; char letter; int letterCount; int i = 0;
				while (i < format.Length) {
					char c = format[i];
					if (quote) {
						if (c == '\'')
							quote = false;
						else
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
									if (letterCount < 4) part = formatInfo.GetAbbreviatedEraName(formatInfo.Calendar.GetEra(date.DateTime));
									else part = formatInfo.GetEraName(formatInfo.Calendar.GetEra(date.DateTime));
									break;
								case 'y':
									if (letterCount == 2) part = (date.Year % 100).ToString("00");
									else part = date.Year.ToString().PadLeft(letterCount, '0');
									break;
								case 'Y':
									if (letterCount == 2) part = (GetWeekBasedYear(date.DateTime) % 100).ToString("00");
									else part = GetWeekBasedYear(date.DateTime).ToString().PadLeft(letterCount, '0');
									break;
								case 'M':
								case 'L':
									if (letterCount >= 4) part = formatInfo.GetMonthName(date.Month);
									else if (letterCount == 3) part = formatInfo.GetAbbreviatedMonthName(date.Month);
									else part = date.Month.ToString().PadLeft(letterCount, '0');
									break;
								case 'w':
									part = formatInfo.Calendar.GetWeekOfYear(date.DateTime, CalendarWeekRule.FirstFourDayWeek, formatInfo.FirstDayOfWeek).ToString().PadLeft(letterCount, '0');
									break;
								case 'W':
									// 'W' gives the calendar week number, where the week containing the first day of the month is week 1.
									var weekNumber = formatInfo.Calendar.GetWeekOfYear(date.DateTime, CalendarWeekRule.FirstDay, formatInfo.FirstDayOfWeek) -
									formatInfo.Calendar.GetWeekOfYear(new DateTime(date.Year, date.Month, 1), CalendarWeekRule.FirstDay, formatInfo.FirstDayOfWeek) + 1;
									part = weekNumber.ToString().PadLeft(letterCount, '0');
									break;
								case 'D':
									part = date.DayOfYear.ToString().PadLeft(letterCount, '0'); break;
								case 'd':
									part = date.Day.ToString().PadLeft(letterCount, '0'); break;
								case 'F':
									// The documentation of 'F' is misleading. 'F' actually returns the ordinal of the instance of the day of the week within the month.
									// For example, 'F' would be 2 for the second Thursday in the month.
									part = ((date.Day + 6) / 7).ToString().PadLeft(letterCount, '0'); break;
								case 'E':
									if (letterCount >= 4) part = formatInfo.GetDayName(date.DayOfWeek);
									else part = formatInfo.GetAbbreviatedDayName(date.DayOfWeek);
									break;
								case 'u':
									var day = (int) date.DayOfWeek;
									if (day == 0) day = 7;
									part = day.ToString().PadLeft(letterCount, '0'); break;
								case 'a':
									if (date.Hour >= 12) part = formatInfo.PMDesignator;
									else part = formatInfo.AMDesignator;
									break;
								case 'H': part = date.Hour.ToString().PadLeft(letterCount, '0'); break;
								case 'k':
									var hour = date.Hour;
									if (hour == 0) hour = 24;
									part = hour.ToString().PadLeft(letterCount, '0'); break;
								case 'K': part = (date.Hour % 12).ToString().PadLeft(letterCount, '0'); break;
								case 'h':
									hour = date.Hour % 12;
									if (hour == 0) hour = 12;
									part = hour.ToString().PadLeft(letterCount, '0'); break;
								case 'm': part = date.Minute.ToString().PadLeft(letterCount, '0'); break;
								case 's': part = date.Second.ToString().PadLeft(letterCount, '0'); break;
								case 'S': part = date.Millisecond.ToString().PadLeft(letterCount, '0'); break;
								case 'z':
								case 'X':
									if (letterCount >= 3) part = GetOffsetString(date.Offset, "hh:mm");
									else if (letterCount == 2) part = GetOffsetString(date.Offset, "hhmm");
									else part = GetOffsetString(date.Offset, "hh");
									break;
								case 'Z': part = GetOffsetString(date.Offset, "hhmm"); break;
								default: part = ""; break;
							}
							builder.Append(part);
						} else {
							if (c == '\'')
								quote = true;
							else
								builder.Append(c);
							++i;
						}
					}
				}

				return builder.ToString();
			}

			/// <summary>
			///     Returns the week-based year according to ISO 8601.
			///     ISO 8601 defines the first week in a year as the first calendar week with four or more days falling in that year.
			///     A date occurring before the first week in the year (such as 2 January 2016) is considered in week 53 of the previous year.
			///     A date occurring after the last week in the year (such as 28 December 2014) is considered in week 1 of the next year.
			/// </summary>
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

			/// <summary>
			///     Returns the week number within the current year, with week 1 starting at the first specified day of the week in the year.
			///     A date falling before the first specified day of the week (such as 1 January 2016 if firstDayOfWeek is not Friday) is considered in week 0.
			/// </summary>
			private static int GetWeekOfCurrentYear(DateTime date, DayOfWeek firstDayOfWeek) {
				var week = new GregorianCalendar().GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, firstDayOfWeek);
				if (week >= 52 && date.Month == 1) return 0;  // A date before the first full week of the year.
				return week;
			}

			private static string GetOffsetString(TimeSpan offset, string format) {
				if (offset < TimeSpan.Zero) return "-" + offset.ToString(format);
				if (offset == TimeSpan.Zero) return " " + offset.ToString(format);
				return "+" + offset.ToString(format);
			}

			public static TemplateNode.Date FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection format = null;
				TemplateElementCollection jformat = null;
				TemplateElementCollection locale = null;
				TemplateElementCollection timezone = null;

				attribute = node.Attributes["format"];
				if (attribute != null) format = new TemplateElementCollection(attribute.Value);
				attribute = node.Attributes["jformat"];
				if (attribute != null) jformat = new TemplateElementCollection(attribute.Value);
				attribute = node.Attributes["locale"];
				if (attribute != null) locale = new TemplateElementCollection(attribute.Value);
				attribute = node.Attributes["timezone"];
				if (attribute != null) timezone = new TemplateElementCollection(attribute.Value);

				// Search for properties in elements.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Element) {
						if (node2.Name.Equals("format", StringComparison.InvariantCultureIgnoreCase)) {
							format = TemplateElementCollection.FromXml(node2, loader);
						} else if (node2.Name.Equals("jformat", StringComparison.InvariantCultureIgnoreCase)) {
							jformat = TemplateElementCollection.FromXml(node2, loader);
						} else if (node2.Name.Equals("locale", StringComparison.InvariantCultureIgnoreCase)) {
							locale = TemplateElementCollection.FromXml(node2, loader);
						} else if (node2.Name.Equals("timezone", StringComparison.InvariantCultureIgnoreCase)) {
							timezone = TemplateElementCollection.FromXml(node2, loader);
						}
					}
				}

				return new Date(format, jformat, locale, timezone);
			}
		}
	}
}
