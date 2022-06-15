using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;
using PX.Objects.IN;
using System;

namespace PX.Objects.IN.GraphExtensions.INReplenishmentCreateExt
{
	/// <exclude/>
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class RoundUpDecimalQtyOnNonDivisibleItemExt : PXGraphExtension<INReplenishmentCreate>
	{
		public override void Initialize()
		{
			base.Initialize();
		}

		#region Overrides

		[PXOverride]
		public virtual decimal? RecalcQty(INReplenishmentItem rec, Func<INReplenishmentItem, decimal?> baseMethod)
		{
			rec.QtyProcess = baseMethod(rec);

			if (rec.QtyProcessRounded != true && rec.DecimalBaseUnit == false && rec.QtyProcess % 1 != 0)
			{
				rec.QtyProcess = Math.Ceiling(rec.QtyProcess.Value);
				rec.QtyProcessRounded = true;
			}

			return rec.QtyProcess;
		}

		[PXOverride]
		public virtual bool ManageQtyProcessRoundedWarning(INReplenishmentItem rec, Func<INReplenishmentItem, bool> baseMethod)
		{
			if (rec.QtyProcessRounded == true)
			{
				Base.Records.Cache.RaiseExceptionHandling<INReplenishmentItem.qtyProcess>(rec, rec.QtyProcess,
																		  new PXSetPropertyException<INReplenishmentItem.qtyProcess>(
																			  IN.Messages.IndivisibleBaseUOMRounded, PXErrorLevel.Warning, rec.InventoryCD));
				return true;

			}
			else
			{
				if (PXUIFieldAttribute.GetErrorWithLevel<INReplenishmentItem.qtyProcess>(Base.Records.Cache, rec).errorLevel == PXErrorLevel.Warning)
				{
					Base.Records.Cache.RaiseExceptionHandling<INReplenishmentItem.qtyProcess>(rec, rec.QtyProcess, null);
				}

				return false;
			}
		}

		#endregion

		#region Events

		public virtual void _(Events.FieldUpdating<INReplenishmentItem.qtyProcess> e)
		{
			INReplenishmentItem row = (INReplenishmentItem)e.Row;

			if (row != null && e.NewValue != null)
			{
				decimal qtyProcessRounded = (decimal)e.NewValue;

				if (row.DecimalBaseUnit == false && qtyProcessRounded % 1 != 0)
				{
					e.NewValue = Math.Ceiling(qtyProcessRounded);
					row.QtyProcessRounded = true;
				}
				else
				{
					row.QtyProcessRounded = false;
				}
			}
		}

		#endregion
	}
}
