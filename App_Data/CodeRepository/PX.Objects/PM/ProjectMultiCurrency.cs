using CommonServiceLocator;
using PX.Data;
using System;
using System.Collections.Generic;
using PX.Objects.CM.Extensions;
using PX.Objects.GL;
using PX.Objects.CS;
using PX.Objects.Extensions.MultiCurrency;

namespace PX.Objects.PM
{
	public interface IProjectMultiCurrency
	{
		decimal GetValueInProjectCurrency(PXGraph graph, PMProject project, string docCuryID, DateTime? docDate, decimal? value);
		decimal GetValueInBillingCurrency(PXGraph graph, PMProject project, CurrencyInfo docCurrencyInfo, decimal? value);
		void CalculateCurrencyValues(PXGraph graph, GLTran tran, PMTran pmt, Batch batch, PMProject project, Ledger ledger);
		CurrencyInfo CreateDirectRate(PXGraph graph, string curyID, DateTime? date, string module);
		CurrencyInfo CreateRate(PXGraph graph, string curyID, string baseCuryID, DateTime? date, string rateTypeID, string module);

		void Clear();
	}

	public class ProjectMultiCurrency : IProjectMultiCurrency
	{
		private class CurrencyInfoInsertingAdapter
		{
			private readonly PXGraph graph;
			private readonly IPXCurrencyHelper currencyHelper;

			public CurrencyInfoInsertingAdapter(PXGraph graph)
			{
				this.graph = graph;
				currencyHelper = graph.FindImplementation<IPXCurrencyHelper>();
			}

			public CurrencyInfo Insert(CurrencyInfo currencyInfo)
			{
				if (currencyHelper == null)
				{
					PXCache pXCache = graph.Caches[typeof(CM.CurrencyInfo)];
					CM.CurrencyInfo current = pXCache.Current as CM.CurrencyInfo;
					CurrencyInfo result = CurrencyInfo.GetEX(pXCache.Insert(currencyInfo.GetCM()) as CM.CurrencyInfo);
					if (current != null) pXCache.Current = current;
					return result;
				}
				else
				{
					PXCache pXCache = graph.Caches[typeof(CurrencyInfo)];
					CurrencyInfo current = currencyHelper.GetDefaultCurrencyInfo();
					CurrencyInfo result = pXCache.Insert(currencyInfo) as CurrencyInfo;
					if (current != null) pXCache.Current = current;
					return result;
				}
			}
		}

		protected Dictionary<CurrencyInfoKey, CurrencyInfo> directRates = new Dictionary<CurrencyInfoKey, CurrencyInfo>();
		protected Dictionary<CurrencyInfoKey, CurrencyInfo> rates = new Dictionary<CurrencyInfoKey, CurrencyInfo>();

