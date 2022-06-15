using PX.Data;
using PX.Objects.CS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM.MaterialManagement
{    
    public class ProjectBalanceValidationMaterialExt : PXGraphExtension<ProjectBalanceValidation>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<CS.FeaturesSet.materialManagement>();
        }

		
		[PXOverride]
		public virtual void InitStockProc()
        {
			StockInitMaint graph = PXGraph.CreateInstance<StockInitMaint>();
			graph.Clear();
			graph.InitStock();
		}

	}
}
