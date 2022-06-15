using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CN.JointChecks.AP.Services.CalculationServices;

namespace PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation
{
	public abstract class ValidationServiceBase
	{
		protected readonly APPaymentEntry Graph;
		protected VendorPreparedBalanceCalculationService VendorPreparedBalanceCalculationService;
		protected CashDiscountCalculationService CashDiscountCalculationService;
		protected IJointCheckErrorHandlingStrategy errorHandlingStrategy;

		protected ValidationServiceBase(APPaymentEntry graph, IJointCheckErrorHandlingStrategy jointCheckErrorHandlingStrategy)
		{
			Graph = graph;
            errorHandlingStrategy = jointCheckErrorHandlingStrategy ?? new ChecksAndPaymentsJointCheckErrorHandlingStrategy(graph);
		}

		
		protected IEnumerable<APAdjust> Adjustments => Graph.Adjustments.SelectMain();

		protected IEnumerable<APAdjust> ActualAdjustments => Adjustments.Where(adjustment => adjustment.Voided != true);

		protected IEnumerable<APAdjust> ActualBillAdjustments =>
			ActualAdjustments.Where(adjustment => adjustment.AdjdDocType == APDocType.Invoice);

		protected void InitializeServices(bool isPaymentByLine)
		{
			VendorPreparedBalanceCalculationService = new VendorPreparedBalanceCalculationService(Graph);
			CashDiscountCalculationService = isPaymentByLine
				? new CashDiscountPerLineCalculationService(Graph)
				: new CashDiscountCalculationService(Graph);
		}
	}
}
