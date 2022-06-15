using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CA.BankStatementProtoHelpers;
using PX.Objects.CM;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.TX;

namespace PX.Objects.CA
{
	[Serializable]
    [PXCacheName(Messages.BankTranMatch)]
	public partial class CABankTranMatch : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<CABankTranMatch>.By<tranID, lineNbr, matchType>
		{
			public static CABankTranMatch Find(PXGraph graph, int? tranID, int? lineNbr, string matchType) => FindBy(graph, tranID, lineNbr, matchType);
		}

		public static class FK
		{
			public class BankTransaction : CA.CABankTran.PK.ForeignKeyOf<CABankTranMatch>.By<tranID> { }
			public class CashAccountTransaction : CA.CATran.UK.ForeignKeyOf<CABankTranMatch>.By<cATranID> { }
			public class BusinessAccount : CR.BAccount.PK.ForeignKeyOf<CABankTranMatch>.By<referenceID> { }
			public class ARInvoice : AR.ARInvoice.PK.ForeignKeyOf<CABankTranMatch>.By<docType, docRefNbr> { }
			public class APInvoice : AP.APInvoice.PK.ForeignKeyOf<CABankTranMatch>.By<docType, docRefNbr> { }
		}

		#endregion

		#region TranID
		public abstract class tranID : PX.Data.BQL.BqlInt.Field<tranID> { }

