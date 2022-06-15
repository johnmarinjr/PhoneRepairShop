using PX.Data;
using PX.Objects.AR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.RelatedItems
{
    public abstract class ValidateRequiredRelatedItems<TGraph, TSubstitutableDocument, TSubstitutableLine> : PXGraphExtension<TGraph> 
        where TGraph : PXGraph
        where TSubstitutableDocument : class, IBqlTable, ISubstitutableDocument, new()
        where TSubstitutableLine : class, IBqlTable, ISubstitutableLine, new()
    {
        protected virtual bool IsMassProcessing => PXLongOperation.GetCustomInfoForCurrentThread(PXProcessing.ProcessingKey) != null;

        public virtual bool Validate(TSubstitutableLine substitutableLine)
        {
            if (SubstitutionRequired(substitutableLine))
            {
                ThrowError();
                return false;
            }
            return true;
        }

        protected virtual bool SubstitutionRequired(TSubstitutableLine substitutableLine) => substitutableLine.SubstitutionRequired == true;

        public abstract void ThrowError();
    }
}
