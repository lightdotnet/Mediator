using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Mediator.Tests
{
    internal class Assert
    {
        internal static void ShouldBe<T>(T actual, T expected)
            => NUnit.Framework.Assert.That(actual, Is.EqualTo(expected));

        internal static void ShouldBeNull(object? actual)
            => NUnit.Framework.Assert.That(actual, Is.Null);

        internal static void ShouldNotBeNull(object? actual)
            => NUnit.Framework.Assert.That(actual, Is.Not.Null);

        internal static void ShouldBeTrue(bool actual)
            => NUnit.Framework.Assert.That(actual, Is.True);

        internal static void ShouldBeFalse(bool actual)
            => NUnit.Framework.Assert.That(actual, Is.False);

        internal static void ShouldBeSameAs(object actual, object expected)
            => NUnit.Framework.Assert.That(actual, Is.SameAs(expected));

        internal static void ShouldBeOfType<T>(object actual)
            => NUnit.Framework.Assert.That(actual, Is.TypeOf<T>());

        internal static void ShouldHaveCount<T>(ICollection<T> collection, int expected)
            => NUnit.Framework.Assert.That(collection, Has.Count.EqualTo(expected));

        internal static T ShouldThrow<T>(Action action) where T : Exception
            => NUnit.Framework.Assert.Throws<T>(() => action())!;

        internal static T ShouldThrowAsync<T>(Func<Task> action) where T : Exception
            => NUnit.Framework.Assert.ThrowsAsync<T>(() => action())!;

        internal static void ShouldNotThrowAsync(Func<Task> action)
            => NUnit.Framework.Assert.DoesNotThrowAsync(() => action());
    }
}
