using System;
using PX.Data;
using PX.Data.EP;
using PX.Objects.CS;
using PX.Objects.CT;

namespace SP.Objects.CR
{
    // Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
    public class ContractTemplateExt : PXCacheExtension<ContractTemplate>
    {
        [PXDimensionSelector(ContractTemplateAttribute.DimensionName, typeof(Search<ContractTemplate.contractCD, Where<ContractTemplate.isTemplate, Equal<boolTrue>, And<ContractTemplate.baseType, Equal<Contract.ContractBaseType>>>>), typeof(ContractTemplate.contractCD), DescriptionField = typeof(ContractTemplate.description))]
        [PXDBString(IsUnicode = true, IsKey = true, InputMask = "")]
        [PXDefault]
        [PXUIField(DisplayName = "Contract Template", Visibility = PXUIVisibility.SelectorVisible)]
        [PXFieldDescription]
        public virtual String ContractCD
        {
            get;
            set;
        }
    }
}