		public virtual void CalculateCurrencyValues(PXGraph graph, GLTran tran, PMTran pmt, Batch batch, PMProject project, Ledger ledger)
		{
			decimal? tranCuryAmount = tran.CuryDebitAmt - tran.CuryCreditAmt;


			if (PXAccess.FeatureInstalled<FeaturesSet.projectMultiCurrency>())
			{
				pmt.TranCuryID = batch.CuryID;
				pmt.ProjectCuryID = project.CuryID;
				pmt.TranCuryAmount = tranCuryAmount;

				if (batch.CuryID == project.BaseCuryID)
				{
					CurrencyInfo baseCuryInfo = CreateDirectRate(graph, project.BaseCuryID, tran.TranDate, GL.BatchModule.PM);
					pmt.BaseCuryInfoID = baseCuryInfo.CuryInfoID;
					pmt.Amount = tranCuryAmount;
				}
				else
				{
					CurrencyInfo baseCuryInfo = CreateRate(graph, pmt.TranCuryID, project.BaseCuryID, tran.TranDate, project.RateTypeID, GL.BatchModule.PM);
					pmt.BaseCuryInfoID = baseCuryInfo.CuryInfoID;
					pmt.Amount = baseCuryInfo.CuryConvBase(tranCuryAmount.GetValueOrDefault());
				}

				if (project.CuryID == batch.CuryID)
				{
					CurrencyInfo projectCuryInfo = CreateDirectRate(graph, project.CuryID, tran.TranDate, GL.BatchModule.PM);
					pmt.ProjectCuryInfoID = projectCuryInfo.CuryInfoID;
					pmt.ProjectCuryAmount = pmt.TranCuryAmount;
				}
				else
				{
					CurrencyInfo projectCuryInfo = CreateRate(graph, pmt.TranCuryID, project.CuryID, tran.TranDate, project.RateTypeID, GL.BatchModule.PM);
					pmt.ProjectCuryInfoID = projectCuryInfo.CuryInfoID;
					pmt.ProjectCuryAmount = projectCuryInfo.CuryConvBase(pmt.TranCuryAmount.GetValueOrDefault());
				}
			}
			else
			{
				decimal? tranAmount = tran.DebitAmt - tran.CreditAmt;

				pmt.TranCuryID = project.BaseCuryID;
				pmt.ProjectCuryID = project.BaseCuryID;

				if (ledger.BaseCuryID == project.BaseCuryID)
				{
					pmt.Amount = tranAmount;

					if (batch.CuryID == project.BaseCuryID)
					{
						pmt.ProjectCuryInfoID = tran.CuryInfoID;
						pmt.BaseCuryInfoID = tran.CuryInfoID;
					}
					else
					{
						CurrencyInfo baseCuryInfo = CreateDirectRate(graph, project.BaseCuryID, tran.TranDate, GL.BatchModule.PM);
						pmt.ProjectCuryInfoID = baseCuryInfo.CuryInfoID;
						pmt.BaseCuryInfoID = baseCuryInfo.CuryInfoID;
					}
				}
				else
				{
					if (batch.CuryID == project.BaseCuryID)
					{
						pmt.Amount = tranAmount;
					}
					else
					{
						CurrencyInfo curyInfo = CreateRate(graph, batch.CuryID, project.BaseCuryID, tran.TranDate, project.RateTypeID, GL.BatchModule.PM);
						pmt.Amount = curyInfo.CuryConvBase(tranCuryAmount.GetValueOrDefault());
					}

					CurrencyInfo baseCuryInfo = CreateDirectRate(graph, project.BaseCuryID, tran.TranDate, GL.BatchModule.PM);
					pmt.ProjectCuryInfoID = baseCuryInfo.CuryInfoID;
					pmt.BaseCuryInfoID = baseCuryInfo.CuryInfoID;
				}

				pmt.TranCuryAmount = pmt.Amount;
				pmt.ProjectCuryAmount = pmt.Amount;
			}
		}

		public virtual CurrencyInfo CreateDirectRate(PXGraph graph, string curyID, DateTime? date, string module)
		{
			CurrencyInfoKey key = new CurrencyInfoKey(curyID, date.GetValueOrDefault());

			if (!directRates.TryGetValue(key, out CurrencyInfo result))
			{
				result = new CurrencyInfoInsertingAdapter(graph).Insert(new CurrencyInfo
				{
					ModuleCode = module,
					BaseCuryID = curyID,
					CuryID = curyID,
					CuryRateTypeID = null,
					CuryEffDate = date,
					CuryRate = 1,
					RecipRate = 1
				});

				directRates.Add(key, result);
			}

			return result;
		}

		public virtual CurrencyInfo CreateRate(PXGraph graph, string curyID, string baseCuryID, DateTime? date, string rateTypeID, string module)
		{
			IPXCurrencyService currencyService = ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(graph);
			CurrencyInfoKey key = new CurrencyInfoKey(curyID, baseCuryID, rateTypeID ?? currencyService.DefaultRateTypeID(module), date.GetValueOrDefault());

			if (!rates.TryGetValue(key, out CurrencyInfo result))
			{
				var rate = currencyService.GetRate(key.CuryID, key.BaseCuryID, key.RateTypeID, key.Date);
				if (rate == null)
				{
					throw new PXException(Messages.FxTranToProjectNotFound, key.CuryID, key.BaseCuryID, key.RateTypeID, date);
				}
				result = new CurrencyInfoInsertingAdapter(graph).Insert(new CurrencyInfo
				{
					ModuleCode = module,
					BaseCuryID = key.BaseCuryID,
					CuryID = key.CuryID,
					CuryRateTypeID = key.RateTypeID,
					CuryEffDate = key.Date
				});

				rates.Add(key, result);
			}

			return result;
		}

