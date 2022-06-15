using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static PX.Objects.PO.POCreate;
using CRLocation = PX.Objects.CR.Standalone.Location;
using PX.Objects.Common.DAC;

namespace PX.Objects.FS
{
    public class SM_POCreate : PXGraphExtension<POCreate>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Initialize()
        {
            base.Initialize();

            Base.FixedDemand.Join<LeftJoin<FSServiceOrder,
                On<FSServiceOrder.noteID, Equal<POFixedDemand.refNoteID>>>>();

            Base.FixedDemand.Join<LeftJoin<FSSODetFSSODetSplit,
                On<FSSODetFSSODetSplit.planID, Equal<POFixedDemand.planID>,
                And<FSSODetFSSODetSplit.srvOrdType, Equal<FSServiceOrder.srvOrdType>,
                And<FSSODetFSSODetSplit.refNbr, Equal<FSServiceOrder.refNbr>>>>>>();

            Base.FixedDemand.WhereAnd<Where<
                Where2<Where<FSServiceOrder.customerID, Equal<Current<POCreateFilter.customerID>>, Or<Current<POCreateFilter.customerID>, IsNull, Or<FSServiceOrder.refNbr, IsNull>>>,
                And2<Where<FSServiceOrder.srvOrdType, Equal<Current<FSxPOCreateFilter.srvOrdType>>, Or<Current<FSxPOCreateFilter.srvOrdType>, IsNull>>,
                And<Where<FSServiceOrder.refNbr, Equal<Current<FSxPOCreateFilter.serviceOrderRefNbr>>, Or<Current<FSxPOCreateFilter.serviceOrderRefNbr>, IsNull>>>>>>>();
        }

        public PXAction<POCreateFilter> viewDocument;

        /// <summary>
        /// Overrides <see cref="POCreate.GetFixedDemandFieldScope"/>
        /// </summary>
        [PXOverride]
        public virtual IEnumerable<Type> GetFixedDemandFieldScope(Func<IEnumerable<Type>> baseFunc)
        {
            foreach(Type r in baseFunc())
            {
                yield return r;
            }
            yield return typeof(FSServiceOrder.srvOrdType);
            yield return typeof(FSServiceOrder.refNbr);
            yield return typeof(FSServiceOrder.customerID);
			yield return typeof(FSServiceOrder.projectID);
			yield return typeof(FSServiceOrder.noteID);
			yield return typeof(FSSODetFSSODetSplit);
        }

		/// <summary>
		/// Overrides <see cref="POCreate.EnumerateAndPrepareFixedDemands(PXResultset&lt;POFixedDemand&gt;)"/>
		/// </summary>
		[PXOverride]
        public virtual IEnumerable EnumerateAndPrepareFixedDemands(PXResultset<POFixedDemand> fixedDemands,
            Func<PXResultset<POFixedDemand>, IEnumerable> baseFunc)
        {
            List<long> planIDList = GetServicePlanIDList();

            foreach (PXResult<POFixedDemand> rec in baseFunc(fixedDemands))
            {
                PrepareFixedDemandFieldServiceRow(
                    (POFixedDemand)rec,
                    PXResult.Unwrap<FSServiceOrder>(rec),
                    PXResult.Unwrap<SOOrder>(rec),
                    PXResult.Unwrap<SOLine>(rec),
                    PXResult.Unwrap<FSSODetFSSODetSplit>(rec),
                    planIDList);

                yield return rec;
            }
        }

        public virtual void PrepareFixedDemandFieldServiceRow(POFixedDemand demand, FSServiceOrder fsServiceOrderRow, SOOrder soOrderRow, SOLine soLineRow, FSSODetFSSODetSplit fsSODetRow, List<long> planIDList)
        {
            if (fsServiceOrderRow != null
                && string.IsNullOrEmpty(fsServiceOrderRow.RefNbr) == false)
            {
                soOrderRow.CustomerID = fsServiceOrderRow.CustomerID;

                if (fsSODetRow != null
                    && string.IsNullOrEmpty(fsSODetRow.RefNbr) == false)
                {
                    soLineRow.UnitPrice = fsSODetRow.UnitPrice;
                    soLineRow.UOM = fsSODetRow.UOM;
                }

                FSxPOFixedDemand fSxPOFixedDemandRow = Base.FixedDemand.Cache.GetExtension<FSxPOFixedDemand>(demand);
                fSxPOFixedDemandRow.FSRefNbr = fsServiceOrderRow.RefNbr;
                demand.ProjectID = fsServiceOrderRow.ProjectID;

                if (planIDList != null
                    && planIDList.Count > 0
                    && planIDList.Contains((long)demand.PlanID) == true)
                {
                    demand.Selected = true;
                }
            }
        }

        public List<long> GetServicePlanIDList()
        {
            if (Base.Filter.Current != null)
            {
                FSxPOCreateFilter filterExt = Base.Filter.Cache.GetExtension<FSxPOCreateFilter>(Base.Filter.Current);

                if (string.IsNullOrEmpty(filterExt.AppointmentRefNbr) == false
                    && string.IsNullOrEmpty(filterExt.SrvOrdType) == false)
                {
                    return SelectFrom<FSSODetSplit>
                                    .InnerJoin<FSAppointmentDet>
                                        .On<FSAppointmentDet.srvOrdType.IsEqual<FSSODetSplit.srvOrdType>
                                            .And<FSAppointmentDet.origSrvOrdNbr.IsEqual<FSSODetSplit.refNbr>>
                                            .And<FSAppointmentDet.origLineNbr.IsEqual<FSSODetSplit.lineNbr>>>
                            .Where<FSAppointmentDet.srvOrdType.IsEqual<@P.AsString>
                                .And<FSAppointmentDet.refNbr.IsEqual<@P.AsString>>
								.And<FSSODetSplit.completed.IsEqual<False>>>
                            .View.Select(Base, filterExt.SrvOrdType, filterExt.AppointmentRefNbr).RowCast<FSSODetSplit>().Select(x => (long)x.PlanID).ToList();
                }
            }

            return null;
        }

        [PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXEditDetailButton]
        public virtual IEnumerable ViewDocument(PXAdapter adapter)
        {
            POFixedDemand line = Base.FixedDemand.Current;
            if (line == null || line.RefNoteID == null) return adapter.Get();

            FSServiceOrder doc = PXSelect<FSServiceOrder, Where<FSServiceOrder.noteID, Equal<Required<POFixedDemand.refNoteID>>>>.Select(Base, line.RefNoteID);

            if (doc != null)
            {
                ServiceOrderEntry graph = PXGraph.CreateInstance<ServiceOrderEntry>();
                graph.ServiceOrderRecords.Current = graph.ServiceOrderRecords.Search<FSServiceOrder.refNbr>
                                                                                                (doc.RefNbr, doc.SrvOrdType);
                PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.NewWindow);
                return adapter.Get();
            }
            else
            {
                return Base.viewDocument.Press(adapter);
            }
        }
    }
}