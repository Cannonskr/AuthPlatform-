using Auth.Domain.Common;
using FluentAssertions;

namespace Auth.Domain.Tests;

public class ValueObjectTests
{
    private class TestValueObject : ValueObject
    {
        public string Name { get; }
        public int Value { get; }

        public TestValueObject(string name, int value)
        {
            Name = name;
            Value = value;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Value;
        }
    }

    [Fact]
    public void Equals_SameComponents_ShouldBeEqual()
    {
        var a = new TestValueObject("test", 42);
        var b = new TestValueObject("test", 42);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentComponents_ShouldNotBeEqual()
    {
        var a = new TestValueObject("test", 42);
        var b = new TestValueObject("test", 99);
        var c = new TestValueObject("other", 42);

        a.Equals(b).Should().BeFalse();
        a.Equals(c).Should().BeFalse();
        (a != b).Should().BeTrue();
        (a != c).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ShouldReturnFalse()
    {
        var a = new TestValueObject("test", 42);

        a.Equals(null).Should().BeFalse();
    }
}
