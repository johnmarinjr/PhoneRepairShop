using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using System;

namespace PX.Objects.GL
{
	public partial class JournalWithSubEntry
	{	
		public abstract class RegisterAdapter<T> : IRegister, IInvoice
			where T : class, IBqlTable, IInvoice, new()
		{	
			public T Entity { get; }

			public RegisterAdapter(T entity) => Entity = entity;


			#region IRegister Members

			public abstract int? BAccountID { get; set; }

			public abstract int? LocationID { get; set; }

			public abstract int? BranchID { get; set; }

			public virtual DateTime? DocDate { get => Entity.DocDate; set => Entity.DocDate = value; }

			public abstract int? AccountID { get; set; }

			public abstract int? SubID { get; set; }

			public string CuryID { get => Entity.CuryID; set => Entity.CuryID = value; }

			public abstract string FinPeriodID { get; set; }

			public abstract long? CuryInfoID { get; set; }

			#endregion

			#region IInvoice Members

			public decimal? CuryDocBal { get => Entity.CuryDocBal; set => Entity.CuryDocBal = value; }

			public decimal? DocBal { get => Entity.DocBal; set => Entity.DocBal = value; }

			public decimal? CuryDiscBal { get => Entity.CuryDiscBal; set => Entity.CuryDiscBal = value; }
	
			public decimal? DiscBal { get => Entity.DiscBal; set => Entity.DiscBal = value; }

			public decimal? CuryWhTaxBal { get => Entity.CuryWhTaxBal; set => Entity.CuryWhTaxBal = value; }

			public decimal? WhTaxBal { get => Entity.WhTaxBal; set => Entity.WhTaxBal = value; }

			public DateTime? DiscDate { get => Entity.DiscDate; set => Entity.DiscDate = value; }

			public string DocType { get => Entity.DocType; set => Entity.DocType = value; }

			public string RefNbr { get => Entity.RefNbr; set => Entity.RefNbr = value; }

			public string OrigModule { get => Entity.OrigModule; set => Entity.OrigModule = value; }

			public decimal? CuryOrigDocAmt { get => Entity.CuryOrigDocAmt; set => Entity.CuryOrigDocAmt = value; }
	
			public decimal? OrigDocAmt { get => Entity.OrigDocAmt; set => Entity.OrigDocAmt = value; }
	
			public string DocDesc { get => Entity.DocDesc; set => Entity.DocDesc = value; }

			#endregion
		}

		public class APRegisterAdapter<T> : RegisterAdapter<T>
			where T : APRegister,  IInvoice, new()

		{
			public APRegisterAdapter(T entity) : base(entity) { }

			public override int? BranchID { get => Entity.BranchID; set => Entity.BranchID = value; }
			public override string FinPeriodID { get => Entity.FinPeriodID; set => Entity.FinPeriodID = value; }
			public override long? CuryInfoID { get => Entity.CuryInfoID; set => Entity.CuryInfoID = value; }
			public override int? BAccountID { get => Entity.VendorID; set => Entity.VendorID = value; }
			public override int? LocationID { get => Entity.VendorLocationID; set => Entity.VendorLocationID = value; }
			public override int? AccountID { get => Entity.APAccountID; set => Entity.APAccountID = value; }
			public override int? SubID { get => Entity.APSubID; set => Entity.APSubID = value; }
		}

		public class ARRegisterAdapter<T> : RegisterAdapter<T>
			where T : ARRegister,  IInvoice, new()
		{
			public ARRegisterAdapter(T entity) : base(entity) { }

			public override int? BranchID { get => Entity.BranchID; set => Entity.BranchID = value; }
			public override string FinPeriodID { get => Entity.FinPeriodID; set => Entity.FinPeriodID = value; }
			public override long? CuryInfoID { get => Entity.CuryInfoID; set => Entity.CuryInfoID = value; }
			public override int? BAccountID { get => Entity.CustomerID; set => Entity.CustomerID = value; }
			public override int? LocationID { get => Entity.CustomerLocationID; set => Entity.CustomerLocationID = value; }
			public override int? AccountID { get => Entity.ARAccountID; set => Entity.ARAccountID = value; }
			public override int? SubID { get => Entity.ARSubID; set => Entity.ARSubID = value; }

		}

		public class GlRegisterAdapter<T> : RegisterAdapter<T>
			where T : GLTranDoc,  IInvoice, new()
		{
			public GlRegisterAdapter(T entity) : base(entity) { }
			public override int? BranchID { get => Entity.BranchID; set => Entity.BranchID = value; }
			public override string FinPeriodID { get => Entity.FinPeriodID; set => Entity.FinPeriodID = value; }
			public override long? CuryInfoID { get => Entity.CuryInfoID; set => Entity.CuryInfoID = value; }
			public override DateTime? DocDate { get => Entity.TranDate; set => Entity.TranDate = value; }
			public override int? BAccountID { get => Entity.BAccountID; set => Entity.BAccountID = value; }
			public override int? LocationID { get => Entity.LocationID; set => Entity.LocationID = value; }

			private  bool? IsDirect()
			{
				switch (Entity.TranModule)
				{
					case GL.BatchModule.AP:
						return APInvoiceType.DrCr(Entity.TranType) == DrCr.Debit;
					case GL.BatchModule.AR:
						return ARInvoiceType.DrCr(Entity.TranType) == DrCr.Debit;
					default: return null;
				}
			}
			public override int? AccountID
			{
				get
				{
					bool? isDirect = IsDirect();
					if (!isDirect.HasValue) return null;
					else if (isDirect.Value) return Entity.CreditAccountID;
					else return Entity.DebitAccountID;
				}
				set
				{
					bool? isDirect = IsDirect();
					if (!isDirect.HasValue) return;
					else if (isDirect.Value) Entity.CreditAccountID = value;
					else Entity.DebitAccountID = value;
				}
			}

			public override int? SubID
			{
				get
				{
					bool? isDirect = IsDirect();
					if (!isDirect.HasValue) return null;
					else if (isDirect.Value) return Entity.CreditSubID;
					else return Entity.DebitSubID;
				}
				set
				{
					bool? isDirect = IsDirect();
					if (!isDirect.HasValue) return;
					else if (isDirect.Value) Entity.CreditSubID = value;
					else Entity.DebitSubID = value;
				}
			}
		}		
	}
}