		public virtual decimal GetValueInProjectCurrency(PXGraph graph, PMProject project, string docCuryID, DateTime? docDate, decimal? value)
		{
			if (value.GetValueOrDefault() == 0)
			{
				return 0;
			}

			if (project.CuryID == docCuryID)
			{
				return value.GetValueOrDefault();
			}

			if (project.BaseCuryID == docCuryID)
			{
				//use project's currency info for conversion
				return CM.TemporaryHelpers.MultiCurrencyCalculator.GetCurrencyInfo<PMProject.curyInfoID>(graph, project).CuryConvCury(value.GetValueOrDefault());
			}
			else
			{
				//use project's rate type to convert
				IPXCurrencyService currencyService = ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(graph);
				var rate = currencyService.GetRate(docCuryID, project.CuryID, GetRateTypeID(graph, project), docDate.GetValueOrDefault(DateTime.Now));


				if (rate == null)
				{
					throw new PXException(Messages.CurrencyRateIsNotDefined, docCuryID, project.CuryID, GetRateTypeID(graph, project), docDate.GetValueOrDefault(DateTime.Now));
				}

				int precision = currencyService.CuryDecimalPlaces(project.CuryID);
				return CuryConvCury(rate, value.GetValueOrDefault(), precision);
			}
		}

		public virtual decimal GetValueInBillingCurrency(PXGraph graph, PMProject project, CurrencyInfo docCurrencyInfo, decimal? value)
		{
			if (value.GetValueOrDefault() == 0)
			{
				return 0;
			}

			if (project.CuryID == project.BillingCuryID)
			{
				return value.GetValueOrDefault();
			}
			else
			{
				if (docCurrencyInfo == null)
					throw new ArgumentNullException(nameof(docCurrencyInfo));

				if (docCurrencyInfo.BaseCuryID == project.CuryID)
				{
					return docCurrencyInfo.CuryConvCury(value.GetValueOrDefault());
				}
				else
				{
					IPXCurrencyService currencyService = ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(graph);
					var rate = currencyService.GetRate(project.CuryID, project.BillingCuryID, docCurrencyInfo.CuryRateTypeID, docCurrencyInfo.CuryEffDate.GetValueOrDefault(DateTime.Now));

					if (rate == null)
					{
						throw new PXException(Messages.CurrencyRateIsNotDefined, project.CuryID, project.BillingCuryID, docCurrencyInfo.CuryRateTypeID, docCurrencyInfo.CuryEffDate.GetValueOrDefault(DateTime.Now));
					}

					int precision = currencyService.CuryDecimalPlaces(project.CuryID);
					return CuryConvCury(rate, value.GetValueOrDefault(), precision);
				}
			}
		}

		public void Clear()
		{
			directRates.Clear();
			rates.Clear();
		}

		protected virtual decimal CuryConvCury(IPXCurrencyRate foundRate, decimal baseval, int? precision)
		{
			if (baseval == 0) return 0m;

			if (foundRate == null)
				throw new ArgumentNullException(nameof(foundRate));

			decimal rate;
			decimal curyval;
			try
			{
				rate = (decimal)foundRate.CuryRate;
			}
			catch (InvalidOperationException)
			{
				throw new CM.PXRateNotFoundException();
			}
			if (rate == 0.0m)
			{
				rate = 1.0m;
			}
			bool mult = foundRate.CuryMultDiv != "D";
			curyval = mult ? (decimal)baseval * rate : (decimal)baseval / rate;

			if (precision.HasValue)
			{
				curyval = Decimal.Round(curyval, precision.Value, MidpointRounding.AwayFromZero);
			}

			return curyval;
		}

		protected virtual string GetRateTypeID(PXGraph graph, PMProject project)
		{
			string rateTypeID = project.RateTypeID;

			if (string.IsNullOrEmpty(rateTypeID))
			{
				CM.CMSetup cmsetup = PXSelect<CM.CMSetup>.Select(graph);
				rateTypeID = cmsetup?.PMRateTypeDflt;
			}

			return rateTypeID;
		}

		protected class CurrencyInfoKey
		{
			public readonly string CuryID;
			public readonly string BaseCuryID;
			public readonly string RateTypeID;
			public readonly DateTime Date;

			public CurrencyInfoKey(string curyID, string baseCuryID, string rateTypeID, DateTime date)
			{
				CuryID = curyID;
				BaseCuryID = baseCuryID;
				RateTypeID = rateTypeID;
				Date = date;
			}

			public CurrencyInfoKey(string curyID, DateTime date) : this(curyID, curyID, string.Empty, date)
			{
			}

			public override int GetHashCode()
			{
				unchecked // Overflow is fine, just wrap
				{
					int hash = 17;
					hash = hash * 23 + CuryID.GetHashCode();
					hash = hash * 23 + BaseCuryID.GetHashCode();
					hash = hash * 23 + RateTypeID.GetHashCode();
					hash = hash * 23 + Date.GetHashCode();

					return hash;
				}
			}
		}
	}
}
