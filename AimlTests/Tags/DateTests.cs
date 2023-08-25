using Aiml.Tags;

namespace Aiml.Tests.Tags;
[TestFixture]
public class DateTests {
	[Test]
	public void ParseWithFormatFull() {
		var tag = new Date(format: new("%F"), jformat: null, nformat: null, locale: new("en-AU"), timezone: new("0"));
		Assert.AreEqual("%F", tag.Format?.ToString());
		Assert.AreEqual("en-AU", tag.Locale?.ToString());
		Assert.AreEqual("0", tag.Timezone?.ToString());
	}

	[Test]
	public void ParseWithJFormatFull() {
		var tag = new Date(format: null, jformat: new("yyyy-MM-dd"), nformat: null, locale: new("en-AU"), timezone: new("0"));
		Assert.AreEqual("yyyy-MM-dd", tag.JFormat?.ToString());
		Assert.AreEqual("en-AU", tag.Locale?.ToString());
		Assert.AreEqual("0", tag.Timezone?.ToString());
	}

	[Test]
	public void ParseWithFormat() {
		var tag = new Date(format: new("%F"), jformat: null, nformat: null, locale: null, timezone: null);
		Assert.AreEqual("%F", tag.Format?.ToString());
		Assert.IsNull(tag.Locale);
		Assert.IsNull(tag.Timezone);
	}

	[Test]
	public void ParseWithJFormat() {
		var tag = new Date(format: null, jformat: new("yyyy-MM-dd"), nformat: null, locale: null, timezone: null);
		Assert.AreEqual("yyyy-MM-dd", tag.JFormat?.ToString());
		Assert.IsNull(tag.Locale);
		Assert.IsNull(tag.Timezone);
	}

	[Test]
	public void ParseWithDefaults() {
		var tag = new Date(format: null, jformat: null, nformat: null, locale: null, timezone: null);
		Assert.IsNull(tag.Format);
		Assert.IsNull(tag.Locale);
		Assert.IsNull(tag.Timezone);
	}

	[Test]
	public void EvaluateWithBasicFormat() {
		var process = new AimlTest().RequestProcess;
		Date.mockDateTime = new(2010, 1, 2, 0, 15, 30, DateTimeKind.Utc);
		Assert.AreEqual("2010-01-02 00:15:30", new Date(new("%Y-%m-%d %H:%M:%S"), null, null, new("en-AU"), new("0")).Evaluate(process).ToString());
	}

