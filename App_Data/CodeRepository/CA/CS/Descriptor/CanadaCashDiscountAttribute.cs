using System;
using PX.Data;

namespace PX.Objects.Localizations.CA.CS
{
    public class CanadaCashDiscountAttribute : PXEventSubscriberAttribute, IPXFieldUpdatedSubscriber
    {
        protected Type _CuryDocBal;
        protected Type _CuryTaxTotal;
        protected Type _CuryDocBalWithoutTax;

        public CanadaCashDiscountAttribute(Type CuryDocBal, Type CuryTaxTotal, Type CuryDocBalWithoutTax)
        {
            _CuryDocBal = CuryDocBal;
            _CuryTaxTotal = CuryTaxTotal;
            _CuryDocBalWithoutTax = CuryDocBalWithoutTax;
        }


        public void FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            decimal CuryDocBal = (decimal)sender.GetValue(e.Row, _CuryDocBal.Name);
            decimal CuryTaxTotal = (decimal)sender.GetValue(e.Row, _CuryTaxTotal.Name);

            sender.SetValueExt(e.Row, _CuryDocBalWithoutTax.Name, CuryDocBal - CuryTaxTotal);
        }
    }
}
