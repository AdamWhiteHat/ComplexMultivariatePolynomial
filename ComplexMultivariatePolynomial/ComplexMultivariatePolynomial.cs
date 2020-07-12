using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace PolynomialLibrary
{
	public class ComplexMultivariatePolynomial
	{
		public Term[] Terms { get; private set; }
		public int Degree { get { return Terms.Any() ? Terms.Select(t => t.Degree).Max() : 0; } }

		#region Constructor & Parse

		public ComplexMultivariatePolynomial(Term[] terms)
		{
			Terms = CloneHelper<Term>.CloneCollection(terms).ToArray();
			OrderMonomials();
		}

		public static ComplexMultivariatePolynomial Parse(string polynomialString)
		{
			string input = polynomialString;
			if (string.IsNullOrWhiteSpace(input)) { throw new ArgumentException(); }

			string inputString = input.Replace(" ", "").Replace("-", "+-");
			if (inputString.StartsWith("+-")) { inputString = new string(inputString.Skip(1).ToArray()); }
			string[] stringTerms = inputString.Split(new char[] { '+' });

			if (!stringTerms.Any()) { throw new FormatException(); }

			Term[] terms = stringTerms.Select(str => Term.Parse(str)).ToArray();

			return new ComplexMultivariatePolynomial(terms);
		}

		public static ComplexMultivariatePolynomial GetDerivative(ComplexMultivariatePolynomial poly, char symbol)
		{
			List<Term> resultTerms = new List<Term>();
			foreach (Term term in poly.Terms)
			{
				if (term.Variables.Any() && term.Variables.Any(indt => indt.Symbol == symbol))
				{
					Complex newTerm_Coefficient = 0;
					List<Indeterminate> newTerm_Variables = new List<Indeterminate>();

					foreach (Indeterminate variable in term.Variables)
					{
						if (variable.Symbol == symbol)
						{
							newTerm_Coefficient = term.CoEfficient * variable.Exponent;

							int newExponent = variable.Exponent - 1;
							if (newExponent > 0)
							{
								newTerm_Variables.Add(new Indeterminate(symbol, newExponent));
							}
						}
						else
						{
							newTerm_Variables.Add(variable.Clone());
						}
					}

					resultTerms.Add(new Term(newTerm_Coefficient, newTerm_Variables.ToArray()));
				}
			}

			return new ComplexMultivariatePolynomial(resultTerms.ToArray());
		}

		private void OrderMonomials()
		{
			var orderedTerms = Terms.OrderByDescending(t => t.Degree);
			orderedTerms = orderedTerms.ThenBy(t => t.VariableCount());
			orderedTerms = orderedTerms.ThenByDescending(t => t.CoEfficient);
			Terms = orderedTerms.ToArray();
		}

		internal bool HasVariables()
		{
			return this.Terms.Any(t => t.HasVariables());
		}

		internal Complex MaxCoefficient()
		{
			if (HasVariables())
			{
				var termsWithVariables = this.Terms.Select(t => t).Where(t => t.HasVariables());
				return termsWithVariables.Select(t => t.CoEfficient).Max();
			}
			return -1;
		}

		#endregion

		#region Evaluate

		public Complex Evaluate(List<Tuple<char, Complex>> indeterminateValues)
		{
			SetIndeterminateValues(indeterminateValues);
			return Evaluate();
		}

		public Complex Evaluate()
		{
			Complex result = new Complex(0, 0);
			foreach (Term term in Terms)
			{
				result = Complex.Add(result, term.Evaluate());
			}
			return result;
		}

		public void SetIndeterminateValues(List<Tuple<char, Complex>> indeterminateValues)
		{
			foreach (Term term in Terms)
			{
				term.SetIndeterminateValues(indeterminateValues);
			}
		}

		#endregion

		#region Arithmetic

		public static ComplexMultivariatePolynomial GCD(ComplexMultivariatePolynomial left, ComplexMultivariatePolynomial right)
		{
			ComplexMultivariatePolynomial minuend = left.Clone();
			ComplexMultivariatePolynomial subtrahend = right.Clone();
			ComplexMultivariatePolynomial difference;
			Complex minuendMaxCoefficient = 0;
			Complex subtrahendMaxCoefficient = 0;
			Complex differenceMaxCoefficient = 0;

			do
			{
				minuendMaxCoefficient = minuend.MaxCoefficient();
				subtrahendMaxCoefficient = subtrahend.MaxCoefficient();
				difference = ComplexMultivariatePolynomial.Subtract(minuend, subtrahend).Clone();
				differenceMaxCoefficient = difference.MaxCoefficient();

				if (Complex.Abs(minuendMaxCoefficient) > Complex.Abs(subtrahendMaxCoefficient) && Complex.Abs(subtrahendMaxCoefficient) > Complex.Abs(differenceMaxCoefficient))
				{
					minuend = subtrahend.Clone();
					subtrahend = difference.Clone();
				}
				else if (Complex.Abs(differenceMaxCoefficient) > Complex.Abs(subtrahendMaxCoefficient))
				{
					//minuend = subtrahend.Clone();
					subtrahend = difference.Clone();
				}
			}
			while (Complex.Abs(minuendMaxCoefficient) > 0 && Complex.Abs(subtrahendMaxCoefficient) > 0 && minuend.HasVariables() && subtrahend.HasVariables());

			if (minuend.HasVariables())
			{
				return subtrahend.Clone();
			}
			else
			{
				return minuend.Clone();
			}
		}

		public static ComplexMultivariatePolynomial Add(ComplexMultivariatePolynomial left, ComplexMultivariatePolynomial right)
		{
			return OneToOneArithmetic(left, right, Term.Add);
		}

		public static ComplexMultivariatePolynomial Subtract(ComplexMultivariatePolynomial left, ComplexMultivariatePolynomial right)
		{
			return OneToOneArithmetic(left, right, Term.Subtract);
		}

		private static ComplexMultivariatePolynomial OneToOneArithmetic(ComplexMultivariatePolynomial left, ComplexMultivariatePolynomial right, Func<Term, Term, Term> operation)
		{
			List<Term> leftTermsList = CloneHelper<Term>.CloneCollection(left.Terms).ToList();

			foreach (Term rightTerm in right.Terms)
			{
				var match = leftTermsList.Where(leftTerm => Term.AreIdentical(leftTerm, rightTerm));
				if (match.Any())
				{
					Term matchTerm = match.Single();
					leftTermsList.Remove(matchTerm);

					Term result = operation.Invoke(matchTerm, rightTerm);
					if (result.CoEfficient != 0)
					{
						if (!leftTermsList.Any(lt => lt.Equals(result)))
						{
							leftTermsList.Add(result);
						}
					}
				}
				else
				{
					leftTermsList.Add(Term.Negate(rightTerm));
				}
			}
			return new ComplexMultivariatePolynomial(leftTermsList.ToArray());
		}

		public static ComplexMultivariatePolynomial Multiply(ComplexMultivariatePolynomial left, ComplexMultivariatePolynomial right)
		{
			List<Term> resultTerms = new List<Term>();

			foreach (var leftTerm in left.Terms)
			{
				foreach (var rightTerm in right.Terms)
				{
					Term newTerm = Term.Multiply(leftTerm, rightTerm);

					// Combine like terms
					var likeTerms = resultTerms.Where(trm => Term.AreIdentical(newTerm, trm));
					if (likeTerms.Any())
					{
						resultTerms = resultTerms.Except(likeTerms).ToList();

						Term likeTermsSum = likeTerms.Aggregate(Term.Add);
						Term sum = Term.Add(newTerm, likeTermsSum);

						newTerm = sum;
					}

					// Add new term to resultTerms
					resultTerms.Add(newTerm);
				}
			}

			return new ComplexMultivariatePolynomial(resultTerms.ToArray());
		}

		public static ComplexMultivariatePolynomial Pow(ComplexMultivariatePolynomial poly, int exponent)
		{
			if (exponent < 0)
			{
				throw new NotImplementedException("Raising a polynomial to a negative exponent not supported.");
			}
			else if (exponent == 0)
			{
				return new ComplexMultivariatePolynomial(new Term[] { new Term(1, new Indeterminate[0]) });
			}
			else if (exponent == 1)
			{
				return poly.Clone();
			}

			ComplexMultivariatePolynomial result = poly.Clone();

			int counter = exponent - 1;
			while (counter != 0)
			{
				result = ComplexMultivariatePolynomial.Multiply(result, poly);
				counter -= 1;
			}
			return new ComplexMultivariatePolynomial(result.Terms);
		}

		public static ComplexMultivariatePolynomial Divide(ComplexMultivariatePolynomial left, ComplexMultivariatePolynomial right)
		{
			List<Term> newTermsList = new List<Term>();
			List<Term> leftTermsList = CloneHelper<Term>.CloneCollection(left.Terms).ToList();

			foreach (Term rightTerm in right.Terms)
			{
				var matches = leftTermsList.Where(leftTerm => Term.ShareCommonFactor(leftTerm, rightTerm)).ToList();
				if (matches.Any())
				{
					foreach (Term matchTerm in matches)
					{
						leftTermsList.Remove(matchTerm);
						Term result = Term.Divide(matchTerm, rightTerm);
						if (result != Term.Empty)
						{
							if (!newTermsList.Any(lt => lt.Equals(result)))
							{
								newTermsList.Add(result);
							}
						}
					}
				}
				else
				{
					///newTermsList.Add(rightTerm);
				}
			}
			return new ComplexMultivariatePolynomial(newTermsList.ToArray());
		}

		#endregion

		#region Overrides and Interface implementations
		public ComplexMultivariatePolynomial Clone()
		{
			return new ComplexMultivariatePolynomial(CloneHelper<Term>.CloneCollection(Terms).ToArray());
		}

		public bool Equals(ComplexMultivariatePolynomial other)
		{
			return this.Equals(this, other);
		}

		public bool Equals(ComplexMultivariatePolynomial x, ComplexMultivariatePolynomial y)
		{
			if (x == null) { return (y == null) ? true : false; }
			if (!x.Terms.Any()) { return (!y.Terms.Any()) ? true : false; }
			if (x.Terms.Length != y.Terms.Length) { return false; }
			if (x.Degree != y.Degree) { return false; }

			int index = 0;
			foreach (Term term in x.Terms)
			{
				if (!term.Equals(y.Terms[index++])) { return false; }
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as ComplexMultivariatePolynomial);
		}

		public int GetHashCode(ComplexMultivariatePolynomial obj)
		{
			return obj.GetHashCode();
		}

		public override int GetHashCode()
		{
			int hashCode = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
			if (Terms.Any())
			{
				foreach (var term in Terms)
				{
					hashCode = Term.CombineHashCodes(hashCode, term.GetHashCode());
				}
			}
			return hashCode;
		}

		public override string ToString()
		{
			bool isFirstPass = true;
			string signString = string.Empty;
			string termString = string.Empty;
			string result = string.Empty;

			foreach (Term term in Terms)
			{
				signString = string.Empty;
				termString = string.Empty;

				if (isFirstPass)
				{
					isFirstPass = false;
				}
				else
				{
					if (Math.Sign(term.CoEfficient.Real) == -1)
					{
						signString = $" - ";
					}
					else if (Math.Sign(term.CoEfficient.Real) == 1)
					{
						signString = $" + ";
					}
				}

				termString = term.ToString();

				result += $"{signString}{termString}";
			}

			return result;
		}

		#endregion

	}
}
