using PX.Commerce.Core;
using PX.Data;
using PX.Objects.SO;
using System;

namespace PX.Commerce.Objects
{
	[Serializable]
	public sealed class BCSOShipmentExt : PXCacheExtension<SOShipment>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		#region ShipmentUpdated
		public abstract class externalShipmentUpdated : PX.Data.BQL.BqlBool.Field<externalShipmentUpdated> { }
		[PXDBBool()]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public  Boolean? ExternalShipmentUpdated { get; set; }
		#endregion
	}
}
