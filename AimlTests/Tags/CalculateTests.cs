using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class CalculateTests {
	[TestCase("8", ExpectedResult = "8", TestName = "Scalar integer")]
	[TestCase("8.5", ExpectedResult = "8.5", TestName = "Scalar fraction")]
	[TestCase("3+2", ExpectedResult = "5", TestName = "Addition")]
	[TestCase("3-2", ExpectedResult = "1", TestName = "Subtraction")]
	[TestCase("3*2", ExpectedResult = "6", TestName = "Multiplication")]
	[TestCase("3/2", ExpectedResult = "1.5", TestName = "Division (float)")]
	[TestCase("3\\2", ExpectedResult = "1", TestName = "Division (integer)")]
	[TestCase("3%2", ExpectedResult = "1", TestName = "Modulo")]
	[TestCase("3^2", ExpectedResult = "9", TestName = "Exponentiation (^)")]
	[TestCase("3**2", ExpectedResult = "9", TestName = "Exponentiation (**)")]
	[TestCase("-3+-2", ExpectedResult = "-5", TestName = "Unary negation")]
	[TestCase("+3++2", ExpectedResult = "5", TestName = "Unary plus")]
	[TestCase("2+3*4", ExpectedResult = "14", TestName = "Order of operations 1")]
	[TestCase("3*4+2", ExpectedResult = "14", TestName = "Order of operations 2")]
	[TestCase("(2+3)*(3+1)", ExpectedResult = "20", TestName = "Parentheses")]
	[TestCase(" ( 2 + 3 )\n\t* ( 3 + 1 ) ", ExpectedResult = "20", TestName = "Whitespace")]
	[TestCase("floor(1.5)", ExpectedResult = "1", TestName = "Function - single argument")]
	[TestCase("floor ( 1.5 )", ExpectedResult = "1", TestName = "Function - single argument, whitespace")]
	[TestCase("pow(2,3)", ExpectedResult = "8", TestName = "Function - multiple arguments")]
	[TestCase("pow ( 2 ,\n\t3 ) ", ExpectedResult = "8", TestName = "Function - multiple arguments, whitespace")]
	public string Evaluate(string expr) => new Calculate(new(expr)).Evaluate(new AimlTest().RequestProcess);

	[TestCase("pi", TestName = "Function - no argument list")]
	[TestCase("pi+0", TestName = "Function - no argument list 2")]
	[TestCase("pi ()", TestName = "Function - empty argument list")]
	public void EvaluatePi(string expr) {
		var tag = new Calculate(new(expr));
		Assert.AreEqual(Math.PI, double.Parse(tag.Evaluate(new AimlTest().RequestProcess).ToString()), 1e-6);
	}

	[TestCase(" ", TestName = "Invalid - empty expression")]
	[TestCase("*2", TestName = "Invalid - missing operand 1")]
	[TestCase("3+", TestName = "Invalid - missing operand 2")]
	[TestCase("-", TestName = "Invalid - bare unary operator")]
	[TestCase("foo", TestName = "Invalid - unknown function")]
	[TestCase("pow", TestName = "Invalid - missing arguments")]
	[TestCase("pi(1)", TestName = "Invalid - too many arguments")]
	public void InvalidEvaluate(string expr) {
		var test = new AimlTest();
		var tag = new Calculate(new(expr));
		Assert.AreEqual("unknown", test.AssertWarning(() => tag.Evaluate(test.RequestProcess)));
	}
}
