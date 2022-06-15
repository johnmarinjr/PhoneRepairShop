using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;
using PX.Objects.PO;
using Messages = PX.Objects.CN.Subcontracts.AP.Descriptor.Messages;

namespace PX.Objects.CN.Subcontracts.AP.CacheExtensions
{
    public sealed class ApTranExt : PXCacheExtension<APTran>
    {
        [PXString(15, IsUnicode = true)]
        [PXUIField(DisplayName = Messages.Subcontract.SubcontractNumber, Enabled = false, IsReadOnly = true)]
        public string SubcontractNbr =>
            Base.POOrderType == POOrderType.RegularSubcontract
                ? Base.PONbr
                : null;

        [PXInt]
        [PXUIField(DisplayName = Messages.Subcontract.SubcontractLine, Visible = false)]
        [PXSelector(typeof(Search<POLine.lineNbr, Where<POLine.orderType, Equal<Current<APTran.pOOrderType>>,
            And<POLine.orderNbr, Equal<Current<APTran.pONbr>>>>>),
            typeof(POLine.lineNbr), typeof(POLine.projectID), typeof(POLine.taskID), typeof(POLine.costCodeID),
            typeof(POLine.inventoryID), typeof(POLine.lineType), typeof(POLine.tranDesc), typeof(POLine.uOM),
            typeof(POLine.orderQty), typeof(POLine.curyUnitCost), typeof(POLine.curyExtCost))]
        public int? SubcontractLineNbr
        {
            get
            {
                return Base.POOrderType == POOrderType.RegularSubcontract
               ? Base.POLineNbr
               : null;
            }
            set
            {
                Base.POLineNbr = value;
            }
        }
           

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.construction>();
        }

        public abstract class subcontractNbr : IBqlField
        {
        }

        public abstract class subcontractLineNbr : IBqlField
        {
        }
    }
}