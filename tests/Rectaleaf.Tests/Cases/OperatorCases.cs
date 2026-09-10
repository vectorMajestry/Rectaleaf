namespace TestCases;

using System;

public readonly struct Vector2D : IEquatable<Vector2D>
{
    public readonly int X;
    public readonly int Y;

    public Vector2D(int x, int y)
    {
        X = x;
        Y = y;
    }

    // Standard binary operators
    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);

    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);

    public static Vector2D operator *(Vector2D a, int scalar) => new(a.X * scalar, a.Y * scalar);

    public static Vector2D operator /(Vector2D a, int divisor) => new(a.X / divisor, a.Y / divisor);

    // Checked binary operators
    public static Vector2D operator checked +(Vector2D a, Vector2D b) =>
        new(checked(a.X + b.X), checked(a.Y + b.Y));

    public static Vector2D operator checked -(Vector2D a, Vector2D b) =>
        new(checked(a.X - b.X), checked(a.Y - b.Y));

    public static Vector2D operator checked *(Vector2D a, int scalar) =>
        new(checked(a.X * scalar), checked(a.Y * scalar));

    // Unsigned right shift operator (op_UnsignedRightShift)
    public static Vector2D operator >>>(Vector2D a, int shift) =>
        new(a.X >>> shift, a.Y >>> shift);

    // Signed shift operators
    public static Vector2D operator <<(Vector2D a, int shift) => new(a.X << shift, a.Y << shift);

    public static Vector2D operator >>(Vector2D a, int shift) => new(a.X >> shift, a.Y >> shift);

    // Standard unary operators
    public static Vector2D operator +(Vector2D a) => a;

    public static Vector2D operator -(Vector2D a) => new(-a.X, -a.Y);

    public static Vector2D operator ~(Vector2D a) => new(~a.X, ~a.Y);

    public static Vector2D operator ++(Vector2D a) => new(a.X + 1, a.Y + 1);

    public static Vector2D operator --(Vector2D a) => new(a.X - 1, a.Y - 1);

    public static bool operator !(Vector2D a) => a.X == 0 && a.Y == 0;

    // Checked unary operators
    public static Vector2D operator checked -(Vector2D a) =>
        new(checked(-a.X), checked(-a.Y));

    public static Vector2D operator checked ++(Vector2D a) =>
        new(checked(a.X + 1), checked(a.Y + 1));

    public static Vector2D operator checked --(Vector2D a) =>
        new(checked(a.X - 1), checked(a.Y - 1));

    // Comparison and equality operators
    public static bool operator ==(Vector2D a, Vector2D b) => a.X == b.X && a.Y == b.Y;

    public static bool operator !=(Vector2D a, Vector2D b) => !(a == b);

    public static bool operator <(Vector2D a, Vector2D b) => a.X < b.X && a.Y < b.Y;

    public static bool operator >(Vector2D a, Vector2D b) => a.X > b.X && a.Y > b.Y;

    // True and false operators
    public static bool operator true(Vector2D a) => a.X != 0 || a.Y != 0;

    public static bool operator false(Vector2D a) => a.X == 0 && a.Y == 0;

    // Implicit conversions
    public static implicit operator double(Vector2D v) => Math.Sqrt(v.X * v.X + v.Y * v.Y);

    public static implicit operator (int x, int y)(Vector2D v) => (v.X, v.Y);

    // Unchecked explicit conversion
    public static explicit operator int(Vector2D v) => v.X + v.Y;

    // Unchecked explicit conversion (required when checked version is defined)
    public static explicit operator byte(Vector2D v) => (byte)(v.X + v.Y);

    // Checked explicit conversion (op_CheckedExplicit)
    public static explicit operator checked byte(Vector2D v) => checked((byte)(v.X + v.Y));

    public bool Equals(Vector2D other) => this == other;

    public override bool Equals(object? obj) => obj is Vector2D other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);
}
