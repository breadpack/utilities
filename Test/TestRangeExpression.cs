using System;
using System.Linq;
using NUnit.Framework;

namespace BreadPack.Utilities.Test {
    public class TestRangeExpression {
        [Test]
        [TestCase("1..5", 1, true)]
        [TestCase("1..5", 4, true)]
        [TestCase("1..5", 5, true)]
        [TestCase("1..5", 0, false)]
        [TestCase("1..5", 6, false)]
        public void Contains_SingleRange_ReturnsExpectedResult(string pattern, int value, bool expected) {
            var range = new RangeExpression<int>(pattern);
            Assert.That(range.Contains(value), Is.EqualTo(expected));
        }

        [Test]
        [TestCase("1,3,5", 1, true)]
        [TestCase("1,3,5", 2, false)]
        [TestCase("1,3,5", 3, true)]
        [TestCase("1,3,5", 5, true)]
        public void Contains_DiscreteValues_ReturnsExpectedResult(string pattern, int value, bool expected) {
            var range = new RangeExpression<int>(pattern);
            Assert.That(range.Contains(value), Is.EqualTo(expected));
        }

        [Test]
        [TestCase("1..", 0, false)]
        [TestCase("1..", 1, true)]
        [TestCase("1..", 100, true)]
        [TestCase("..5", 4, true)]
        [TestCase("..5", 5, true)]
        [TestCase("..5", 6, false)]
        public void Contains_OpenEndedRange_ReturnsExpectedResult(string pattern, int value, bool expected) {
            var range = new RangeExpression<int>(pattern);
            Assert.That(range.Contains(value), Is.EqualTo(expected));
        }

        [Test]
        [TestCase("1..5,7..10", 1, true)]
        [TestCase("1..5,7..10", 3, true)]
        [TestCase("1..5,7..10", 6, false)]
        [TestCase("1..5,7..10", 7, true)]
        [TestCase("1..5,7..10", 9, true)]
        [TestCase("1..5,7..10", 11, false)]
        public void Contains_MultipleRanges_ReturnsExpectedResult(string pattern, int value, bool expected) {
            var range = new RangeExpression<int>(pattern);
            Assert.That(range.Contains(value), Is.EqualTo(expected));
        }

        [Test]
        [TestCase("2024-01-01..2024-12-31")]
        [TestCase("2024-01-01T00:00:00..2024-12-31T23:59:59")]
        public void Constructor_ValidDateTimePattern_DoesNotThrow(string pattern) {
            Assert.DoesNotThrow(() => new RangeExpression<DateTime>(pattern));
        }

