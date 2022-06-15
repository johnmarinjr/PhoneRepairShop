using PX.Data;
using PX.Objects.PM;
using System.Linq;

namespace PX.Objects.PR
{
	public class MaximumInsurableWageWorkCodeAttribute : PMWorkCodeAttribute
	{
		public MaximumInsurableWageWorkCodeAttribute()
		{
			FieldClass = null;
			DisplayName = "WC Code";

			PXEventSubscriberAttribute activeRestrictor = _Attributes.FirstOrDefault(x => x is PXRestrictorAttribute);
			if (activeRestrictor != null)
			{
				_Attributes.Remove(activeRestrictor);
			}
		}
	}
}
