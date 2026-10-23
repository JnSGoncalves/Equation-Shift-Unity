using System;
using System.Numerics;

namespace EquationShift
{
    /// <summary>Arithmetic only: no scene lookups, eval, or floating-point rounding.</summary>
    public static class EquationEvaluator
    {
        public static bool TryEvaluate(string equation, out bool isTrue, out string error)
        {
            isTrue = false;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(equation))
            {
                error = "Equacao vazia.";
                return false;
            }

            int equals = equation.IndexOf('=');
            if (equals < 0 || equals != equation.LastIndexOf('='))
            {
                error = "A equacao precisa de exatamente um sinal de igual.";
                return false;
            }

            try
            {
                Rational left = new Parser(equation.Substring(0, equals)).Parse();
                Rational right = new Parser(equation.Substring(equals + 1)).Parse();
                isTrue = left.Equals(right);
                return true;
            }
            catch (FormatException exception)
            {
                error = exception.Message;
                return false;
            }
            catch (DivideByZeroException)
            {
                error = "Divisao por zero.";
                return false;
            }
        }

        private sealed class Parser
        {
            private readonly string text;
            private int index;

            public Parser(string expression) => text = expression;

            public Rational Parse()
            {
                Rational result = Expression();
                SkipSpaces();
                if (index != text.Length)
                    throw new FormatException("Simbolo inesperado ou operador ausente.");
                return result;
            }

            private Rational Expression()
            {
                Rational result = Term();
                while (true)
                {
                    if (Take('+')) result += Term();
                    else if (Take('-')) result -= Term();
                    else return result;
                }
            }

            private Rational Term()
            {
                Rational result = Number();
                while (true)
                {
                    if (Take('*')) result *= Number();
                    else if (Take('/')) result /= Number();
                    else return result;
                }
            }

            private Rational Number()
            {
                SkipSpaces();
                bool negative = Take('-');
                SkipSpaces();
                int start = index;
                BigInteger value = BigInteger.Zero;
                while (index < text.Length && text[index] >= '0' && text[index] <= '9')
                    value = value * 10 + (text[index++] - '0');
                if (index == start) throw new FormatException("Numero esperado.");
                return new Rational(negative ? -value : value, BigInteger.One);
            }

            private bool Take(char symbol)
            {
                SkipSpaces();
                if (index >= text.Length || text[index] != symbol) return false;
                index++;
                return true;
            }

            private void SkipSpaces()
            {
                while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
            }
        }

        // Reduced fractions preserve equalities such as 1/3*3=1 exactly.
        private readonly struct Rational : IEquatable<Rational>
        {
            private readonly BigInteger numerator;
            private readonly BigInteger denominator;

            public Rational(BigInteger numerator, BigInteger denominator)
            {
                if (denominator.IsZero) throw new DivideByZeroException();
                if (denominator.Sign < 0) { numerator = -numerator; denominator = -denominator; }
                BigInteger divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
                this.numerator = numerator / divisor;
                this.denominator = denominator / divisor;
            }

            public bool Equals(Rational other) => numerator == other.numerator && denominator == other.denominator;
            public static Rational operator +(Rational a, Rational b) =>
                new Rational(a.numerator * b.denominator + b.numerator * a.denominator, a.denominator * b.denominator);
            public static Rational operator -(Rational a, Rational b) =>
                new Rational(a.numerator * b.denominator - b.numerator * a.denominator, a.denominator * b.denominator);
            public static Rational operator *(Rational a, Rational b) =>
                new Rational(a.numerator * b.numerator, a.denominator * b.denominator);
            public static Rational operator /(Rational a, Rational b) =>
                new Rational(a.numerator * b.denominator, a.denominator * b.numerator);
        }
    }
}
