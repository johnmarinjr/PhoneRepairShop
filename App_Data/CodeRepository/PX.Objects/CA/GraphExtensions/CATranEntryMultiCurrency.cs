using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.Extensions.MultiCurrency;

namespace PX.Objects.CA.MultiCurrency
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public sealed class CATranEntryMultiCurrency : CAMultiCurrencyGraph<CATranEntry, CAAdj>
    {
        #region SettingUp
        protected override CurySourceMapping GetCurySourceMapping()
        {
            return new CurySourceMapping(typeof(CashAccount))
            {
                CuryID = typeof(CashAccount.curyID),
                CuryRateTypeID = typeof(CashAccount.curyRateTypeID)
            };
        }
        protected override DocumentMapping GetDocumentMapping()
        {
            return new DocumentMapping(typeof(CAAdj))
            {
                CuryID = typeof(CAAdj.curyID),
                CuryInfoID = typeof(CAAdj.curyInfoID),
                BAccountID = typeof(CAAdj.cashAccountID),
                DocumentDate = typeof(CAAdj.tranDate),
                BranchID = typeof(CAAdj.branchID)
            };
        }
        protected override PXSelectBase[] GetChildren()
        {
            return new PXSelectBase[]
            {
                Base.CAAdjRecords,
                Base.CASplitRecords,
                Base.Tax_Rows,
                Base.Taxes
            };
        }
        protected int? AccountProcessing;

		protected override string Module => GL.BatchModule.CA;

		protected override CurySource CurrentSourceSelect()
        {
            return CurySource.Select(AccountProcessing);
        }
        #endregion
        #region CAAdj

        protected void _(Events.FieldUpdated<CAAdj, CAAdj.cashAccountID> e)
        {
            SourceFieldUpdated<CAAdj.curyInfoID, CAAdj.curyID, CAAdj.tranDate>(e.Cache, e.Row);
        }

        protected void _(Events.FieldUpdated<CAAdj, CAAdj.tranDate> e)
        {
            CAAdj adj = e.Row as CAAdj;
            if (adj == null) return;

            DateFieldUpdated<CAAdj.curyInfoID, CAAdj.tranDate>(e.Cache, e.Row);

        }

        protected override void _(Events.RowSelected<CurrencyInfo> e)
        {
            if (e.Row != null)
            {
                // Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [the same code is specified in the base Events.RowSelected<CurrencyInfo>]
                e.Row.DisplayCuryID = e.Row.CuryID;

                long? baseCuryInfoID = CM.CurrencyCollection.MatchBaseCuryInfoId(e.Row);
                PXUIFieldAttribute.SetVisible<CurrencyInfo.curyRateTypeID>(e.Cache, e.Row, baseCuryInfoID == null);
                PXUIFieldAttribute.SetVisible<CurrencyInfo.curyEffDate>(e.Cache, e.Row, baseCuryInfoID == null);

                PXUIFieldAttribute.SetEnabled<CurrencyInfo.curyMultDiv>(e.Cache, e.Row, true);
                PXUIFieldAttribute.SetEnabled<CurrencyInfo.baseCuryID>(e.Cache, e.Row, false);

                PXUIFieldAttribute.SetEnabled<CurrencyInfo.displayCuryID>(e.Cache, e.Row, false);
                PXUIFieldAttribute.SetEnabled<CurrencyInfo.curyID>(e.Cache, e.Row, true);

                PXUIFieldAttribute.SetEnabled<CurrencyInfo.curyRateTypeID>(e.Cache, e.Row, true);
                PXUIFieldAttribute.SetEnabled<CurrencyInfo.curyEffDate>(e.Cache, e.Row, true);
                PXUIFieldAttribute.SetEnabled<CurrencyInfo.sampleCuryRate>(e.Cache, e.Row, true);
                PXUIFieldAttribute.SetEnabled<CurrencyInfo.sampleRecipRate>(e.Cache, e.Row, true);
            }
        }

        #endregion
        #region CASplit
        protected void _(Events.RowInserting<CASplit> e, PXRowInserting baseMethod)
		{
            UpdateNewTranDetailCuryTranAmtOrCuryUnitPrice(e.Cache, new CASplit(), e.Row);
            baseMethod(e.Cache, e.Args);
        }

        protected void _(Events.RowUpdating<CASplit> e, PXRowUpdating baseMethod)
        {
            UpdateNewTranDetailCuryTranAmtOrCuryUnitPrice(e.Cache, e.Row, e.NewRow);
            baseMethod(e.Cache, e.Args);
        }
        #endregion
    }
}