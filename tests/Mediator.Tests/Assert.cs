namespace Mediator.Tests
{
    internal class Assert
    {
        internal static void ShouldBe<T>(T value, T equalTo) => NUnit.Framework.Assert.That(value, Is.EqualTo(equalTo));
    }
}
