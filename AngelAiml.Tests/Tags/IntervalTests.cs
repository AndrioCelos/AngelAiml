using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class IntervalTests {
	[Test]
	public void Parse() {
		var tag = new Interval(new("yyyy-MM-dd"), new("2023-01-01"), new("2023-02-01"), new("days"));
		Assert.Multiple(() => {
			Assert.That(tag.JFormat?.ToString(), Is.EqualTo("yyyy-MM-dd"));
			Assert.That(tag.Start?.ToString(), Is.EqualTo("2023-01-01"));
			Assert.That(tag.End?.ToString(), Is.EqualTo("2023-02-01"));
			Assert.That(tag.Style?.ToString(), Is.EqualTo("days"));
		});
	}

	[Test]
	public void ParseWithDefaultFormat() {
		var tag = new Interval(null, new("2023-01-01"), new("2023-02-01"), new("days"));
		Assert.That(tag.JFormat, Is.Null);
		Assert.Multiple(() => {
			Assert.That(tag.Start?.ToString(), Is.EqualTo("2023-01-01"));
			Assert.That(tag.End?.ToString(), Is.EqualTo("2023-02-01"));
			Assert.That(tag.Style?.ToString(), Is.EqualTo("days"));
		});
	}

	[Test]
	public void EvaluateWithDates_Hours() {
		var tag = new Interval(new("yyyy-MM-dd"), new("2023-01-01"), new("2023-07-01"), new("hours"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("4344"));
	}

	[Test]
	public void EvaluateWithDates_HoursDifferentOffsets() {
		var tag = new Interval(new("yyyy-MM-dd HH:mm:ss zzz"), new("2023-01-01 00:00:00 -05:00"), new("2023-07-01 00:00:00 +10:00"), new("hours"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("4329"));
	}

	[Test]
	public void EvaluateWithDates_Days() {
		var tag = new Interval(new("yyyy-MM-dd"), new("2023-01-01"), new("2023-07-01"), new("days"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("181"));
	}

	[Test]
	public void EvaluateWithDates_Weeks() {
		var tag = new Interval(new("yyyy-MM-dd"), new("2023-01-01"), new("2023-07-01"), new("weeks"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("25"));
	}

	[Test]
	public void EvaluateWithDates_Months() {
		var tag = new Interval(new("yyyy-MM-dd"), new("2023-01-01"), new("2023-07-01"), new("months"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("6"));
	}

	[Test]
	public void EvaluateWithDates_MonthsWrapAround() {
		var tag = new Interval(new("yyyy-MM-dd"), new("2023-01-31"), new("2023-06-30"), new("months"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("4"));
	}

	[Test]
	public void EvaluateWithDates_MonthsWrapAroundDifferentOffsets() {
		var tag = new Interval(new("yyyy-MM-dd HH:mm:ss zzz"), new("2023-05-31 00:00:00 +00:00"), new("2023-06-30 22:00:00 -05:00"), new("months"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("1"));
	}

	[Test]
	public void EvaluateWithDates_NegativeMonths() {
		var tag = new Interval(new("yyyy-MM-dd"), new("2023-06-30"), new("2023-01-31"), new("months"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("-4"));
	}

	[Test]
	public void EvaluateWithDates_Years() {
		var tag = new Interval(new("yyyy-MM-dd"), new("2023-01-01"), new("2024-12-31"), new("years"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("1"));
	}

	[Test]
	public void EvaluateWithDates_YearsWrapAround() {
		var tag = new Interval(new("yyyy-MM-dd"), new("2020-02-29"), new("2021-02-28"), new("years"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("0"));
	}

	[Test]
	public void EvaluateWithDates_YearsWrapAroundWithLeapYear() {
		var tag = new Interval(new("yyyy-MM-dd"), new("2020-03-01"), new("2021-03-01"), new("years"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("1"));
	}

	[Test]
	public void EvaluateWithDatesWithoutYear() {
		var tag = new Interval(new("MMMM dd"), new("November 30"), new("December 25"), new("days"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("25"));
	}

	[Test]
	public void EvaluateWithDates_NegativeYears() {
		var tag = new Interval(new("yyyy-MM-dd"), new("2023-02-28"), new("2020-02-29"), new("years"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("-2"));
	}

	[Test]
	public void EvaluateWithTime_Hours() {
		var tag = new Interval(new("HH:mm:ss.fff"), new("12:00:00.000"), new("18:59:59.999"), new("hours"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("6"));
	}

	[Test]
	public void EvaluateWithTime_Minutes() {
		var tag = new Interval(new("HH:mm:ss.fff"), new("12:00:00.000"), new("18:59:59.999"), new("minutes"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("419"));
	}

	[Test]
	public void EvaluateWithTime_Seconds() {
		var tag = new Interval(new("HH:mm:ss.fff"), new("12:00:00.000"), new("18:59:59.999"), new("seconds"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("25199"));
	}

	[Test]
	public void EvaluateWithTime_Milliseconds() {
		var tag = new Interval(new("HH:mm:ss.fff"), new("12:00:00.000"), new("18:59:59.999"), new("milliseconds"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("25199999"));
	}

	[Test]
	public void EvaluateWithTime_LongMilliseconds() {
		var tag = new Interval(new("yyyy-MM-dd"), new("1970-01-01"), new("2023-08-25"), new("milliseconds"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("1692921600000"));
	}

	[Test]
	public void Evaluate_UnspecifiedFormat() {
		var tag = new Interval(null, new("2023-01-01 12:00"), new("2023-01-02 18:00"), new("minutes"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("1800"));
	}

	[Test]
	public void Evaluate_InvalidDate() {
		var test = new AimlTest();
		var tag = new Interval(new("yyyy-MM-dd"), new("2023-01-01"), new("2023-02-29"), new("days"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess)), Is.EqualTo("unknown"));
	}
}
