using System;
using PX.Common;
using PX.Data;

namespace PX.Objects.CA
{
	public class CAMatchProcessContext : IDisposable
	{
		private int? CashAccountID { get; set; }

		public CAMatchProcessContext(CABankMatchingProcess graph, int? cashAccountID, Guid? processorID)
		{
			CashAccountID = cashAccountID;
			VerifyRunningProcess(graph, cashAccountID);
			InsertMatchInfo(processorID, cashAccountID);
		}

		private static void VerifyRunningProcess(CABankMatchingProcess graph, int? cashAccountID)
		{
			var runnungProcess = graph.MatchProcessSelect.SelectSingle(cashAccountID);

			if (runnungProcess?.CashAccountID == null)
			{
				return;
			}

			var keyGUID = runnungProcess.ProcessUID ?? new Guid();
			if (PXLongOperation.GetStatus(keyGUID) == PXLongRunStatus.InProcess)
			{
				var cashAccount = graph.cashAccount.SelectSingle(cashAccountID);
				throw new PXException(Messages.CashAccountIsInMatchingProcess, cashAccount.CashAccountCD);
			}

			DeleteMatchInfo(cashAccountID);
		}

		private static void InsertMatchInfo(Guid? processUID, int? cashAccountID)
		{
			PXDatabase.Insert<CAMatchProcess>(
				new PXDataFieldAssign<CAMatchProcess.processUID>(processUID),
				new PXDataFieldAssign<CAMatchProcess.cashAccountID>(cashAccountID),
				new PXDataFieldAssign<CAMatchProcess.operationStartDate>(PXTimeZoneInfo.Now),
				new PXDataFieldAssign<CAMatchProcess.startedByID>(PXAccess.GetUserID()));
		}

		private static void DeleteMatchInfo(int? cashAccountID)
		{
			PXDatabase.Delete<CAMatchProcess>(new PXDataFieldRestrict<CAMatchProcess.cashAccountID>(PXDbType.Int, cashAccountID));
		}

		public void Dispose()
		{
			//this is workaround caused by PXLongOperation.PXUntouchedScope, will be removed in 2022R2 (AC-227995)
			System.Threading.Thread.Sleep(120);
			DeleteMatchInfo(CashAccountID);
		}
	}
}
