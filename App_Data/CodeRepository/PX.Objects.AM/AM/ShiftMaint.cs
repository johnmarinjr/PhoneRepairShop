using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AM.Attributes;
using PX.Objects.AM.CacheExtensions;
using PX.Objects.EP;
using System;
using System.Linq;

namespace PX.Objects.AM
{
	/// <summary>
	/// Manufacturing Shift Maintenance
	/// </summary>
	public class ShiftMaint : PXGraph<ShiftMaint>
    {
		public PXSave<EPShiftCode> Save;
        public PXCancel<EPShiftCode> Cancel;

        #region Views
        public SelectFrom<EPShiftCode>
            .Where<EPShiftCode.isManufacturingShift.IsEqual<True>>.View ShiftRecords;
        public ShiftCodeRateQuery Rates;
        public PXSetup<AMBSetup> ambsetup;
        #endregion Views

        #region Cache attached
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [PXCustomizeBaseAttribute(typeof(PXDefaultAttribute), nameof(PXDefaultAttribute.Constant), true)]
        public virtual void _(Events.CacheAttached<EPShiftCode.isManufacturingShift> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Shift")]
		public virtual void _(Events.CacheAttached<EPShiftCode.shiftCD> e) { }
		#endregion Cache attached

		#region Events
		protected virtual void _(Events.RowInserting<EPShiftCode> e)
		{
            if (e.Row == null)
			{
                return;
			}

            EPShiftCodeRate rate = new EPShiftCodeRate()
            {
                ShiftID = e.Row.ShiftID,
                EffectiveDate = new DateTime(1900, 1, 1),
				WageAmount = 0,
				CostingAmount = 0
            };
			rate = Rates.Insert(rate);
			DisplayShiftDiffAndType(e.Cache, e.Row);
		}

        protected virtual void _(Events.RowSelecting<EPShiftCode> e)
        {
            if (e.Row == null)
            {
                return;
            }

            using (new PXConnectionScope())
            {
				DisplayShiftDiffAndType(e.Cache, e.Row);
			}
		}

        protected virtual void _(Events.RowUpdating<EPShiftCode> e)
		{
			if (e.NewRow == null)
			{
				return;
			}

			EPShiftCodeRate rate = Rates.SelectSingle(e.Row.ShiftID);
			EPShiftCodeExt newRowExt = PXCache<EPShiftCode>.GetExtension<EPShiftCodeExt>(e.NewRow);
			if (Equals(newRowExt.DiffType, ShiftDiffType.Rate))
			{
				rate.Type = EPShiftCodeType.Percent;
				rate.Percent = newRowExt.ShftDiff;
				rate.WageAmount = null;
				rate.CostingAmount = null;
			}
			else if (Equals(newRowExt.DiffType, ShiftDiffType.Amount))
			{
				rate.Type = EPShiftCodeType.Amount;
				rate.Percent = null;
				rate.WageAmount = newRowExt.ShftDiff;
				rate.CostingAmount = newRowExt.ShftDiff;
			}
			else
			{
				rate.Type = null;
				rate.Percent = null;
				rate.WageAmount = null;
				rate.CostingAmount = null;
			}

			Rates.Update(rate);
		}

        protected virtual void _(Events.RowPersisting<EPShiftCode> e)
		{
            if (e.Operation.Command() == PXDBOperation.Delete)
			{
                return;
			}

            EPShiftCodeExt rowExt = PXCache<EPShiftCode>.GetExtension<EPShiftCodeExt>(e.Row);
            if (string.IsNullOrEmpty(rowExt.DiffType))
			{
                e.Cache.RaiseExceptionHandling<EPShiftCodeExt.diffType>(e.Row, null,
                    new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<EPShiftCodeExt.diffType>(e.Cache)));
			}
            if (rowExt.ShftDiff == null)
            {
                e.Cache.RaiseExceptionHandling<EPShiftCodeExt.shftDiff>(e.Row, null,
                    new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<EPShiftCodeExt.shftDiff>(e.Cache)));
            }
            if (rowExt.AMCrewSize == null)
            {
                e.Cache.RaiseExceptionHandling<EPShiftCodeExt.amCrewSize>(e.Row, null,
                    new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<EPShiftCodeExt.amCrewSize>(e.Cache)));
            }
        }

        protected virtual void _(Events.FieldUpdating<EPShiftCode.shiftCD> e)
		{
            if (SelectFrom<EPShiftCode>
                .Where<EPShiftCode.isManufacturingShift.IsEqual<False>
                    .And<EPShiftCode.shiftCD.IsEqual<P.AsString>>>.View.Select(this, e.NewValue).Any())
			{
                throw new PXSetPropertyException<EPShiftCode.shiftCD>(Messages.ShiftCodeExistsInTime);
			}
		}
        #endregion Events

        #region Helpers
		protected virtual void DisplayShiftDiffAndType(PXCache cache, EPShiftCode row)
		{
			(decimal? shftDiff, string diffType) = GetShiftDiffAndType(this, row);
			cache.SetValue<EPShiftCodeExt.diffType>(row, diffType);
			cache.SetValue<EPShiftCodeExt.shftDiff>(row, shftDiff);
		}

        public static (decimal?, string) GetShiftDiffAndType(PXGraph graph, EPShiftCode shiftCode)
        {
			if (shiftCode != null)
			{
				EPShiftCodeRate rate = new ShiftCodeRateQuery(graph).SelectSingle(shiftCode.ShiftID);
				if (rate?.Type == EPShiftCodeType.Percent)
				{
					return (rate.Percent, ShiftDiffType.Rate);
				}
				else if (rate?.Type == EPShiftCodeType.Amount)
				{
					return (rate.CostingAmount, ShiftDiffType.Amount);
				} 
			}

            return (null, null);
        }

		public class ShiftCodeRateQuery : SelectFrom<EPShiftCodeRate>
            .Where<EPShiftCodeRate.shiftID.IsEqual<EPShiftCode.shiftID.AsOptional>>
            .OrderBy<EPShiftCodeRate.effectiveDate.Desc>.View
		{
            public ShiftCodeRateQuery(PXGraph graph) : base(graph) { }
		}
        #endregion Helpers
    }
}
