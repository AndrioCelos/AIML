using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Evaluates the content as an arithmetic expression.
		/// </summary>
		/// <remarks>
		///     <para>This element supports decimal <see cref="double"/> values, operators, functions and parentheses. Whitespace is ignored.</para>
		///     <para>The following operators are supported, in order of priority:</para>
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
		///			<item><term><c>acosh(x)</c></term> <description>returns the angle whose hyperbolic cosine is x, in radians.</description></item>
		///			<item><term><c>asin(x)</c></term> <description>returns the angle whose sine is x, in radians.</description></item>
		///			<item><term><c>asinh(x)</c></term> <description>returns the angle whose hyperbolic sine is x, in radians.</description></item>
		///			<item><term><c>atan(x)</c></term> <description>returns the angle whose tangent is x, in radians.</description></item>
		///			<item><term><c>atan(y, x)</c></term> <description>returns the angle whose cosine is y/x, in radians, taking into account quadrants and x = 0.</description></item>
		///			<item><term><c>atanh(x)</c></term> <description>returns the angle whose hyperbolic tangent is x, in radians.</description></item>
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
		///			<item><term><c>pi(x)</c></term> <description>returns the circle constant π.</description></item>
		///			<item><term><c>pow(x, y)</c></term> <description>returns x raised to the power y.</description></item>
		///			<item><term><c>round(x)</c></term> <description>rounds x to the nearest integer, or the nearest even integer at midpoints.</description></item>
		///			<item><term><c>roundcom(x)</c></term> <description>rounds x to the nearest integer, or away from zero at midpoints (commercial rounding).</description></item>
		///			<item><term><c>sign(x)</c></term> <description>returns the sign of x-1 if x is negative, 0 if x is 0, or 1 if x is positive.</description></item>
		///			<item><term><c>sin(x)</c></term> <description>returns the sine of x radians.</description></item>
		///			<item><term><c>sinh(x)</c></term> <description>returns the hyperbolic sine of x radians.</description></item>
		///			<item><term><c>sqrt(x)</c></term> <description>returns the square root of x.</description></item>
		///			<item><term><c>tan(x)</c></term> <description>returns the tangent of x radians.</description></item>
		///			<item><term><c>tanh(x)</c></term> <description>returns the hyperbolic tangent of x radians.</description></item>
		///		</list>
		///		<para>This element is part of an extension to AIML.</para>
		/// </remarks>
		public sealed class Calculate : RecursiveTemplateTag {
			public Calculate(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				var s = this.Children?.Evaluate(process)?.Trim() ?? "";
				if (string.IsNullOrWhiteSpace(s)) {
					process.Log(LogLevel.Warning, "In element <calculate>: invalid syntax " + s);
					return "unknown";
				}

				try {
					var pos = 0;
					var result = EvaluateExpr(process, s, 0, ref pos);
					if (pos < s.Length) throw new FormatException();
					return result.ToString();
				} catch (FormatException) {
					process.Log(LogLevel.Warning, "In element <calculate>: invalid syntax " + s);
					return "unknown";
				}
			}

			private double EvaluateExpr(RequestProcess process, string s, int priority, ref int pos) {
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
						if (s[pos] >= '0' && s[pos] <= '9') {
							var startPos = pos;
							while (pos < s.Length && ((s[pos] >= '0' && s[pos] <= '9') || SubstringAt(s, process.Bot.Config.Locale.NumberFormat.NumberDecimalSeparator, pos)))
								++pos;
							var numString = s.Substring(startPos, pos - startPos);
							SkipWhitespace(s, ref pos);
							if (SubstringAt(s, "point", pos)) {
								pos += 5;
								SkipWhitespace(s, ref pos);
								startPos = pos;
								while (pos < s.Length && ((s[pos] >= '0' && s[pos] <= '9') || SubstringAt(s, process.Bot.Config.Locale.NumberFormat.NumberDecimalSeparator, pos)))
									++pos;
								numString += "." + s.Substring(startPos, pos - startPos);
							}
							v1 = double.Parse(numString, process.Bot.Config.Locale);
						} else if (s[pos] == '_' || char.IsLetter(s[pos])) {
							var startPos = pos;
							while (pos < s.Length && (s[pos] == '_' || char.IsLetter(s[pos])))
								++pos;
							var functionName = s.Substring(startPos, pos - startPos);
							SkipWhitespace(s, ref pos);
							var parameters = new List<double>();
							if (pos < s.Length && s[pos] == '(') {
								++pos;
								SkipWhitespace(s, ref pos);
								if (pos < s.Length && s[pos] != ')') {
									while (true) {
										parameters.Add(EvaluateExpr(process, s, 0, ref pos));
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
								"pi" => Math.PI,
								"π" => Math.PI,
								"e" => Math.E,
								"abs" => Math.Abs(parameters[0]),
								"acos" => Math.Acos(parameters[0]),
								"acosh" => Math.Acosh(parameters[0]),
								"asin" => Math.Asin(parameters[0]),
								"asinh" => Math.Asinh(parameters[0]),
								"atan" => parameters.Count > 1 ? Math.Atan2(parameters[0], parameters[1]) : Math.Atan(parameters[0]),
								"atan2" => Math.Atan2(parameters[0], parameters[1]),
								"atanh" => Math.Atanh(parameters[0]),
								"ceiling" => Math.Ceiling(parameters[0]),
								"ceil" => Math.Ceiling(parameters[0]),
								"cos" => Math.Cos(parameters[0]),
								"cosh" => Math.Cosh(parameters[0]),
								"exp" => Math.Exp(parameters[0]),
								"floor" => Math.Floor(parameters[0]),
								"log" => parameters.Count > 1 ? Math.Log(parameters[0], parameters[1]) : Math.Log(parameters[0]),
								"log10" => Math.Log(parameters[0], 10),
								"ln" => Math.Log(parameters[0], Math.E),
								"max" => parameters.Count > 0 ? parameters.Max() : throw new FormatException(),
								"min" => parameters.Count > 0 ? parameters.Min() : throw new FormatException(),
								"pow" => Math.Pow(parameters[0], parameters[1]),
								"round" => Math.Round(parameters[0], parameters.Count == 2 ? (int) parameters[1] : 0),
								"roundcom" => Math.Round(parameters[0], parameters.Count == 2 ? (int) parameters[1] : 0, MidpointRounding.AwayFromZero),
								"sign" => Math.Sign(parameters[0]),
								"sin" => Math.Sin(parameters[0]),
								"sinh" => Math.Sinh(parameters[0]),
								"sqrt" => Math.Sqrt(parameters[0]),
								"tan" => Math.Tan(parameters[0]),
								"tanh" => Math.Tanh(parameters[0]),
								"truncate" => Math.Truncate(parameters[0]),
								"fix" => Math.Truncate(parameters[0]),
								_ => throw new FormatException()
							};
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
							if (SubstringAt(s, "**", pos)) priority2 = 4;
							else priority2 = 2;
							break;
						case '^':
							priority2 = 4;
							break;
						default:
							if (SubstringAt(s, "mod", pos)) priority2 = 2;
							else throw new FormatException();
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

			public static Calculate FromXml(XmlNode node, AimlLoader loader) {
				return new Calculate(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
