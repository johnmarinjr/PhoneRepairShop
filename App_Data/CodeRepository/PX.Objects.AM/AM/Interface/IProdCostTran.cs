using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.AM
{
	public interface IProdCostTran : IProdOper
	{
		string DocType { get; set; }
		string TranType { get; set; }
		int? SubcontractSource { get; set; }
		bool? IsScrap { get; set; }
		bool? IsByproduct { get; set; }
		bool? LastOper { get; set; }
		Decimal? TranAmt { get; set; }
		Int32? LaborTime { get; set; }
		Decimal? BaseQty { get; set; }
		Int32? LineNbr { get; set; }
	}
}
