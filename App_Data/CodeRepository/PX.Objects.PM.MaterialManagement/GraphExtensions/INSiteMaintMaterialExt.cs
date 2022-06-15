using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
    public class INSiteMaintMaterialExt : PXGraphExtension<INSiteMaint>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<CS.FeaturesSet.materialManagement>();
        }

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [PXRestrictor(typeof(Where<PMProject.accountingMode, Equal<ProjectAccountingModes.linked>>), PM.Messages.InvalidProject_AccountingMode, typeof(PMProject.contractCD))]
        protected virtual void _(Events.CacheAttached<INLocation.projectID> e) { }
    }
}
