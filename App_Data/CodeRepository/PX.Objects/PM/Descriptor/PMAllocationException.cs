using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
	public class PMAllocationException : PXException
	{
		public string RefNbr { get; private set; }
		public PMAllocationException(string refNbr) :
				base(Messages.AutoAllocationFailed)
		{
			RefNbr = refNbr;
		}

		public PMAllocationException(SerializationInfo info, StreamingContext context) :
			base(info, context)
		{ }
	}
}
