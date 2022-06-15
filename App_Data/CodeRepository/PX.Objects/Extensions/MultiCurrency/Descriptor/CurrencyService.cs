using System;
using System.Collections.Generic;
using PX.Data;

namespace PX.Objects.CM.Extensions
{
	public interface IPXCurrencyService
	{
		int BaseDecimalPlaces();
		int CuryDecimalPlaces(string curyID);
		int PriceCostDecimalPlaces();
		int QuantityDecimalPlaces();
		string DefaultRateTypeID(string moduleCode);
		IPXCurrencyRate GetRate(string fromCuryID, string toCuryID, string rateTypeID, DateTime? curyEffDate);
		int GetRateEffDays(string rateTypeID);
		/// <summary>
		/// Returns base currency of the tenant.
		/// </summary>
		string BaseCuryID();
		/// <summary>
		/// Returns base currency of the branch or base currency of the tenant if branchID is null.
		/// </summary>
		string BaseCuryID(int? branchID);
		IEnumerable<IPXCurrency> Currencies();
		IEnumerable<IPXCurrencyRateType> CurrencyRateTypes();
		void PopulatePrecision(PXCache cache, CurrencyInfo info);
	}

	public class DatabaseCurrencyService : IPXCurrencyService
	{
		protected PXGraph Graph;
		public DatabaseCurrencyService(PXGraph graph)
		{
			Graph = graph;
		}
		public int BaseDecimalPlaces()
		{
			return CurrencyCollection.GetBaseCurrency()?.DecimalPlaces ?? 2;
		}
		public int CuryDecimalPlaces(string curyID)
		{
			return CurrencyCollection.GetCurrency(curyID)?.DecimalPlaces ?? 2;
		}
		public int PriceCostDecimalPlaces()
		{
			CS.CommonSetup c = PXSelect<CS.CommonSetup>
				.Select(Graph);
			return c?.DecPlPrcCst ?? 2;
		}
		public int QuantityDecimalPlaces()
		{
			CS.CommonSetup c = PXSelect<CS.CommonSetup>
				.Select(Graph);
			return c?.DecPlQty ?? 2;
		}
		public string DefaultRateTypeID(string moduleCode)
		{
			string rateType = null;
			CMSetup CMSetup = (CMSetup)Graph.Caches[typeof(CMSetup)].Current;
			if (CMSetup == null)
			{
				CMSetup = PXSelectReadonly<CMSetup>.Select(Graph);
			}
			if (CMSetup != null && PXAccess.FeatureInstalled<CS.FeaturesSet.multicurrency>())
			{
				switch (moduleCode)
				{
					case GL.BatchModule.CA:
						rateType = CMSetup.CARateTypeDflt;
						break;
					case GL.BatchModule.AP:
					case GL.BatchModule.PO:
						rateType = CMSetup.APRateTypeDflt;
						break;
					case GL.BatchModule.AR:
						rateType = CMSetup.ARRateTypeDflt;
						break;
					case GL.BatchModule.GL:
						rateType = CMSetup.GLRateTypeDflt;
						break;
					case GL.BatchModule.PM:
						rateType = CMSetup.PMRateTypeDflt;
						break;
					default:
						rateType = null;
						break;
				}
			}
			return rateType;
		}
		public IPXCurrencyRate GetRate(string fromCuryID, string toCuryID, string rateTypeID, DateTime? curyEffDate)
		{
			CurrencyRate c = PXSelectReadonly<CurrencyRate,
							Where<CurrencyRate.toCuryID, Equal<Required<CurrencyInfo.baseCuryID>>,
							And<CurrencyRate.fromCuryID, Equal<Required<CurrencyInfo.curyID>>,
							And<CurrencyRate.curyRateType, Equal<Required<CurrencyInfo.curyRateTypeID>>,
							And<CurrencyRate.curyEffDate, LessEqual<Required<CurrencyInfo.curyEffDate>>>>>>,
							OrderBy<Desc<CurrencyRate.curyEffDate>>>.SelectWindowed(Graph, 0, 1, toCuryID, fromCuryID, rateTypeID, curyEffDate);
			return c;
		}
		public int GetRateEffDays(string rateTypeID)
		{
			CurrencyRateType c = PXSelect<CurrencyRateType,
				Where<CurrencyRateType.curyRateTypeID, Equal<Required<CurrencyRateType.curyRateTypeID>>>>
				.Select(Graph, rateTypeID);
			return c?.RateEffDays ?? 0;
		}

		/// <summary>
		/// Returns base currency of the tenant.
		/// </summary>
		public string BaseCuryID()
		{
			return BaseCuryID(null);
		}

		/// <summary>
		/// Returns base currency of the branch or base currency of the tenant if branchID is null.
		/// </summary>
		public string BaseCuryID(int? branchID)
		{
			return PXAccess.GetBranch(branchID)?.BaseCuryID ??
				CurrencyCollection.GetBaseCurrency()?.CuryID;
		}

		public IEnumerable<IPXCurrency> Currencies()
		{
			foreach (Currency c in PXSelect<Currency>.Select(Graph))
			{
				yield return c;
			}
		}
		public IEnumerable<IPXCurrencyRateType> CurrencyRateTypes()
		{
			foreach (CurrencyRateType c in PXSelect<CurrencyRateType>.Select(Graph))
			{
				yield return c;
			}
		}

		public void PopulatePrecision(PXCache cache, CurrencyInfo info)
		{
			if (info != null && (info.CuryPrecision == null || info.BasePrecision == null))
			{
				if (info.CuryPrecision == null)
				{
					info.CuryPrecision = Convert.ToInt16(CuryDecimalPlaces(info.CuryID));
				}

				if (info.BasePrecision == null)
				{
					info.BasePrecision = Convert.ToInt16(CuryDecimalPlaces(info.BaseCuryID));
				}

				if (cache.GetStatus(info) == PXEntryStatus.Notchanged)
				{
					cache.SetStatus(info, PXEntryStatus.Held);
				}
			}
		}
	}
}
