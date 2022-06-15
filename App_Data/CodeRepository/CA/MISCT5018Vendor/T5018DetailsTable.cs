// Decompiled
using System;
using PX.Data;

namespace PX.Objects.Localizations.CA
{
    [Serializable]
    [PXHidden]
    public class T5018DetailsTable : IBqlTable
    {
        public abstract class selected : IBqlField, IBqlOperand
        {
        }

        public abstract class curyAdjdAmt : IBqlField, IBqlOperand
        {
        }

        public abstract class vAcctCD : IBqlField, IBqlOperand
        {
        }

        public abstract class vAcctName : IBqlField, IBqlOperand
        {
        }

        public abstract class lTaxRegistrationID : IBqlField, IBqlOperand
        {
        }

        public abstract class payerOrganizationID : IBqlField, IBqlOperand
        {
        }

        [PXBool]
        [PXUIField(DisplayName = "Selected")]
        public virtual bool? Selected
        {
            get;
            set;
        }

        public abstract class vAcctID : PX.Data.BQL.BqlInt.Field<vAcctID> { }

        [PXInt(IsKey = true)]
        public virtual int? VAcctID
        {
	        get;
	        set;
        }

        [PXDecimal]
        [PXUnboundDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Amount", Visibility = PXUIVisibility.Visible)]
        public virtual decimal? CuryAdjdAmt
        {
            get;
            set;
        }

        [PXUIField(DisplayName = "Vendor")]
        [PXString(30, IsUnicode = true, InputMask = "")]
        public virtual string VAcctCD
        {
            get;
            set;
        }

        [PXString(60, IsUnicode = true)]
        [PXUIField(DisplayName = "Vendor Name")]
        public virtual string VAcctName
        {
            get;
            set;
        }

        [PXString(50)]
        [PXUIField(DisplayName = "Tax Registration ID")]
        public virtual string LTaxRegistrationID
        {
            get;
            set;
        }

        [PXString(50)]
        [PXUIField(DisplayName = "Payer")]
        public virtual string PayerOrganizationID
        {
            get;
            set;
        }
    }
}
