using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace PolynomialLibrary
{
	public static class BigIntegerExtensionMethods
	{
		public static Complex Clone(this Complex source)
		{
			return new Complex(source.Real, source.Imaginary);
		}
	}
}
