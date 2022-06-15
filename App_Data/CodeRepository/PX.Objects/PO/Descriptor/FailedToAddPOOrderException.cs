using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO
{
	public class FailedToAddPOOrderException : PXException
	{
		public FailedToAddPOOrderException(string format, params object[] args)
			: base(format, args)
		{
		}

		public FailedToAddPOOrderException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}