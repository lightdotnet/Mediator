using System;
using System.Threading.Tasks;

namespace Light.Mediator
{
    public readonly struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Value = default;
        public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

        public bool Equals(Unit other) => true;
        public override bool Equals(object obj) => obj is Unit;
        public override int GetHashCode() => 0;
        public override string ToString() => "()";

        public static bool operator ==(Unit left, Unit right) => left.Equals(right);
        public static bool operator !=(Unit left, Unit right) => !left.Equals(right);
    }
}