		[PXDBInt(IsKey = true)]
		public virtual int? TranID
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = false)]
		[PXLineNbr(typeof(CABankTran.lineCntrMatch))]
		[PXParent(typeof(Select<CABankTran, Where<CABankTran.tranID, Equal<Current<CABankTranMatch.tranID>>>>))]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region MatchType
		public abstract class matchType : PX.Data.BQL.BqlString.Field<matchType>
		{
			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute()
					: base(
						new string[] { matchType.Match, Charge },
						new string[] { Messages.Match, Messages.Charge })
				{ }
			}

			public const string Match = "M";
			public const string Charge = "C";

			public class match : PX.Data.BQL.BqlString.Constant<match>
			{
				public match() : base(Match) { }
			}

			public class charge : PX.Data.BQL.BqlString.Constant<charge>
			{
				public charge() : base(Charge) { }
			}
		}

		[PXDBString(1, IsKey = true)]
		[matchType.List]
		[PXDefault(matchType.Match)]
		public virtual string MatchType
		{
			get;
			set;
		}
		#endregion
		#region TranType
		public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType> { }

		[PXDBString(1, IsFixed = true)]
		[PXDefault]
		[CABankTranType.List]
		public virtual string TranType
		{
			get;
			set;
		}
		#endregion
		#region CATranID
		public abstract class cATranID : PX.Data.BQL.BqlLong.Field<cATranID> { }

		[PXDBLong]
		public virtual long? CATranID
		{
			get;
			set;
		}
		#endregion
		#region DocModule
		public abstract class docModule : PX.Data.BQL.BqlString.Field<docModule> { }

		[PXDBString(2, IsFixed = true)]
		[PXStringList(new string[] { GL.BatchModule.AP, GL.BatchModule.AR }, new string[] { GL.Messages.ModuleAP, GL.Messages.ModuleAR })]
		public virtual string DocModule
		{
			get;
			set;
		}
		#endregion
		#region DocType
		public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }

		[PXDBString(3, IsFixed = true, InputMask = "")]
		public virtual string DocType
		{
			get;
			set;
		}
		#endregion
		#region TaxCategoryID
		public abstract class taxCategoryID : PX.Data.BQL.BqlString.Field<taxCategoryID> { }
		[PXDBString(TaxCategory.taxCategoryID.Length, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Category", Visible = true, Visibility = PXUIVisibility.SelectorVisible)]
		[CABankTranMatchTax(typeof(CABankTran), typeof(CABankChargeTax), typeof(CABankTaxTranMatch), typeof(CABankTran.chargeTaxCalcMode),
			CuryOrigDocAmt = typeof(CABankTran.curyChargeAmt), CuryLineTotal = typeof(CABankTran.curyChargeAmt), TaxZoneID = typeof(CABankTran.chargeTaxZoneID))]
		[PXSelector(typeof(TaxCategory.taxCategoryID), DescriptionField = typeof(TaxCategory.descr))]
		[PXRestrictor(typeof(Where<TaxCategory.active, Equal<True>>), TX.Messages.InactiveTaxCategory, typeof(TaxCategory.taxCategoryID))]
		[PXDefault(typeof(Search<TaxZone.dfltTaxCategoryID,
						   Where<TaxZone.taxZoneID, Equal<Current<CABankTran.chargeTaxZoneID>>, And<Current<CABankTranMatch.matchType>, Equal<matchType.charge>,
							   And<Current<CABankTranMatch.cATranID>, IsNull>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual string TaxCategoryID
		{
			get;
			set;
		}
		#endregion
		#region DocRefNbr
		public abstract class docRefNbr : PX.Data.BQL.BqlString.Field<docRefNbr> { }

		[PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		public virtual string DocRefNbr
		{
			get;
			set;
		}
		#endregion
		#region ReferenceID
		public abstract class referenceID : PX.Data.BQL.BqlInt.Field<referenceID> { }

		[PXDBInt]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual int? ReferenceID
		{
			get;
			set;
		}
		#endregion
		#region CuryAmt
		public abstract class curyAmt : PX.Data.BQL.BqlDecimal.Field<curyAmt> { }

		[PXDBDecimal]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual decimal? CuryAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryApplAmt
		public abstract class curyApplAmt : PX.Data.BQL.BqlDecimal.Field<curyApplAmt> { }

		[PXDBDecimal]
		[PXUnboundFormula(typeof(IIf<Where<matchType, Equal<matchType.match>>, curyApplAmt, decimal0>), typeof(SumCalc<CABankTran.curyApplAmtMatch>))]
		public virtual decimal? CuryApplAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryApplTaxableAmt
		public abstract class curyApplTaxableAmt : PX.Data.BQL.BqlDecimal.Field<curyApplTaxableAmt> { }

		[PXDBDecimal]
		public virtual decimal? CuryApplTaxableAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryApplTaxAmt
		public abstract class curyApplTaxAmt : PX.Data.BQL.BqlDecimal.Field<curyApplTaxAmt> { }

		[PXDBDecimal]
		public virtual decimal? CuryApplTaxAmt
		{
			get;
			set;
		}
		#endregion
		#region IsCharge
		public abstract class isCharge : PX.Data.BQL.BqlBool.Field<isCharge> { }

		[PXDBBool]
		public virtual bool? IsCharge
		{
			get;
			set;
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		[PXDBLong]
		[CurrencyInfo]
		public virtual long? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }

		[PXDBTimestamp]
		public virtual byte[] tstamp
		{
			get;
			set;
		}
		#endregion
		public static void Redirect(PXGraph graph, CABankTranMatch match)
		{
			if (match.DocModule == GL.BatchModule.AP && match.DocType == CATranType.CABatch && match.DocRefNbr != null)
			{
				CABatchEntry docGraph = PXGraph.CreateInstance<CABatchEntry>();
				docGraph.Clear();
				docGraph.Document.Current = PXSelect<CABatch, Where<CABatch.batchNbr, Equal<Required<CATran.origRefNbr>>>>.Select(docGraph, match.DocRefNbr);
				throw new PXRedirectRequiredException(docGraph, true, "Document") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}
			else if (match.CATranID != null)
			{
				CATran catran = PXSelect<CATran, Where<CATran.tranID, Equal<Required<CABankTranMatch.cATranID>>>>.Select(graph, match.CATranID);
				CATran.Redirect(null, catran);
			}
			else if (match.DocModule != null && match.DocType != null && match.DocRefNbr != null)
			{
				RedirectionToOrigDoc.TryRedirect(match.DocType, match.DocRefNbr, match.DocModule);
			}
		}

		public void Copy(CABankTranDocRef docRef)
		{
			CATranID = docRef.CATranID;
			DocModule = docRef.DocModule;
			DocType = docRef.DocType;
			DocRefNbr = docRef.DocRefNbr;
			ReferenceID = docRef.ReferenceID;

			bool cashDiscIsApplicable = docRef.CuryDiscAmt != null
												&& docRef.TranDate != null
												&& docRef.DiscDate != null
												&& (DateTime)docRef.TranDate <= (DateTime)docRef.DiscDate;
			CuryApplAmt = docRef.CuryTranAmt - (cashDiscIsApplicable ? docRef.CuryDiscAmt : 0);
		}
	}

	public partial class CABankTranMatch2 : CABankTranMatch
	{
		#region TranID
		public new abstract class tranID : PX.Data.BQL.BqlInt.Field<tranID> { }
		#endregion
		#region TranType
		public new abstract class tranType : PX.Data.BQL.BqlString.Field<tranType> { }
		#endregion
		#region CATranID
		public new abstract class cATranID : PX.Data.BQL.BqlLong.Field<cATranID> { }
		#endregion
		#region DocModule
		public new abstract class docModule : PX.Data.BQL.BqlString.Field<docModule> { }
		#endregion
		#region DocType
		public new abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		#endregion
		#region DocRefNbr
		public new abstract class docRefNbr : PX.Data.BQL.BqlString.Field<docRefNbr> { }
		#endregion
	}
}
