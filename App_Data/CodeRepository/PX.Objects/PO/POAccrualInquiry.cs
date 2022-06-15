using PX.Common;    
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.Objects.IN;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO
{
    [TableAndChartDashboardType]
    public class POAccrualInquiry : PXGraph<POAccrualInquiry>
    {
        [InjectDependency]
        public IFinPeriodRepository FinPeriodRepository { get; set; }

        protected Lazy<IEnumerable<POAccrualInquiryResult>> RecordsCache;

        public override bool IsDirty => false;

        public POAccrualInquiry()
        {
            this.Caches<POAccrualInquiryFilter>();
            
            ResultRecords.Cache.AllowInsert = false;
            ResultRecords.Cache.AllowUpdate = false;
            ResultRecords.Cache.AllowDelete = false;

            InitializeRecordsCache();
        }

        #region Views

        public PXFilter<POAccrualInquiryFilter> Filter;

        protected virtual IEnumerable filter()
        {
            var cache = Filter.Cache;
            var filter = cache.Current as POAccrualInquiryFilter;
            if (filter != null)
            {
                ClearSummary(filter);

                if (!IsEmptyFilter(filter))
                {
                    var records = ResultRecords.Select()
                        .RowCast<POAccrualInquiryResult>()
                        .ToArray();
                    records.ForEach(record => AggregateSummary(filter, record));
                }
            }
            cache.IsDirty = false;
            yield return filter;
        }

        [PXFilterable]
        public PXSelect<POAccrualInquiryResult> ResultRecords;

        protected virtual IEnumerable resultRecords()
        {
            return RecordsCache.Value;
        }

        #endregion

        #region Actions

        public PXAction<POAccrualInquiryFilter> viewDocument;

        [PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
        [PXEditDetailButton(ImageKey = Web.UI.Sprite.Main.DataEntry)]
        public virtual IEnumerable ViewDocument(PXAdapter adapter)
        {
            var row = ResultRecords.Current;
            if(row != null)
            {
                switch (row.DocumentType)
                {
                    case POAccrualInquiryResult.documentType.Receipt:
                    case POAccrualInquiryResult.documentType.Return:
                        var receiptGraph = CreateInstance<POReceiptEntry>();
                        receiptGraph.Document.Current = receiptGraph.Document.Search<POReceipt.receiptNbr>(row.POReceiptNbr, row.POReceiptType);
                        throw new PXRedirectRequiredException(receiptGraph, true, Messages.POReceipt) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                    
                    case POAccrualInquiryResult.documentType.Bill:
                    case POAccrualInquiryResult.documentType.DebitAdj:
                        var invoiceGraph = CreateInstance<APInvoiceEntry>();
                        invoiceGraph.Document.Current = invoiceGraph.Document.Search<APInvoice.refNbr>(row.APRefNbr, row.APDocType);
                        throw new PXRedirectRequiredException(invoiceGraph, true, AP.Messages.APInvoice) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
            }
            return adapter.Get<POAccrualInquiryFilter>();
        }

        public PXAction<POAccrualInquiryFilter> refreshAll;

        [PXUIField(DisplayName = "", Visible = true)]
        [PXButton(ImageKey = Web.UI.Sprite.Main.Refresh, Tooltip = IN.Messages.ttipRefresh, SpecialType = PXSpecialButtonType.Refresh)]
        public virtual IEnumerable RefreshAll(PXAdapter adapter)
        {
            Filter.Cache.Current = (POAccrualInquiryFilter)Filter.Select();
            ResultRecords.View.RequestRefresh();
            return adapter.Get();
        }

        public PXCancel<POAccrualInquiryFilter> Cancel;

        public PXAction<POAccrualInquiryFilter> previousPeriod;

        [PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXPreviousButton]
        public virtual IEnumerable PreviousPeriod(PXAdapter adapter)
        {
            var filter = Filter.Current;

            int? calendarOrganizationID = FinPeriodRepository.GetCalendarOrganizationID(filter.OrganizationID, filter.BranchID, false);
            FinPeriod prevPeriod = FinPeriodRepository.FindPrevPeriod(calendarOrganizationID, filter.FinPeriodID, looped: true);
            if (prevPeriod != null)
            {
                filter.FinPeriodID = prevPeriod.FinPeriodID;
                ResetCaches();
            }

            return adapter.Get();
        }

        public PXAction<POAccrualInquiryFilter> nextPeriod;

        [PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXNextButton]
        public virtual IEnumerable NextPeriod(PXAdapter adapter)
        {
            var filter = Filter.Current;

            int? calendarOrganizationID = FinPeriodRepository.GetCalendarOrganizationID(filter.OrganizationID, filter.BranchID, false);
            FinPeriod nextPeriod = FinPeriodRepository.FindNextPeriod(calendarOrganizationID, filter.FinPeriodID, looped: true);
            if (nextPeriod != null)
            {
                filter.FinPeriodID = nextPeriod.FinPeriodID;
                ResetCaches();
            }
            return adapter.Get();
        }

        public PXAction<POAccrualInquiryFilter> openReleaseINDocuments;

        [PXUIField(DisplayName = "View Unreleased IN Documents", Visible = true)]
        [PXButton]
        public virtual IEnumerable OpenReleaseINDocuments(PXAdapter adapter)
        {
            var filter = Filter.Current;

            var releaseGraph = CreateInstance<INDocumentRelease>();
            var ex = new PXRedirectRequiredException(releaseGraph, true, AP.Messages.APInvoice) 
            {
                Mode = PXBaseRedirectException.WindowMode.New
            };
            if (filter.NotAdjustedAmt != 0)
            {
                var gridFilters = new PXBaseRedirectException.Filter(
                    nameof(releaseGraph.INDocumentList),
                    new[]
                    {
                    new PXFilterRow(nameof(INRegister.origModule), PXCondition.EQ, BatchModule.AP),
                    new PXFilterRow(nameof(INRegister.docType), PXCondition.EQ, INDocType.Adjustment)
                    });

                ex.Filters.Add(gridFilters);
            }
            throw ex;
        }

        #endregion

        #region Records loading

        protected virtual bool IsEmptyFilter(POAccrualInquiryFilter filter)
            => filter?.OrgBAccountID == null
                || filter.FinPeriodID == null
                || filter.AcctID == null;

        protected virtual IEnumerable<POAccrualInquiryResult> LoadRecords()
        {
            var filter = Filter.Current;

            if (IsEmptyFilter(filter))
                return Array<POAccrualInquiryResult>.Empty;
            
            var query = GetQuery(filter);
            var records = query.SelectMain();
            
            return records;
        }

        protected virtual PXSelectBase<POAccrualInquiryResult> GetQuery(POAccrualInquiryFilter filter)
        {
            PXSelectBase<POAccrualInquiryResult> query;
            if (filter.ShowByLines == true)
            {
                query = new SelectFrom<POAccrualInquiryResult>
                    .View(this);
            }
            else
            {
                query = new SelectFrom<POAccrualInquiryResult>
                    .Aggregate<To<
                        GroupBy<POAccrualInquiryResult.documentNoteID>,
                        GroupBy<POAccrualInquiryResult.subID>,
                        Sum<POAccrualInquiryResult.accruedCost>,
                        Sum<POAccrualInquiryResult.pPVAmt>,
                        Sum<POAccrualInquiryResult.accruedCostTotal>,
                        Sum<POAccrualInquiryResult.taxAdjAmt>,
                        Sum<POAccrualInquiryResult.accruedByReceiptsCost>,
                        Sum<POAccrualInquiryResult.accruedByReceiptsPPVAmt>,
                        Sum<POAccrualInquiryResult.accruedByReceiptsTotal>,
                        Sum<POAccrualInquiryResult.accruedByBillsTotal>,
                        Min<POAccrualInquiryResult.pPVAdjPosted>,
                        Min<POAccrualInquiryResult.taxAdjPosted>>>
                    .View(this);
            }
            return query;
        }

        #endregion

        #region Records cache

        protected virtual void InitializeRecordsCache()
        {
            RecordsCache = new Lazy<IEnumerable<POAccrualInquiryResult>>(LoadRecords);
        }

        #endregion

        #region Summary

        protected virtual void ClearSummary(POAccrualInquiryFilter filter)
        {
            filter.UnbilledAmt = 0;
            filter.NotReceivedAmt = 0;
            filter.NotInvoicedAmt = 0;
            filter.NotAdjustedAmt = 0;
            filter.Balance = 0;
        }

        protected virtual void AggregateSummary(POAccrualInquiryFilter filter, POAccrualInquiryResult record)
        {
            filter.UnbilledAmt += record.UnbilledAmt;
            filter.NotReceivedAmt += record.NotReceivedAmt;
            filter.NotInvoicedAmt += record.NotInvoicedAmt;
            filter.NotAdjustedAmt += record.NotAdjustedAmt;
            filter.Balance += record.AccrualAmt;
        }

        #endregion

        #region Event handlers

        protected virtual void _(Events.FieldDefaulting<POAccrualInquiryFilter.acctID> e)
        {
            Account[] poAccounts = SelectFrom<Account>
                .Where<Account.controlAccountModule.IsEqual<ControlAccountModule.pO>
                    .And<Match<Current<AccessInfo.userName>>>>
                .View
                .ReadOnly
                .SelectWindowed(this, 0, 2)
                .RowCast<Account>()
                .ToArray();
            if (poAccounts.Length == 1)
                e.NewValue = poAccounts[0].AccountID;
        }

        protected virtual void _(Events.RowInserted<POAccrualInquiryFilter> e)
        {
            ResetCaches();
        }

        protected virtual void _(Events.RowSelected<POAccrualInquiryFilter> e)
        {
            if (e.Row == null)
                return;

            var showByLines = e.Row.ShowByLines == true;
            var rowsCache = ResultRecords.Cache;

            rowsCache
                .Adjust<PXUIFieldAttribute>()
                .For<POAccrualInquiryResult.siteID>(a =>
                {
                    a.Visible = showByLines;
                })
                .SameFor<POAccrualInquiryResult.inventoryID>()
                .SameFor<POAccrualInquiryResult.tranDesc>();
        }

        protected virtual void _(Events.RowUpdated<POAccrualInquiryFilter> e)
        {
            if (!e.Cache.ObjectsEqual<
                POAccrualInquiryFilter.orgBAccountID,
                POAccrualInquiryFilter.vendorID,
                POAccrualInquiryFilter.finPeriodID,
                POAccrualInquiryFilter.acctID,
                POAccrualInquiryFilter.subCD,
                POAccrualInquiryFilter.showByLines>(e.OldRow, e.Row))
            {
                ResetCaches();
            }
        }

        protected virtual void _(Events.RowSelected<POAccrualInquiryResult> e)
        {
            if (e.Row == null)
                return;
            ResultRecords.Cache.RaiseExceptionHandling<POAccrualInquiryResult.pPVAdjRefNbr>(e.Row, e.Row.PPVAdjRefNbr,
                e.Row.PPVAdjRefNbr != null && e.Row.PPVAdjPosted != true ? new PXSetPropertyException(Messages.AdjustmentHasNotBeenReleased, PXErrorLevel.Warning, e.Row.PPVAdjRefNbr) : null);
            ResultRecords.Cache.RaiseExceptionHandling<POAccrualInquiryResult.taxAdjRefNbr>(e.Row, e.Row.TaxAdjRefNbr,
                e.Row.TaxAdjRefNbr != null && e.Row.TaxAdjPosted != true ? new PXSetPropertyException(Messages.AdjustmentHasNotBeenReleased, PXErrorLevel.Warning, e.Row.TaxAdjRefNbr) : null);
        }

        #endregion

        protected virtual void ResetCaches()
        {
            ResultRecords.Cache.Clear();
            ResultRecords.Cache.ClearQueryCache();

            InitializeRecordsCache();
        }
    }

}
