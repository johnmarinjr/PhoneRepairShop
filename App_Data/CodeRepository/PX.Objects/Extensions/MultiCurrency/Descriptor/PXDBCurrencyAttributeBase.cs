using CommonServiceLocator;
using PX.Data;
using PX.Objects.Extensions.MultiCurrency;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.CM.Extensions
{
	public abstract class PXDBCurrencyAttributeBase : PXDBDecimalAttribute, ICurrencyAttribute
	{
		#region State

		public Type ResultField { get; }
		public Type KeyField { get; }

		public virtual bool BaseCalc { get; set; } = true;
		public virtual int? CustomPrecision => null;

		#endregion

		#region Initialization

		public PXDBCurrencyAttributeBase(Type keyField, Type resultField)
		{
			ResultField = resultField;
			KeyField = keyField;
		}

		public PXDBCurrencyAttributeBase(Type precision, Type keyField, Type resultField)
			: base(precision)
		{
			ResultField = resultField;
			KeyField = keyField;
		}

		public PXDBCurrencyAttributeBase(int precision, Type keyField, Type resultField)
			: base(precision)
		{
			ResultField = resultField;
			KeyField = keyField;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			void subscribeToEvents(PXGraph graph)
			{
				Type itemType = sender.GetItemType();
				var curyHost = sender.Graph.FindImplementation<ICurrencyHost>();

				if (curyHost != null && !curyHost.IsTrackedType(itemType))
				{
					//We need an ability to toggle cury - base values on DACs from Join<> that are shown on UI.
					sender.Graph.FieldSelecting.AddHandler(itemType, FieldName, (s, e) => CuryFieldSelecting(s, e, new CuryField(this)));
					sender.Graph.RowPersisting.AddHandler(itemType, (s, e) => CuryRowPersisting(s, e, new CuryField(this)));
				}
			}

			if (sender.Graph.IsInitializing)
				sender.Graph.Initialized += subscribeToEvents;
			else
				subscribeToEvents(sender.Graph);
		}

		#endregion

		#region Implementation

		protected virtual void CuryFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e, CuryField curyField)
		{
			if (sender.Graph.Accessinfo.CuryViewState && !string.IsNullOrEmpty(curyField.BaseName))
			{
				e.ReturnValue = sender.GetValue(e.Row, curyField.BaseName);
				var curyHost = sender.Graph.FindImplementation<ICurrencyHost>();
				if (CM.PXCurrencyAttribute.IsNullOrEmpty(e.ReturnValue as decimal?) && curyHost != null)
				{
					object curyValue = sender.GetValue(e.Row, curyField.CuryName);
					CurrencyInfo curyInfo = curyHost.GetCurrencyInfo(sender, e.Row, curyField.CuryInfoIDName);

					curyField.RecalculateFieldBaseValue(sender, e.Row, curyValue, curyInfo, true);
					e.ReturnValue = sender.GetValue(e.Row, curyField.BaseName);
				}

				if (e.IsAltered)
				{
					e.ReturnState = PXFieldState.CreateInstance(e.ReturnState, null, enabled: false);
				}
			}
		}

		protected virtual void CuryRowPersisting(PXCache sender, PXRowPersistingEventArgs e, CuryField curyField)
		{
			var curyHost = sender.Graph.FindImplementation<ICurrencyHost>();
			CurrencyInfo curyInfo = curyHost.GetCurrencyInfo(sender, e.Row, curyField.CuryInfoIDName);

			decimal? curyValue = (decimal?)sender.GetValue(e.Row, curyField.CuryName);
			curyField.RecalculateFieldBaseValue(sender, e.Row, curyValue, curyInfo);
		}

		#endregion
	}
}
