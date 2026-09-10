namespace TestCases;

using System;
using System.Collections.Generic;

public class LocalFunctionContainer
{
    private int _state;

    // Constructor containing local functions
    public LocalFunctionContainer(int initial)
    {
        int multiplier = 2;

        int ComputeInitial(int val)
        {
            return val * multiplier;
        }

        _state = ComputeInitial(initial);
    }

    // Property getter and setter containing local functions
    public int ComputedProperty
    {
        get
        {
            int LocalGetterHelper() => _state + 1;
            return LocalGetterHelper();
        }
        set
        {
            static int ValidateValue(int v) => v < 0 ? 0 : v;
            _state = ValidateValue(value);
        }
    }

    // Outer method with recursive local function hierarchy and variable capture
    public int ComplexProcess(int seed)
    {
        int outerAccumulator = seed;

        // Level 1 local function capturing outerAccumulator
        int OuterLocalFunction(int factor)
        {
            outerAccumulator += factor;
            string localTag = "level1";

            // Level 2 local function nested inside Level 1
            int InnerLocalFunction(int innerStep)
            {
                // Level 3 generic local function with constraints
                TResult DeepLocalFunction<TResult>(TResult val)
                    where TResult : class, new()
                {
                    return val;
                }

                outerAccumulator += innerStep;
                var res = DeepLocalFunction(new List<string>());
                res.Add(localTag);
                return res.Count + outerAccumulator;
            }

            return InnerLocalFunction(factor * 2);
        }

        // Static local function (does not capture closure)
        static double StaticLocalCalculation(double a, double b)
        {
            static double NestedStaticLocal(double x) => x * 2.0;
            return NestedStaticLocal(a) + b;
        }

        int result = OuterLocalFunction(5);
        return result + (int)StaticLocalCalculation(1.5, 2.5);
    }

    // Operator containing local functions
    public static LocalFunctionContainer operator +(LocalFunctionContainer a, LocalFunctionContainer b)
    {
        static int CombineStates(int s1, int s2)
        {
            int Add(int x, int y) => x + y;
            return Add(s1, s2);
        }

        return new LocalFunctionContainer(CombineStates(a._state, b._state));
    }
}
