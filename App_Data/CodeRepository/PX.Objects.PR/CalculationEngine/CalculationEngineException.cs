using System;

namespace PX.Objects.PR
{
	public class CalculationEngineException : Exception
	{
		public PRPayment Payment { get; set; }

		public CalculationEngineException(PRPayment payment, Exception inner) : base(inner.Message, inner)
		{
			Payment = payment;
		}
	}
}
