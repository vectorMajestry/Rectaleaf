namespace TestCases;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public struct ExtPoint
{
    public double X;
    public double Y;

    public ExtPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}

public static class GeometryExtensions
{
    // Instance extension block on ref struct/type
    extension(ref ExtPoint pt)
    {
        public double Magnitude => Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);

        public double Area
        {
            get => pt.X * pt.Y;
        }

        /*
        // Note: Roslyn 5.6 emits CS9282 (member not allowed in extension block) for indexers
        [IndexerName("Dimensions")]
        public double this[int index]
        {
            get => index == 0 ? pt.X : pt.Y;
            set
            {
                if (index == 0) pt.X = value;
                else pt.Y = value;
            }
        }
        */

        public void Translate(double dx, double dy)
        {
            pt.X += dx;
            pt.Y += dy;
        }
    }

    // Instance extension block with 'in' receiver
    extension(in ExtPoint pt)
    {
        public double InMagnitude => Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
    }

    // Instance extension block with 'scoped ref' receiver
    extension(scoped ref ExtPoint pt)
    {
        public void Reset()
        {
            pt.X = 0;
            pt.Y = 0;
        }
    }

    // Extension on standard reference type
    extension(string s)
    {
        public bool IsBlank => string.IsNullOrWhiteSpace(s);

        public int WordCount() => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // Static extension block without receiver parameter
    extension(ExtPoint)
    {
        public static ExtPoint Zero => new(0, 0);

        public static ExtPoint UnitX => new(1, 0);

        public static ExtPoint Create(double x, double y) => new(x, y);
    }

    // Generic extension block with generic constraints
    extension<TKey, TValue>(Dictionary<TKey, TValue> dict)
        where TKey : notnull
    {
        public int KeyCount => dict.Count;

        public TValue? GetValueOrFallback(TKey key, TValue? fallback = default)
        {
            return dict.TryGetValue(key, out var val) ? val : fallback;
        }
    }

    // Generic extension block on open collection
    extension<T>(List<T> list)
    {
        public bool IsEmpty => list.Count == 0;

        public T? FirstOrFallback() => list.Count > 0 ? list[0] : default;
    }
}
