using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PolynomialLibrary;

namespace TestComplexMultivariatePolynomial
{
	[TestClass]
	public class CoreFunctionality
	{
		private TestContext m_testContext;
		public TestContext TestContext { get { return m_testContext; } set { m_testContext = value; } }

		[TestMethod]
		public void TestParseAndToString()
		{
			string[] toTest = new string[]
			{
				"a*b*c*d - w*x*y*z",
				"X - 1",
				"2*X^4 + 13*X^3 + 29*X^2 + 29*X + 13",
				"w^2*x*y + w*x + w*y + 1",
				"144*x*y + 12*x + 12*y + 1",
				"144*x*y + 12*y - 12*x - 1",
				"144*x*y - 12*x - 12*y - 1",
				"144*x*y - 12*x - 12*y",
				"144*x",
				"x",
				"1",
				"0"
			};

			int counter = 1;
			foreach (string testString in toTest)
			{
				ComplexMultivariatePolynomial testPolynomial = ComplexMultivariatePolynomial.Parse(testString);
				string expected = testString;//.Replace(" ", "");
				string actual = testPolynomial.ToString();//.Replace(" ", "");
				bool isMatch = (expected == actual);
				string passFailString = isMatch ? "PASS" : "FAIL";
				string inputOutputString = isMatch ? $"Polynomial: \'{testPolynomial.ToString()}\"" : $"Expected: \"{expected}\"; Actual: \"{actual}\"";
				TestContext.WriteLine($"Test #{counter} => Pass/Fail: \"{passFailString}\" {inputOutputString}");
				Assert.AreEqual(expected, actual, $"Test #{counter}: ComplexMultivariatePolynomial.Parse(\"{testString}\").ToString();");

				counter++;
			}
		}

		[TestMethod]
		public void TestEvaluate()
		{
			Complex expected = new Complex(104053773133, 0);

			string polyString = "36*x*y - 6*x - 6*y + 1";
			ComplexMultivariatePolynomial poly = ComplexMultivariatePolynomial.Parse(polyString);
			List<Tuple<char, Complex>> indeterminants = new List<Tuple<char, Complex>>()
			{
				new Tuple<char, Complex>('x', 45468),
				new Tuple<char, Complex>('y', 63570),
			};

			Complex actual = poly.Evaluate(indeterminants);

			TestContext.WriteLine($"Result: \"{actual}\".");
			Assert.AreEqual(expected, actual, $"Test of: ComplexMultivariatePolynomial.Evaluate({polyString}) where {string.Join(" and ", indeterminants.Select(tup => $"{tup.Item1} = {tup.Item2}"))}");
		}

		[TestMethod]
		public void TestMonomialOrdering()
		{
			string toParse = "3*X^2*Y^3 + 6*X* Y^4 + X^3*Y^2 + 4*X^5 - 6*X^2*Y + 3*X* Y*Z - 5*X^2 + 3*Y^3 + 24*X* Y - 4";
			string expected = "4*X^5 + 6*X*Y^4 + 3*X^2*Y^3 + X^3*Y^2 + 3*Y^3 - 6*X^2*Y + 3*X*Y*Z - 5*X^2 + 24*X*Y - 4";

			ComplexMultivariatePolynomial poly = ComplexMultivariatePolynomial.Parse(toParse);
			string actual = poly.ToString();

			TestContext.WriteLine($"Result: \"{actual}\".");
			Assert.AreEqual(expected, actual, $"Test of: Monomial Ordering");
		}
	}
}