	[TestCase("%a", ExpectedResult = "Sat", TestName = "UNIX format specifier %a")]
	[TestCase("%A", ExpectedResult = "Saturday", TestName = "UNIX format specifier %A")]
	[TestCase("%b", ExpectedResult = "Jan", TestName = "UNIX format specifier %b")]
	[TestCase("%B", ExpectedResult = "January", TestName = "UNIX format specifier %B")]
	[TestCase("%c", ExpectedResult = "2/1/2010 12:15 am", TestName = "UNIX format specifier %c")]
	[TestCase("%C", ExpectedResult = "20", TestName = "UNIX format specifier %C")]
	[TestCase("%d", ExpectedResult = "02", TestName = "UNIX format specifier %d")]
	[TestCase("%D", ExpectedResult = "01/02/10", TestName = "UNIX format specifier %D")]
	[TestCase("%e", ExpectedResult = " 2", TestName = "UNIX format specifier %e")]
	[TestCase("%F", ExpectedResult = "2010-01-02", TestName = "UNIX format specifier %F")]
	[TestCase("%G", ExpectedResult = "2009", TestName = "UNIX format specifier %G")]
	[TestCase("%g", ExpectedResult = "09", TestName = "UNIX format specifier %g")]
	[TestCase("%h", ExpectedResult = "Jan", TestName = "UNIX format specifier %h")]
	[TestCase("%H", ExpectedResult = "00", TestName = "UNIX format specifier %H")]
	[TestCase("%I", ExpectedResult = "12", TestName = "UNIX format specifier %I")]
	[TestCase("%j", ExpectedResult = "002", TestName = "UNIX format specifier %j")]
	[TestCase("%k", ExpectedResult = " 0", TestName = "UNIX format specifier %k")]
	[TestCase("%l", ExpectedResult = "12", TestName = "UNIX format specifier %l")]
	[TestCase("%m", ExpectedResult = "01", TestName = "UNIX format specifier %m")]
	[TestCase("%M", ExpectedResult = "15", TestName = "UNIX format specifier %M")]
	[TestCase("%n", ExpectedResult = "\n", TestName = "UNIX format specifier %n")]
	[TestCase("%p", ExpectedResult = "am", TestName = "UNIX format specifier %p")]
	[TestCase("%P", ExpectedResult = "am", TestName = "UNIX format specifier %P")]
	[TestCase("%r", ExpectedResult = "12:15:30 AM", TestName = "UNIX format specifier %r")]
	[TestCase("%R", ExpectedResult = "00:15", TestName = "UNIX format specifier %R")]
	[TestCase("%s", ExpectedResult = "1262391330", TestName = "UNIX format specifier %s")]
	[TestCase("%S", ExpectedResult = "30", TestName = "UNIX format specifier %S")]
	[TestCase("%t", ExpectedResult = "\t", TestName = "UNIX format specifier %t")]
	[TestCase("%T", ExpectedResult = "00:15:30", TestName = "UNIX format specifier %T")]
	[TestCase("%u", ExpectedResult = "6", TestName = "UNIX format specifier %u")]
	[TestCase("%U", ExpectedResult = "00", TestName = "UNIX format specifier %U")]
	[TestCase("%V", ExpectedResult = "53", TestName = "UNIX format specifier %V")]
	[TestCase("%w", ExpectedResult = "6", TestName = "UNIX format specifier %w")]
	[TestCase("%W", ExpectedResult = "00", TestName = "UNIX format specifier %W")]
	[TestCase("%x", ExpectedResult = "2/1/2010", TestName = "UNIX format specifier %x")]
	[TestCase("%X", ExpectedResult = "12:15:30 am", TestName = "UNIX format specifier %X")]
	[TestCase("%y", ExpectedResult = "10", TestName = "UNIX format specifier %y")]
	[TestCase("%Y", ExpectedResult = "2010", TestName = "UNIX format specifier %Y")]
	[TestCase("%z", ExpectedResult = "+0000", TestName = "UNIX format specifier %z")]
	[TestCase("%Z", ExpectedResult = "UTC", TestName = "UNIX format specifier %Z")]
	[TestCase("%+", ExpectedResult = "Sat Jan 2 00:15:30 UTC 2010", TestName = "UNIX format specifier %+")]
	[TestCase("%%", ExpectedResult = "%", TestName = "UNIX format specifier %%")]
	[TestCase("%?", ExpectedResult = "%?", TestName = "UNIX format specifier - unknown")]
	public string EvaluateFormatSpecifier(string format) {
		var process = new AimlTest().RequestProcess;
		Date.mockDateTime = new(2010, 1, 2, 0, 15, 30, DateTimeKind.Utc);
		return new Date(new(format), null, null, new("en-AU"), new("0")).Evaluate(process).ToString();
	}

	[Test]
	public void EvaluateWithBasicJFormat() {
		var process = new AimlTest().RequestProcess;
		Date.mockDateTime = new(2010, 1, 2, 0, 15, 30, 789, DateTimeKind.Utc);
		Assert.AreEqual("2010-01-02 00:15:30", new Date(null, new("yyyy-MM-dd HH:mm:ss"), null, new("en-AU"), new("0")).Evaluate(process).ToString());
	}

	[Test]
	public void EvaluateWithQuotedJFormat() {
		var process = new AimlTest().RequestProcess;
		Date.mockDateTime = new(2010, 1, 2, 0, 15, 30, 789, DateTimeKind.Utc);
		Assert.AreEqual("'2010-01-02T00:15:30Z'", new Date(null, new("''yyyy-MM-dd'T'HH:mm:ss'Z'''"), null, new("en-AU"), new("0")).Evaluate(process).ToString());
	}