        [Test]
        [TestCase("1.5..5.5")]
        [TestCase("-1.5..5.5")]
        [TestCase("1.5..")]
        public void Constructor_ValidFloatPattern_DoesNotThrow(string pattern) {
            Assert.DoesNotThrow(() => new RangeExpression<double>(pattern));
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Constructor_InvalidPattern_ThrowsArgumentException(string pattern) {
            if(string.IsNullOrEmpty(pattern)) 
                Assert.Throws<ArgumentException>(() => new RangeExpression<int>(pattern));
            else 
                Assert.Throws<ArgumentException>(() => new RangeExpression<int>(pattern));
        }

        [Test]
        [TestCase("invalid")]
        [TestCase("1.5..5")]  // Float pattern for int
        [TestCase("1..5..10")] // Multiple dots
        [TestCase("..")]      // Empty range
        [TestCase("5..1")]    // End less than start
        public void Constructor_InvalidFormatPattern_ThrowsFormatException(string pattern) {
            Assert.Throws<ArgumentException>(() => new RangeExpression<int>(pattern));
        }

        [Test]
        public void CustomParser_ValidInput_UsesCustomLogic() {
            var range = new RangeExpression<int>(
                "A..C",
                parser: s => s switch {
                    "A" => 1,
                    "B" => 2,
                    "C" => 3,
                    _ => throw new ArgumentException("Invalid value")
                }
            );

            Assert.That(range.Contains(1), Is.True);
            Assert.That(range.Contains(2), Is.True);
            Assert.That(range.Contains(4), Is.False);
        }

        [Test]
        public void CustomNextValue_ValidInput_UsesCustomLogic() {
            var range = new RangeExpression<int>(
                "1,3,5",
                nextValueFunc: value => value + 2
            );

            Assert.That(range.Contains(1), Is.True);
            Assert.That(range.Contains(2), Is.False);
            Assert.That(range.Contains(3), Is.True);
            Assert.That(range.Contains(4), Is.False);
            Assert.That(range.Contains(5), Is.True);
        }

        private static IEnumerable<TestCaseData> RandomNumbers {
            get {
                for (var i = 0; i < 1000; ++i) 
                    yield return new TestCaseData(Random.Shared.Next(-1000, 1000));
            }
        }

        [Test, TestCaseSource(nameof(RandomNumbers))]
        public void TestRandomNumberWithinRange(int num) {
            var rangePattern = new RangeExpression<int>("..3,5..9,10..150,-5..-1,900..");
            var isMatch = rangePattern.Contains(num);
            
            var expected = num <= 3 || 
                          (num >= 5 && num <= 9) || 
                          (num >= 10 && num <= 150) || 
                          (num >= -5 && num <= -1) || 
                          num >= 900;
            
            Assert.That(isMatch, Is.EqualTo(expected), 
                $"Number {num} {(expected ? "should" : "should not")} be in range");
        }

        [Test]
        public void DateTime_ValidRanges_ReturnsExpectedResults() {
            var range = new RangeExpression<DateTime>("2024-01-01..2024-12-31");
            
            Assert.Multiple(() => {
                Assert.That(range.Contains(new DateTime(2024, 1, 1)), Is.True);
                Assert.That(range.Contains(new DateTime(2024, 6, 15)), Is.True);
                Assert.That(range.Contains(new DateTime(2024, 12, 31)), Is.True);
                Assert.That(range.Contains(new DateTime(2023, 12, 31)), Is.False);
                Assert.That(range.Contains(new DateTime(2025, 1, 1)), Is.False);
            });
        }

        [Test]
        public void DateTime_OpenEndedRanges_ReturnsExpectedResults() {
            var futureRange = new RangeExpression<DateTime>("2024-01-01..");
            var pastRange = new RangeExpression<DateTime>("..2024-12-31");
            var now = DateTime.Now;
            
            Assert.Multiple(() => {
                Assert.That(futureRange.Contains(new DateTime(2023, 12, 31)), Is.False);
                Assert.That(futureRange.Contains(new DateTime(2024, 1, 1)), Is.True);
                Assert.That(futureRange.Contains(new DateTime(2099, 12, 31)), Is.True);

                Assert.That(pastRange.Contains(new DateTime(2000, 1, 1)), Is.True);
                Assert.That(pastRange.Contains(new DateTime(2024, 12, 31)), Is.True);
                Assert.That(pastRange.Contains(new DateTime(2025, 1, 1)), Is.False);
            });
        }

        [Test]
        public void TimeSpan_ValidRanges_ReturnsExpectedResults() {
            var range = new RangeExpression<TimeSpan>("00:00:00..23:59:59");
            
            Assert.Multiple(() => {
                Assert.That(range.Contains(TimeSpan.Zero), Is.True);
                Assert.That(range.Contains(TimeSpan.FromHours(12)), Is.True);
                Assert.That(range.Contains(TimeSpan.FromHours(24)), Is.False);
                Assert.That(range.Contains(TimeSpan.FromDays(1)), Is.False);
            });
        }

        [Test]
        public void Double_ValidRanges_ReturnsExpectedResults() {
            var range = new RangeExpression<double>("-1.5..1.5,2.0..3.0");
            
            Assert.Multiple(() => {
                Assert.That(range.Contains(-2.0), Is.False);
                Assert.That(range.Contains(-1.5), Is.True);
                Assert.That(range.Contains(-1.0), Is.True);
                Assert.That(range.Contains(0.0), Is.True);
                Assert.That(range.Contains(1.4), Is.True);
                Assert.That(range.Contains(1.5), Is.True);
                Assert.That(range.Contains(1.75), Is.False);
                Assert.That(range.Contains(2.0), Is.True);
                Assert.That(range.Contains(2.5), Is.True);
                Assert.That(range.Contains(3.0), Is.True);
                Assert.That(range.Contains(3.5), Is.False);
            });
        }

        [Test]
        public void Constructor_InvalidDateTimePattern_ThrowsFormatException() {
            Assert.Multiple(() => {
                Assert.Throws<ArgumentException>(() => new RangeExpression<DateTime>("invalid-date"));
                Assert.Throws<ArgumentException>(() => new RangeExpression<DateTime>("2024-13-01..2024-12-31")); // Invalid month
                Assert.Throws<ArgumentException>(() => new RangeExpression<DateTime>("2024-12-32..2025-01-01")); // Invalid day
                Assert.Throws<ArgumentException>(() => new RangeExpression<DateTime>("2024-01-01..2023-12-31")); // End before start
            });
        }

        [Test]
        public void Constructor_InvalidTimeSpanPattern_ThrowsFormatException() {
            Assert.Multiple(() => {
                Assert.Throws<ArgumentException>(() => new RangeExpression<TimeSpan>("invalid-time"));
                Assert.Throws<ArgumentException>(() => new RangeExpression<TimeSpan>("00:60:00..01:00:00")); // Invalid minute
                Assert.Throws<ArgumentException>(() => new RangeExpression<TimeSpan>("00:00:60..00:01:00")); // Invalid second
            });
        }

        [Test]
        public void Constructor_InvalidDoublePattern_ThrowsFormatException() {
            Assert.Multiple(() => {
                Assert.Throws<ArgumentException>(() => new RangeExpression<double>("invalid-number"));
                Assert.Throws<ArgumentException>(() => new RangeExpression<double>("1.2.3..4.5")); // Invalid decimal format
                Assert.Throws<ArgumentException>(() => new RangeExpression<double>("..")); // Empty range
                Assert.Throws<ArgumentException>(() => new RangeExpression<double>("5.0..3.0")); // End less than start
            });
        }

        [Test]
        public void CustomDateTimeFormat_ValidInput_UsesCustomLogic() {
            var range = new RangeExpression<DateTime>(
                "20240101..20241231",
                parser: s => DateTime.ParseExact(s, "yyyyMMdd", null)
            );

            Assert.Multiple(() => {
                Assert.That(range.Contains(new DateTime(2024, 1, 1)), Is.True);
                Assert.That(range.Contains(new DateTime(2024, 6, 15)), Is.True);
                Assert.That(range.Contains(new DateTime(2025, 1, 1)), Is.False);
            });
        }

        [Test]
        public void CustomTimeSpanNextValue_ValidInput_UsesCustomLogic() {
            var range = new RangeExpression<TimeSpan>(
                "00:00,04:00,08:00",
                nextValueFunc: ts => ts.Add(TimeSpan.FromHours(4))
            );

            Assert.Multiple(() => {
                Assert.That(range.Contains(TimeSpan.Zero), Is.True);
                Assert.That(range.Contains(TimeSpan.FromHours(2)), Is.False);
                Assert.That(range.Contains(TimeSpan.FromHours(4)), Is.True);
                Assert.That(range.Contains(TimeSpan.FromHours(6)), Is.False);
                Assert.That(range.Contains(TimeSpan.FromHours(8)), Is.True);
            });
        }
    }
}