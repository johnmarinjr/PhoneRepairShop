using System.Collections;
using PX.Objects.Localizations.GB.HMRC.DAC;


namespace PX.Objects.Localizations.GB.HMRC.Attributes
{
	public class ObligationSelectorAttribute : PX.Data.PXCustomSelectorAttribute
	{
		public ObligationSelectorAttribute()
		: base(typeof(Obligation.periodKey),
			 typeof(Obligation.periodKey),
			 typeof(Obligation.start),
			 typeof(Obligation.end),
			 typeof(Obligation.due),
			 typeof(Obligation.status),
			 typeof(Obligation.received))
		{
		}

		public virtual IEnumerable GetRecords()
		{
			return ((VATMaint)_Graph).obligations();
		}
	}
}
