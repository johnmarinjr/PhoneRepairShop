using System;
using System.Runtime.Serialization;
using PX.Data;

namespace PX.Objects.DR
{
	public class ScheduleCuryTotalAmtLessOrEqualZeroException : PXException
	{
		public ScheduleCuryTotalAmtLessOrEqualZeroException(string message) : base(message) { }

		public ScheduleCuryTotalAmtLessOrEqualZeroException(string format, params object[] args) : base(format, args) { }

		public ScheduleCuryTotalAmtLessOrEqualZeroException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

}
