using BreadPack.Utilities;

namespace Test;

public class TestRangeExpression {
    [SetUp]
    public void Setup() { }

    private static IEnumerable<TestCaseData> RandomNumbers {
        get {
            for (var i = 0; i < 1000; ++i) yield return new(Random.Shared.Next(-1000, 1000));
        }
    }

    [Test, TestCaseSource(nameof(RandomNumbers))]
    public void TestRandomNumberWithinRange(int num) {
        var rangePattern = new RangeExpression("..3,5..9,10.. 150,-5 ..-1 ,900..");
        for (var i = 0; i < 100; i++) // Test with 100 random numbers
        {
            var isMatch = rangePattern.IsMatch(num);
            if (num is <= 3
                    or >= 5 and <= 9
                    or >= 10 and <= 150
                    or >= -5 and <= -1
                    or >= 900)
                Assert.That(isMatch, Is.True, $"Number {num} is expected to match but didn't.");
            else
                Assert.That(isMatch, Is.False, $"Number {num} is not expected to match but did.");
        }
    }

    [Test, TestCaseSource(nameof(RandomNumbers))]
    public void TestAllNumbers(int num) {
        var rangePattern = new RangeExpression("*");
        var isMatch      = rangePattern.IsMatch(num);
        Assert.That(isMatch, Is.True, $"Number {num} is expected to match but didn't.");
    }

    [Test, TestCaseSource(nameof(RandomNumbers))]
    public void TestSingleNumber(int num) {
        var rangePattern = new RangeExpression("1, 13, 17, 29, 46, -1, -13, -17, -29, -46");
        var isMatch      = rangePattern.IsMatch(num);
        if (num is 1 or 13 or 17 or 29 or 46 or -1 or -13 or -17 or -29 or -46) {
            Assert.That(isMatch, Is.True);
        }
        else {
            Assert.That(isMatch, Is.False);
        }
    }

    [Test]
    public void TestInvalidPattern() {
        Assert.Catch<ArgumentException>(() => new RangeExpression("1..2..3"));
        Assert.Catch<ArgumentException>(() => new RangeExpression("1....3,4"));
        Assert.Catch<ArgumentException>(() => new RangeExpression("abc..d"));
    }
}