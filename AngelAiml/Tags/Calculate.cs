﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AngelAiml.Tags;
/// <summary>Evaluates the content as an arithmetic expression.</summary>
/// <remarks>
///		<para>This element supports decimal <see cref="double"/> values, operators, functions and parentheses. Whitespace is ignored.</para>
///		<para>The following operators are supported, in order of priority:</para>
///		<list type="number">
///			<item><c>**</c> <c>^</c> (exponentiation)</item>
///			<item>Unary <c>+</c> <c>-</c></item>
///			<item><c>*</c> (multiplication); <c>/</c> (float division); <c>%</c> <c>mod</c> (modulo); <c>\</c> (integer division)</item>
///			<item><c>+</c> (addition); <c>-</c> (subtraction)</item>
///			<item>Functions</item>
///		</list>
///		<para>The following functions are supported:</para>
///		<list type="table">
///			<item><term><c>abs(x)</c></term> <description>returns the absolute value of x.</description></item>
///			<item><term><c>acos(x)</c></term> <description>returns the angle whose cosine is x, in radians.</description></item>
///			<item><term><c>acosh(x)</c></term> <description>returns the angle whose hyperbolic cosine is x, in radians (unavailable in .NET Standard 2.0).</description></item>
///			<item><term><c>asin(x)</c></term> <description>returns the angle whose sine is x, in radians.</description></item>
///			<item><term><c>asinh(x)</c></term> <description>returns the angle whose hyperbolic sine is x, in radians (unavailable in .NET Standard 2.0).</description></item>
///			<item><term><c>atan(x)</c></term> <description>returns the angle whose tangent is x, in radians.</description></item>
///			<item><term><c>atan(y, x)</c></term> <description>returns the angle whose cosine is y/x, in radians, taking into account quadrants and x = 0.</description></item>
///			<item><term><c>atanh(x)</c></term> <description>returns the angle whose hyperbolic tangent is x, in radians (unavailable in .NET Standard 2.0).</description></item>
///			<item><term><c>ceil(x)</c>, <c>ceiling(x)</c></term> <description>rounds x to the nearest integer upward.</description></item>
///			<item><term><c>cos(x)</c></term> <description>returns the cosine of x radians.</description></item>
///			<item><term><c>cosh(x)</c></term> <description>returns the hyperbolic cosine of x radians.</description></item>
///			<item><term><c>e</c></term> <description>returns the constant natural logarithmic base.</description></item>
///			<item><term><c>exp(x)</c></term> <description>returns e raised to the power x.</description></item>
///			<item><term><c>fix(x)</c>, <c>truncate(x)</c></term> <description>rounds x to the nearest integer toward zero, truncating its fractional part.</description></item>
///			<item><term><c>floor(x)</c></term> <description>rounds x to the nearest integer downward.</description></item>
///			<item><term><c>log(x, y)</c></term> <description>returns the base y logarithm of x.</description></item>
///			<item><term><c>log10(x)</c></term> <description>returns the base 10 logarithm of x.</description></item>
///			<item><term><c>ln(x)</c>, <c>log(x)</c></term> <description>returns the natural logarithm of x.</description></item>
///			<item><term><c>max(x, ...)</c></term> <description>returns the maximum number in the list.</description></item>
///			<item><term><c>min(x, ...)</c></term> <description>returns the minimum number in the list.</description></item>
///			<item><term><c>pi</c></term> <description>returns the circle constant π.</description></item>
///			<item><term><c>pow(x, y)</c></term> <description>returns x raised to the power y.</description></item>
///			<item><term><c>round(x)</c></term> <description>rounds x to the nearest integer, or the nearest even integer at midpoints.</description></item>
///			<item><term><c>roundcom(x)</c></term> <description>rounds x to the nearest integer, or away from zero at midpoints (commercial rounding).</description></item>
///			<item><term><c>sign(x)</c></term> <description>returns the sign of x: -1 if x is negative, 0 if x is 0, or 1 if x is positive.</description></item>
///			<item><term><c>sin(x)</c></term> <description>returns the sine of x radians.</description></item>
///			<item><term><c>sinh(x)</c></term> <description>returns the hyperbolic sine of x radians.</description></item>
///			<item><term><c>sqrt(x)</c></term> <description>returns the square root of x.</description></item>
///			<item><term><c>tan(x)</c></term> <description>returns the tangent of x radians.</description></item>
///			<item><term><c>tanh(x)</c></term> <description>returns the hyperbolic tangent of x radians.</description></item>
///		</list>
///		<para>This element is part of an extension to AIML.</para>
/// </remarks>
public sealed partial class Calculate(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	private const string ERROR_RESPONSE = "unknown";

	public override string Evaluate(RequestProcess process) {
		var s = EvaluateChildren(process);
		if (string.IsNullOrWhiteSpace(s)) {
			LogSyntaxError(GetLogger(process, true), s);
			return ERROR_RESPONSE;
		}

		try {
			var pos = 0;
			var result = EvaluateExpr(process, s, 0, ref pos);
			return pos >= s.Length ? result.ToString() : throw new FormatException();
		} catch (FormatException) {
			LogSyntaxError(GetLogger(process, true), s);
			return ERROR_RESPONSE;
		}
	}

	private static double EvaluateExpr(RequestProcess process, string s, int priority, ref int pos) {
		while (pos < s.Length && char.IsWhiteSpace(s[pos])) ++pos;
		if (pos >= s.Length) throw new FormatException();

		double v1;
		switch (s[pos]) {
			case '(':
				++pos;
				v1 = EvaluateExpr(process, s, 0, ref pos);
				while (pos < s.Length && char.IsWhiteSpace(s[pos])) ++pos;
				if (pos >= s.Length || s[pos] != ')') throw new FormatException();
				++pos;
				break;
			case ')':
				throw new FormatException();
			case '+':
				++pos;
				v1 = EvaluateExpr(process, s, 3, ref pos);
				break;
			case '-':
				++pos;
				v1 = -EvaluateExpr(process, s, 3, ref pos);
				break;
			case ',':
				throw new FormatException();
			default:
				if (s[pos] is >= '0' and <= '9') {
					var startPos = pos;
					while (pos < s.Length && ((s[pos] >= '0' && s[pos] <= '9') || SubstringAt(s, process.Bot.Config.Locale.NumberFormat.NumberDecimalSeparator, pos)))
						++pos;
					var numString = s[startPos..pos];
					SkipWhitespace(s, ref pos);
					if (SubstringAt(s, "point", pos)) {
						pos += 5;
						SkipWhitespace(s, ref pos);
						startPos = pos;
						while (pos < s.Length && ((s[pos] >= '0' && s[pos] <= '9') || SubstringAt(s, process.Bot.Config.Locale.NumberFormat.NumberDecimalSeparator, pos)))
							++pos;
						numString += "." + s[startPos..pos];
					}
					v1 = double.Parse(numString, process.Bot.Config.Locale);
				} else if (s[pos] == '_' || char.IsLetter(s[pos])) {
					var startPos = pos;
					while (pos < s.Length && (s[pos] == '_' || char.IsLetter(s[pos])))
						++pos;
					var functionName = s[startPos..pos];
					SkipWhitespace(s, ref pos);
					var args = new List<double>();
					if (pos < s.Length && s[pos] == '(') {
						++pos;
						SkipWhitespace(s, ref pos);
						if (pos < s.Length && s[pos] != ')') {
							while (true) {
								args.Add(EvaluateExpr(process, s, 0, ref pos));
								SkipWhitespace(s, ref pos);
								if (pos >= s.Length) throw new FormatException();
								if (s[pos] == ')') break;
								if (s[pos] != ',') throw new FormatException();
								++pos;
							}
						}
						++pos;
					}
					v1 = functionName.ToLowerInvariant() switch {
						"pi"       => args.Count == 0 ? Math.PI : ThrowArgumentCountException("pi", args, 0),
						"π"        => args.Count == 0 ? Math.PI : ThrowArgumentCountException("π", args, 0),
						"e"        => args.Count == 0 ? Math.E : ThrowArgumentCountException("e", args, 0),
						"abs"      => args.Count == 1 ? Math.Abs(args[0]) : ThrowArgumentCountException("abs", args, 1),
						"acos"     => args.Count == 1 ? Math.Acos(args[0]) : ThrowArgumentCountException("acos", args, 1),
						"asin"     => args.Count == 1 ? Math.Asin(args[0]) : ThrowArgumentCountException("asin", args, 1),
						"atan"     => args.Count switch { 1 => Math.Atan(args[0]), 2 => Math.Atan2(args[0], args[1]), _ => ThrowArgumentCountException("atan", args, 1, 2) },
						"atan2"    => args.Count == 2 ? Math.Atan2(args[0], args[1]) : ThrowArgumentCountException("atan2", args, 2),
						"ceiling"  => args.Count == 1 ? Math.Ceiling(args[0]) : ThrowArgumentCountException("ceiling", args, 1),
						"ceil"     => args.Count == 1 ? Math.Ceiling(args[0]) : ThrowArgumentCountException("ceil", args, 1),
						"cos"      => args.Count == 1 ? Math.Cos(args[0]) : ThrowArgumentCountException("cos", args, 1),
						"cosh"     => args.Count == 1 ? Math.Cosh(args[0]) : ThrowArgumentCountException("cosh", args, 1),
						"exp"      => args.Count == 1 ? Math.Exp(args[0]) : ThrowArgumentCountException("exp", args, 1),
						"floor"    => args.Count == 1 ? Math.Floor(args[0]) : ThrowArgumentCountException("floor", args, 1),
						"log"      => args.Count switch { 1 => Math.Log(args[0]), 2 => Math.Log(args[0], args[1]), _ => ThrowArgumentCountException("log", args, 1, 2) },
						"log10"    => args.Count == 1 ? Math.Log(args[0], 10) : ThrowArgumentCountException("log10", args, 1),
						"ln"       => args.Count == 1 ? Math.Log(args[0]) : ThrowArgumentCountException("ln", args, 1),
						"max"      => args.Count > 0 ? args.Max() : throw new FormatException($"Invalid number of arguments for max: expected at least 1 but found {args.Count}"),
						"min"      => args.Count > 0 ? args.Min() : throw new FormatException($"Invalid number of arguments for max: expected at least 1 but found {args.Count}"),
						"pow"      => args.Count == 2 ? Math.Pow(args[0], args[1]) : ThrowArgumentCountException("pow", args, 2),
						"round"    => args.Count is 1 or 2 ? Math.Round(args[0], args.Count == 2 ? (int) args[1] : 0) : ThrowArgumentCountException("round", args, 1, 2),
						"roundcom" => args.Count is 1 or 2 ? Math.Round(args[0], args.Count == 2 ? (int) args[1] : 0, MidpointRounding.AwayFromZero) : ThrowArgumentCountException("roundcom", args, 1, 2),
						"sign"     => args.Count == 1 ? Math.Sign(args[0]) : ThrowArgumentCountException("sign", args, 1),
						"sin"      => args.Count == 1 ? Math.Sin(args[0]) : ThrowArgumentCountException("sin", args, 1),
						"sinh"     => args.Count == 1 ? Math.Sinh(args[0]) : ThrowArgumentCountException("sinh", args, 1),
						"sqrt"     => args.Count == 1 ? Math.Sqrt(args[0]) : ThrowArgumentCountException("sqrt", args, 1),
						"tan"      => args.Count == 1 ? Math.Tan(args[0]) : ThrowArgumentCountException("tan", args, 1),
						"tanh"     => args.Count == 1 ? Math.Tanh(args[0]) : ThrowArgumentCountException("tanh", args, 1),
						"truncate" => args.Count == 1 ? Math.Truncate(args[0]) : ThrowArgumentCountException("truncate", args, 1),
						"fix"      => args.Count == 1 ? Math.Truncate(args[0]) : ThrowArgumentCountException("fix", args, 1),
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
						"acosh"    => args.Count == 1 ? Math.Acosh(args[0]) : ThrowArgumentCountException("acosh", args, 1),
						"asinh"    => args.Count == 1 ? Math.Asinh(args[0]) : ThrowArgumentCountException("asinh", args, 1),
						"atanh"    => args.Count == 1 ? Math.Atanh(args[0]) : ThrowArgumentCountException("atanh", args, 1),
#else
						"acosh"    => throw new PlatformNotSupportedException("acosh is unavailable in .NET Standard 2.0."),
						"asinh"    => throw new PlatformNotSupportedException("asinh is unavailable in .NET Standard 2.0."),
						"atanh"    => throw new PlatformNotSupportedException("atanh is unavailable in .NET Standard 2.0."),
#endif
						_ => throw new FormatException($"Invalid function name: {functionName.ToLowerInvariant()}")
					};;
				} else
					throw new FormatException();
				break;
		}

		SkipWhitespace(s, ref pos);
		while (pos < s.Length) {
			int priority2;
			switch (s[pos]) {
				case ',':
					return v1;
				case ')':
					return v1;
				case '+':
				case '-':
					priority2 = 1;
					break;
				case '*':
				case '×':
				case '/':
				case '\\':
				case '%':
					priority2 = SubstringAt(s, "**", pos) ? 4 : 2;
					break;
				case '^':
					priority2 = 4;
					break;
				default:
					priority2 = SubstringAt(s, "mod", pos) ? 2 : throw new FormatException();
					break;
			}
			if (priority >= priority2) return v1;

			switch (s[pos]) {
				case '+':
					++pos;
					v1 += EvaluateExpr(process, s, 1, ref pos);
					break;
				case '-':
					++pos;
					v1 -= EvaluateExpr(process, s, 1, ref pos);
					break;
				case '*':
					if (SubstringAt(s, "**", pos)) {
						pos += 2;
						v1 = Math.Pow(v1, EvaluateExpr(process, s, 4, ref pos));
						break;
					}
					++pos;
					v1 *= EvaluateExpr(process, s, 2, ref pos);
					break;
				case '×':
					++pos;
					v1 *= EvaluateExpr(process, s, 2, ref pos);
					break;
				case '/':
					++pos;
					v1 /= EvaluateExpr(process, s, 2, ref pos);
					break;
				case '\\':
					++pos;
					v1 = (long) v1 / (long) EvaluateExpr(process, s, 2, ref pos);
					break;
				case '%':
					++pos;
					v1 %= EvaluateExpr(process, s, 2, ref pos);
					break;
				case '^':
					++pos;
					v1 = Math.Pow(v1, EvaluateExpr(process, s, 4, ref pos));
					break;
				default:
					if (SubstringAt(s, "mod", pos)) {
						++pos;
						v1 %= EvaluateExpr(process, s, 2, ref pos);
						break;
					}
					throw new FormatException();
			}
			SkipWhitespace(s, ref pos);
		}
		return v1;
	}

	private static void SkipWhitespace(string s, ref int pos) {
		while (pos < s.Length && char.IsWhiteSpace(s[pos])) ++pos;
	}
	private static bool SubstringAt(string haystack, string needle, int pos)
		=> haystack.Length - pos >= needle.Length && haystack.Substring(pos, needle.Length) == needle;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
	[DoesNotReturn]
#endif
	private static double ThrowArgumentCountException(string functionName, List<double> list, int min, int max)
		=> throw new FormatException($"Invalid number of arguments for {functionName}: expected {min} ~ {max} but found {list.Count}");
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
	[DoesNotReturn]
#endif
	private static double ThrowArgumentCountException(string functionName, List<double> list, int expected)
		=> throw new FormatException($"Invalid number of arguments for {functionName}: expected {expected} but found {list.Count}");

	#region Log templates

	[LoggerMessage(LogLevel.Warning, "In element <calculate>: syntax error: {Content}")]
	private static partial void LogSyntaxError(ILogger logger, string content);

	#endregion
}
