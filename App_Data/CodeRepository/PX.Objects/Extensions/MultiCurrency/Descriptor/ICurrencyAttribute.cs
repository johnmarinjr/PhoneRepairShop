using System;
using PX.Data;

namespace PX.Objects.CM.Extensions
{
	public interface ICurrencyAttribute
	{
		int? CustomPrecision { get; }
		string FieldName { get; }
		Type ResultField { get; }
		Type KeyField { get; }
		bool BaseCalc { get; }
	}
}
