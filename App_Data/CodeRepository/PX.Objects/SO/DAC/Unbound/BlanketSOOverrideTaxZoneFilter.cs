using PX.Data;
using PX.Objects.TX;
using System;

namespace PX.Objects.SO.DAC.Unbound
{
	[PXCacheName(Messages.BlanketSOOverrideTaxZoneFilter)]
	public class BlanketSOOverrideTaxZoneFilter : IBqlTable
	{
		#region TaxZoneID
		public abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }
		protected String _TaxZoneID;
		[PXString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Customer Tax Zone", Visibility = PXUIVisibility.Visible)]
		[PXSelector(typeof(TaxZone.taxZoneID), DescriptionField = typeof(TaxZone.descr), Filterable = true)]
		[PXRestrictor(typeof(Where<TaxZone.isManualVATZone, Equal<False>>), TX.Messages.CantUseManualVAT)]
		public virtual String TaxZoneID
		{
			get
			{
				return this._TaxZoneID;
			}
			set
			{
				this._TaxZoneID = value;
			}
		}
		#endregion
	}
}