	[TestCase("G", ExpectedResult = "AD", TestName = "Java format specifier G")]
	[TestCase("yy", ExpectedResult = "10", TestName = "Java format specifier yy")]
	[TestCase("yyyy", ExpectedResult = "2010", TestName = "Java format specifier yyyy")]
	[TestCase("YY", ExpectedResult = "09", TestName = "Java format specifier YY")]
	[TestCase("YYYY", ExpectedResult = "2009", TestName = "Java format specifier YYYY")]
	[TestCase("M", ExpectedResult = "1", TestName = "Java format specifier M")]
	[TestCase("MM", ExpectedResult = "01", TestName = "Java format specifier MM")]
	[TestCase("MMM", ExpectedResult = "Jan", TestName = "Java format specifier MMM")]
	[TestCase("MMMM", ExpectedResult = "January", TestName = "Java format specifier MMMM")]
	[TestCase("L", ExpectedResult = "1", TestName = "Java format specifier L")]
	[TestCase("LL", ExpectedResult = "01", TestName = "Java format specifier LL")]
	[TestCase("LLL", ExpectedResult = "Jan", TestName = "Java format specifier LLL")]
	[TestCase("LLLL", ExpectedResult = "January", TestName = "Java format specifier LLLL")]
	[TestCase("w", ExpectedResult = "1", TestName = "Java format specifier w")]
	[TestCase("W", ExpectedResult = "1", TestName = "Java format specifier W")]
	[TestCase("DDD", ExpectedResult = "002", TestName = "Java format specifier DDD")]
	[TestCase("dd", ExpectedResult = "02", TestName = "Java format specifier dd")]
	[TestCase("F", ExpectedResult = "1", TestName = "Java format specifier F")]
	[TestCase("E", ExpectedResult = "Sat", TestName = "Java format specifier E")]
	[TestCase("EEEE", ExpectedResult = "Saturday", TestName = "Java format specifier EEEE")]
	[TestCase("u", ExpectedResult = "6", TestName = "Java format specifier u")]
	[TestCase("a", ExpectedResult = "am", TestName = "Java format specifier a")]
	[TestCase("H", ExpectedResult = "0", TestName = "Java format specifier H")]
	[TestCase("k", ExpectedResult = "24", TestName = "Java format specifier k")]
	[TestCase("K", ExpectedResult = "0", TestName = "Java format specifier K")]
	[TestCase("h", ExpectedResult = "12", TestName = "Java format specifier h")]
	[TestCase("m", ExpectedResult = "15", TestName = "Java format specifier m")]
	[TestCase("s", ExpectedResult = "30", TestName = "Java format specifier s")]
	[TestCase("S", ExpectedResult = "789", TestName = "Java format specifier S")]
	[TestCase("z", ExpectedResult = "UTC", TestName = "Java format specifier z")]
	[TestCase("Z", ExpectedResult = "+0000", TestName = "Java format specifier Z")]
	[TestCase("X", ExpectedResult = "+00", TestName = "Java format specifier X")]
	[TestCase("XX", ExpectedResult = "+0000", TestName = "Java format specifier XX")]
	[TestCase("XXX", ExpectedResult = "+00:00", TestName = "Java format specifier XXX")]
	public string EvaluateJFormatSpecifier(string format) {
		var process = new AimlTest().RequestProcess;
		Date.mockDateTime = new(2010, 1, 2, 0, 15, 30, 789, DateTimeKind.Utc);
		return new Date(null, new(format), null, new("en-AU"), new("0")).Evaluate(process).ToString();
	}

	[Test]
	public void EvaluateWithTimeZone() {
		var process = new AimlTest().RequestProcess;
		Date.mockDateTime = new(2010, 1, 2, 0, 15, 30, 789, DateTimeKind.Utc);
		Assert.AreEqual("2010-01-01 19:15:30", new Date(null, new("yyyy-MM-dd HH:mm:ss"), null, new("en-AU"), new("+5")).Evaluate(process).ToString());
	}

	[Test]
	public void EvaluateWithTimeZoneWithMinutes() {
		var process = new AimlTest().RequestProcess;
		Date.mockDateTime = new(2010, 1, 2, 0, 15, 30, 789, DateTimeKind.Utc);
		Assert.AreEqual("2010-01-02 14:00:30", new Date(null, new("yyyy-MM-dd HH:mm:ss"), null, new("en-AU"), new("-13:45")).Evaluate(process).ToString());
	}

	[Test]
	public void EvaluateWithTimeZoneWithWhitespace() {
		var process = new AimlTest().RequestProcess;
		Date.mockDateTime = new(2010, 1, 2, 0, 15, 30, 789, DateTimeKind.Utc);
		Assert.AreEqual("2010-01-02 14:00:30", new Date(null, new("yyyy-MM-dd HH:mm:ss"), null, new("en-AU"), new(" - 13\n\t: 45 ")).Evaluate(process).ToString());
	}
}
