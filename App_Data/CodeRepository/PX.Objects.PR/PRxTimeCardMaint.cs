using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;

namespace PX.Objects.PR
{
	public class PRxTimeCardMaint : PXGraphExtension<TimeCardMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		public delegate void CheckTimeCardUsageDelegate(EPTimeCard timeCard);
		[PXOverride]
		public virtual void CheckTimeCardUsage(EPTimeCard timeCard, CheckTimeCardUsageDelegate baseMethod)
		{
			CheckTimeCardUsage(timeCard);

			baseMethod(timeCard);
		}

		public virtual void CheckTimeCardUsage(EPTimeCard timeCard)
		{
			PXResult<PREarningDetail, PMTimeActivity, PRPayment, PRBatch> linkedRecord = (PXResult<PREarningDetail, PMTimeActivity, PRPayment, PRBatch>)SelectFrom<PREarningDetail>
				.InnerJoin<PMTimeActivity>
					.On<PREarningDetail.sourceType.IsEqual<EarningDetailSourceType.timeActivity>
						.And<PREarningDetail.sourceNoteID.IsEqual<PMTimeActivity.noteID>>>
				.LeftJoin<PRPayment>
					.On<PREarningDetail.FK.Payment
						.And<PRPayment.voided.IsNotEqual<True>>
						.And<PRPayment.docType.IsNotEqual<PayrollType.voidCheck>>>
				.LeftJoin<PRBatch>
					.On<PREarningDetail.FK.PayrollBatch
						.And<PRBatch.status.IsEqual<BatchStatus.hold>
							.Or<PRBatch.status.IsEqual<BatchStatus.balanced>>>>
				.Where<PMTimeActivity.timeCardCD.IsEqual<P.AsString>>.View.SelectSingleBound(Base, null, timeCard?.TimeCardCD);

			PRPayment linkedPayment = linkedRecord;
			PRBatch linkedPayrollBatch = linkedRecord;

			if (!string.IsNullOrWhiteSpace(linkedPayment?.RefNbr))
			{
				string message = linkedPayment.Released == false && linkedPayment.Paid == false ? Messages.TimeActivitiesImportedToPaycheck : Messages.TimeActivitiesImportedToPaidOrReleasedPaycheck;
				throw new PXException(message, linkedPayment.PaymentDocAndRef);
			}

			if (!string.IsNullOrWhiteSpace(linkedPayrollBatch?.BatchNbr))
			{
				throw new PXException(Messages.TimeActivitiesImportedToPayrollBatch, linkedPayrollBatch.BatchNbr);
			}
		}
	}
}
