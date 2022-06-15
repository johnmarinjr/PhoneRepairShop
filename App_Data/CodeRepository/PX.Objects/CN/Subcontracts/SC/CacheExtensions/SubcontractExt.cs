using System;
using PX.Data;
using PX.Objects.CN.Subcontracts.SC.DAC;
using PX.Objects.CS;
using PX.Objects.PO;

namespace PX.Objects.CN.Subcontracts.SC.CacheExtensions
{
    public sealed class SubcontractExt : PXCacheExtension<Subcontract>
    {
	    public static bool IsActive()
	    {
		    return PXAccess.FeatureInstalled<FeaturesSet.construction>();
	    }

		[PXMergeAttributes(Method = MergeMethod.Append)]
        [PXRemoveBaseAttribute(typeof(PXNoteAttribute))]
        [PXNote(ShowInReferenceSelector = true,
            Selector = typeof(Search<POOrder.noteID,
                Where<POOrder.orderType, Equal<POOrderType.regularSubcontract>>>))]
        public Guid? NoteID
        {
            get;
            set;
        }
    }
}
