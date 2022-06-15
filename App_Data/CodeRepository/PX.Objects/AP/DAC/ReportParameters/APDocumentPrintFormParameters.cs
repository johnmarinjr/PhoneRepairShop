using System;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.Common;

namespace PX.Objects.AP.DAC.ReportParameters
{
    [PXHidden]
    public class APDocumentPrintFormParameters : IBqlTable
    {
        #region PaymentDocType
        public abstract class paymentDocType : PX.Data.BQL.BqlString.Field<paymentDocType>
        {
            public class ListAttribute : PXStringListAttribute
            {
                public ListAttribute() : base(GetAllowedValues(), GetAllowedLabels()) { }

                public static string[] GetAllowedValues()
                {
                    List<string> allowedValues = new List<string>
                    {
                        APDocType.Check,
                        APDocType.VoidCheck,
                        APDocType.Prepayment,
                        APDocType.Refund,
                        APDocType.VoidRefund,
                        APDocType.QuickCheck,
                        APDocType.VoidQuickCheck
                    };

                    return allowedValues.ToArray();
                }

                public static string[] GetAllowedLabels()
                {
                    List<string> allowedLabels = new List<string>
                    {
                        Messages.Check,
                        Messages.VoidCheck,
                        Messages.Prepayment,
                        Messages.Refund,
                        Messages.VoidRefund,
                        Messages.QuickCheck,
                        Messages.VoidQuickCheck
                    };

                    return allowedLabels.ToArray();
                }
            }
        }

        [paymentDocType.List()]
        [PXDBString(3)]
        [PXUIField(DisplayName = "Doc Type", Visibility = PXUIVisibility.SelectorVisible)]
        public String PaymentDocType { get; set; }
        #endregion
    }
}
