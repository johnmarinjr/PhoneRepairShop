using PX.Data;
using PX.Objects.AR.CCPaymentProcessing.Common;
using PX.Objects.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AR
{
	public class CardType
	{
		public const string VisaCode = "VIS";
		public const string MasterCardCode = "MSC";
		public const string AmericanExpressCode = "AME";
		public const string DiscoverCode = "DSC";
		public const string DinersClubCode = "DNC";
		public const string UnionPayCode = "UNI";
		public const string JCBCode = "JCB";
		public const string DebitCode = "DBT";
		public const string AleloCode = "ALE";
		public const string AliaCode = "ALI";
		public const string CabalCode = "CBL";
		public const string CarnetCode = "CRN";
		public const string DankortCode = "DNK";
		public const string EloCode = "ELO";
		public const string ForbrugsforeningenCode = "FRB";
		public const string MaestroCode = "MAE";
		public const string NaranjaCode = "NRJ";
		public const string SodexoCode = "SDX";
		public const string VrCode = "VRR";
		public const string OtherCode = "OTH";

		public const string VisaLabel = "Visa";
		public const string MasterCardLabel = "MasterCard";
		public const string AmericanExpressLabel = "American Express";
		public const string DiscoverLabel = "Discover";
		public const string DinersClubLabel = "Diners Club";
		public const string UnionPayLabel = "Union Pay";
		public const string JCBLabel = "JCB";
		public const string DebitLabel = "Debit";
		public const string AleloLabel = "Alelo";
		public const string AliaLabel = "Alia";
		public const string CabalLabel = "Cabal";
		public const string CarnetLabel = "Carnet";
		public const string DankortLabel = "Dankort";
		public const string EloLabel = "Elo";
		public const string ForbrugsforeningenLabel = "Forbrugsforeningen";
		public const string MaestroLabel = "Maestro";
		public const string NaranjaLabel = "Naranja";
		public const string SodexoLabel = "Sodexo";
		public const string VrLabel = "Vr";
		public const string OtherLabel = "Other";

		public IEnumerable<ValueLabelPair> ValueLabelPairs => _valueLabelPairs;

		protected static readonly IEnumerable<ValueLabelPair> _valueLabelPairs = new ValueLabelList
		{
			{ MasterCardCode, MasterCardLabel },
			{ VisaCode, VisaLabel },
			{ AmericanExpressCode, AmericanExpressLabel },
			{ DiscoverCode, DiscoverLabel },
			{ JCBCode, JCBLabel },
			{ DinersClubCode, DinersClubLabel },
			{ UnionPayCode, UnionPayLabel },
			{ MaestroCode, MaestroLabel},
			{ OtherCode, OtherLabel },
			{ AleloCode, AleloLabel },
			{ AliaCode, AliaLabel },
			{ CabalCode, CabalLabel },
			{ CarnetCode, CarnetLabel},
			{ DankortCode, DankortLabel},
			{ EloCode, EloLabel},
			{ ForbrugsforeningenCode, ForbrugsforeningenLabel},
			{ NaranjaCode, NaranjaLabel},
			{ SodexoCode, SodexoLabel},
			{ VrCode, VrLabel},
			{ DebitCode, DebitLabel }
		};

		public static IEnumerable<ValueLabelPair> valueLabelPairsWithBlankItem = _valueLabelPairs.Append(new ValueLabelPair("", ""));

		protected static readonly IEnumerable<ValueLabelPair> _valueLabelPairsWithBlankItem = new ValueLabelList(valueLabelPairsWithBlankItem);
		
		public class ListAttribute : LabelListAttribute
		{

			public ListAttribute() : base(_valueLabelPairs)
			{ }
		}

		public class ListWithBlankItemAttribute : LabelListAttribute
		{
			public ListWithBlankItemAttribute() : base(_valueLabelPairsWithBlankItem)
			{ }
		}

		public static string GetCardTypeCode(CCCardType tranType)
		{
			if (!cardTypeCodes.Where(i => i.Item1 == tranType).Any())
			{
				throw new InvalidOperationException();
			}
			return cardTypeCodes.Where(i => i.Item1 == tranType).First().Item2;
		}

		public static string GetDisplayName(CCCardType cardType)
		{
			string typeCode = GetCardTypeCode(cardType);
			ListAttribute attr = new ListAttribute();
			string label = PXMessages.LocalizeNoPrefix(attr.ValueLabelDic[typeCode]).Trim();
			return label;
		}

		public static string GetDisplayName(string cardType)
		{
			ListAttribute attr = new ListAttribute();
			string label = PXMessages.LocalizeNoPrefix(attr.ValueLabelDic[cardType]).Trim();
			return label;
		}

		public static CCCardType GetCardTypeEnumByCode(string code)
		{
			if (!cardTypeCodes.Where(i => i.Item2.Equals(code, StringComparison.CurrentCultureIgnoreCase)).Any())
			{
				return CCCardType.Other;
			}
			return cardTypeCodes.Where(i => i.Item2.Equals(code, StringComparison.CurrentCultureIgnoreCase)).First().Item1;
		}

		private static (CCCardType, string)[] cardTypeCodes = {
			(CCCardType.Visa, VisaCode),
			(CCCardType.MasterCard, MasterCardCode),
			(CCCardType.AmericanExpress, AmericanExpressCode),
			(CCCardType.Discover, DiscoverCode),
			(CCCardType.DinersClub, DinersClubCode),
			(CCCardType.UnionPay, UnionPayCode),
			(CCCardType.JCB, JCBCode),
			(CCCardType.Debit, DebitCode),
			(CCCardType.Alelo, AleloCode ),
			(CCCardType.Alia, AliaCode ),
			(CCCardType.Cabal, CabalCode ),
			(CCCardType.Carnet, CarnetCode),
			(CCCardType.Dankort, DankortCode),
			(CCCardType.Elo, EloCode),
			(CCCardType.Forbrugsforeningen, ForbrugsforeningenCode),
			(CCCardType.Maestro, MaestroCode),
			(CCCardType.Naranja, NaranjaCode),
			(CCCardType.Sodexo, SodexoCode),
			(CCCardType.Vr, VrCode),
			(CCCardType.Other, OtherCode)
		};

		public class other : PX.Data.BQL.BqlString.Constant<other>
		{
			public other() : base(OtherCode) { }
		}
	}
}
