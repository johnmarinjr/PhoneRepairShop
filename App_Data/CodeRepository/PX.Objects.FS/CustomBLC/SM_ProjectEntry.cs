using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CM.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.IN;
using PX.SM;
using System;
using System.Collections;
using System.Linq;
using PX.Objects.CR.Workflows;
using System.Collections.Generic;
using PX.Objects.PM;

namespace PX.Objects.FS
{
    public class SM_ProjectEntry : PXGraphExtension<ProjectEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        [PXHidden]
        public PXSelect<FSSetup> SetupRecord;

        [PXHidden]
        public PXSetup<PMSetup> PMSetupRecord;
    }
}