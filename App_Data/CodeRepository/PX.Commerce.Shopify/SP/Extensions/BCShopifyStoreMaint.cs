using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Commerce.Shopify.API.REST;
using PX.Api;
using PX.Data;
using RestSharp;
using PX.Commerce.Core;
using PX.Objects.CA;
using PX.Commerce.Objects;
using PX.Objects.GL;
using PX.Objects.CS;
using PX.Common;

namespace PX.Commerce.Shopify
{
	public class BCShopifyStoreMaint : BCStoreMaint
	{
		public PXSelect<BCBindingShopify, Where<BCBindingShopify.bindingID, Equal<Current<BCBinding.bindingID>>>> CurrentBindingShopify;

		public BCShopifyStoreMaint()
		{
			base.Bindings.WhereAnd<Where<BCBinding.connectorType, Equal<SPConnector.spConnectorType>>>();

			PXStringListAttribute.SetList<BCBindingExt.visibility>(base.CurrentStore.Cache, null,
				new[]
				{
					BCItemVisibility.Visible,
					BCItemVisibility.Invisible,
				},
				new[]
				{
					BCCaptions.Visible,
					BCCaptions.Invisible,
				});
		}

		#region Cache Attached
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Shopify Location")]
		[PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
		public virtual void ExportBCLocations_ExternalLocationID_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Shopify Location")]
		[PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
		public virtual void ImportBCLocations_ExternalLocationID_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(BCItemNotAvailModes.List))]
		[PXStringList(
			new[] { BCItemNotAvailModes.DoNothing, BCItemNotAvailModes.DisableItem, BCItemNotAvailModes.PreOrderItem },
			new[] { BCCaptions.DoNothing, BCCaptions.DisableItem, BCCaptions.ContinueSellingItem } )]
		public virtual void BCBindingExt_NotAvailMode_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXSelectorAttribute))]
		[PXSelector(typeof(Search2<CCProcessingCenter.processingCenterID,
			InnerJoin<CCProcessingCenterPmntMethod,
				On<CCProcessingCenterPmntMethod.processingCenterID, Equal<CCProcessingCenter.processingCenterID>>,
			LeftJoin<CCProcessingCenterDetail,
				On<CCProcessingCenterDetail.processingCenterID, Equal<CCProcessingCenter.processingCenterID>,
					And<CCProcessingCenterDetail.detailID, Equal<ShopifyPayments.ShopifyPluginHelper.SettingsKeys.Const_StoreName>>>
			>>,
			Where<CCProcessingCenter.isActive, Equal<True>,
				And2<
					Where<CCProcessingCenter.processingTypeName, IsNull,
						Or<CCProcessingCenter.processingTypeName, NotEqual<ShopifyPayments.ShopifyPaymentsProcessingPlugin.Const_PluginName>,
						Or<CCProcessingCenterDetail.value, Equal<Current<BCPaymentMethods.bindingID>>>>>,
				And<CCProcessingCenterPmntMethod.isActive, Equal<True>,
				And<CCProcessingCenterPmntMethod.paymentMethodID, Equal<Current<BCPaymentMethods.paymentMethodID>>,
				And<CCProcessingCenter.cashAccountID, Equal<Current<BCPaymentMethods.cashAccountID>>>>>>>>))]
		public virtual void BCPaymentMethods_ProcessingCenterID_CacheAttached(PXCache sender) { }
		#endregion

		#region Actions
		public PXAction<BCBinding> TestConnection;
		[PXButton(IsLockedOnToolbar = true)]
		[PXUIField(DisplayName = "Test Connection", Enabled = false)]
		protected virtual IEnumerable testConnection(PXAdapter adapter)
		{
			Actions.PressSave();

			BCBinding binding = Bindings.Current;
			BCBindingShopify bindingShopify = CurrentBindingShopify.Current ?? CurrentBindingShopify.Select();

			if (binding.ConnectorType != SPConnector.TYPE) return adapter.Get();
			if (binding == null || bindingShopify == null || bindingShopify.ShopifyApiBaseUrl == null
				|| (string.IsNullOrWhiteSpace(bindingShopify.ShopifyAccessToken) && (string.IsNullOrEmpty(bindingShopify.ShopifyApiKey) || string.IsNullOrEmpty(bindingShopify.ShopifyApiPassword))))
			{
				throw new PXException(BCMessages.TestConnectionFailedParameters);
			}

			PXLongOperation.StartOperation(this, delegate
			{
				BCShopifyStoreMaint graph = PXGraph.CreateInstance<BCShopifyStoreMaint>();
				graph.Bindings.Current = binding;
				graph.CurrentBindingShopify.Current = bindingShopify;

				StoreRestDataProvider restClient = new StoreRestDataProvider(SPConnector.GetRestClient(bindingShopify));
				try
				{
					var store = restClient.Get();
					if (store == null || store.Id == null)
						throw new PXException(ShopifyMessages.TestConnectionStoreNotFound);

					graph.CurrentBindingShopify.Cache.SetValueExt(binding, nameof(BCBindingShopify.ShopifyStoreUrl), store.Domain);
					graph.CurrentBindingShopify.Cache.SetValueExt(binding, nameof(BCBindingShopify.ShopifySupportCurrencies), string.Join(",", store.EnabledPresentmentCurrencies));
					graph.CurrentBindingShopify.Cache.SetValueExt(binding, nameof(BCBindingShopify.ShopifyStoreTimeZone), store.Timezone);
					graph.CurrentBindingShopify.Update(bindingShopify);
					graph.CurrentStore.Cache.SetValueExt<BCBindingExt.defaultStoreCurrency>(CurrentStore.Current, store.Currency);
					graph.CurrentStore.Cache.Update(CurrentStore.Current);
					graph.Persist();
				}
				catch (Exception ex)
				{
					throw new PXException(ex, BCMessages.TestConnectionFailedGeneral, ex.Message);
				}
			});

			return adapter.Get();
		}
		#endregion

		#region BCBinding Events
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(BCConnectorsAttribute), "DefaultConnector", SPConnector.TYPE)]
		public virtual void _(Events.CacheAttached<BCBinding.connectorType> e) { }

		public override void _(Events.RowSelected<BCBinding> e)
		{
			base._(e);

			BCBinding row = e.Row as BCBinding;
			if (row == null) return;

			//Actions
			TestConnection.SetEnabled(row.BindingID > 0 && row.ConnectorType == SPConnector.TYPE);
		}
		public override void _(Events.RowSelected<BCBindingExt> e)
		{
			base._(e);

			BCBindingExt row = e.Row as BCBindingExt;
			if (row == null) return;

			PXStringListAttribute.SetList<BCBindingExt.availability>(e.Cache, row, new[] {
					BCItemAvailabilities.AvailableTrack,
					BCItemAvailabilities.AvailableSkip,
					BCItemAvailabilities.DoNotUpdate,
					BCItemAvailabilities.Disabled,
				},
				new[]
				{
					BCCaptions.AvailableTrack,
					BCCaptions.AvailableSkip,
					BCCaptions.DoNotUpdate,
					BCCaptions.Disabled,
				});
		}

		public virtual void _(Events.RowSelected<BCBindingShopify> e)
		{
			BCBindingShopify row = e.Row;
			if (row == null) return;

			if (PXAccess.FeatureInstalled<FeaturesSet.shopifyPOS>() && row.ShopifyPOS == true && Entities.Select().RowCast<BCEntity>()?.FirstOrDefault(x => x.EntityType == BCEntitiesAttribute.Order)?.IsActive == true)
			{
				PXUIFieldAttribute.SetRequired<BCBindingShopify.pOSDirectOrderType>(e.Cache, true);
				PXUIFieldAttribute.SetRequired<BCBindingShopify.pOSShippingOrderType>(e.Cache, true);
				PXDefaultAttribute.SetPersistingCheck<BCBindingShopify.pOSDirectOrderType>(e.Cache, e.Row, PXPersistingCheck.NullOrBlank);
				PXDefaultAttribute.SetPersistingCheck<BCBindingShopify.pOSShippingOrderType>(e.Cache, e.Row, PXPersistingCheck.NullOrBlank);
			}
			else
			{
				PXUIFieldAttribute.SetRequired<BCBindingShopify.pOSDirectOrderType>(e.Cache, false);
				PXUIFieldAttribute.SetRequired<BCBindingShopify.pOSShippingOrderType>(e.Cache, false);
				PXDefaultAttribute.SetPersistingCheck<BCBindingShopify.pOSDirectOrderType>(e.Cache, e.Row, PXPersistingCheck.Nothing);
				PXDefaultAttribute.SetPersistingCheck<BCBindingShopify.pOSShippingOrderType>(e.Cache, e.Row, PXPersistingCheck.Nothing);
			}
		}

		public override void _(Events.RowInserted<BCBinding> e)
		{
			base._(e);

			bool dirty = CurrentBindingShopify.Cache.IsDirty;
			CurrentBindingShopify.Insert();
			CurrentBindingShopify.Cache.IsDirty = dirty;
		}
		protected virtual void _(Events.RowPersisting<BCBindingExt> e)
		{
			BCBindingExt row = e.Row as BCBindingExt;
			BCBinding binding = CurrentBinding.Current ?? CurrentBinding.Select();
			BCBindingShopify bindingShopify = CurrentBindingShopify.Current ?? CurrentBindingShopify.Select();
			if (row == null || binding == null || bindingShopify == null) return;
			if (string.IsNullOrWhiteSpace(bindingShopify.ShopifyAccessToken) && string.IsNullOrWhiteSpace(bindingShopify.ShopifyApiKey) && string.IsNullOrWhiteSpace(bindingShopify.ShopifyApiPassword))
			{
				throw new PXSetPropertyException(ShopifyMessages.ApiTokenRequired);
			}
			else if (string.IsNullOrWhiteSpace(bindingShopify.ShopifyAccessToken) && (string.IsNullOrWhiteSpace(bindingShopify.ShopifyApiKey) || string.IsNullOrWhiteSpace(bindingShopify.ShopifyApiPassword)))
			{
				if (string.IsNullOrWhiteSpace(bindingShopify.ShopifyApiKey))
					throw new PXSetPropertyException<BCBindingShopify.shopifyApiKey>(BCMessages.CannotBeNullOrEmpty, "API Key");
				if (string.IsNullOrWhiteSpace(bindingShopify.ShopifyApiPassword))
					throw new PXSetPropertyException<BCBindingShopify.shopifyApiPassword>(BCMessages.CannotBeNullOrEmpty, "API Password");
			}
			FetchDataFromShopify(bindingShopify);
			if (binding.BranchID != null && row.DefaultStoreCurrency != null)
			{
				PX.Objects.GL.Branch branch = PX.Objects.GL.Branch.PK.Find(this, binding.BranchID);
				if (branch?.BaseCuryID != row.DefaultStoreCurrency && binding.IsActive == true)
				{
					CurrentBinding.Cache.RaiseExceptionHandling<BCBinding.branchID>(CurrentBinding.Current, branch.BranchCD, new PXException(BCMessages.BranchWithIncorrectCurrencyForStore, branch?.BaseCuryID, row.DefaultStoreCurrency));
					throw new PXException(BCMessages.BranchWithIncorrectCurrencyForStore, branch?.BaseCuryID, row.DefaultStoreCurrency);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<BCBindingShopify, BCBindingShopify.shopifyApiBaseUrl> e)
		{
			string val = e.NewValue?.ToString();
			if (val != null)
			{
				val = val.ToLower();
				if (!val.EndsWith("/")) val += "/";
				if (val.EndsWith(".myshopify.com/")) val += "admin/";
				if (!val.EndsWith("/admin/"))
				{
					throw new PXSetPropertyException(ShopifyMessages.InvalidStoreUrl, PXErrorLevel.Warning);
				}
				e.NewValue = val;
			}
		}

		protected virtual void _(Events.FieldSelecting<BCBindingShopify, BCBindingShopify.shopifyApiVersion> e)
		{
			string COMMERCE_SHOPIFY_API_VERSION = "CommerceShopifyApiVersion";
			string ApiVersion = WebConfig.GetString(COMMERCE_SHOPIFY_API_VERSION, ShopifyConstants.ApiVersion_202201);
			e.ReturnValue = ApiVersion;
		}

		public override void _(Events.FieldUpdated<BCEntity, BCEntity.isActive> e)
		{
			base._(e);

			BCEntity row = e.Row;
			if (row == null || row.CreatedDateTime == null) return;

			if (row.IsActive == true)
			{
				if (row.EntityType == BCEntitiesAttribute.ProductWithVariant)
					if (PXAccess.FeatureInstalled<FeaturesSet.matrixItem>() == false)
					{
						EntityReturn(row.EntityType).IsActive = false;
						e.Cache.Update(EntityReturn(row.EntityType));
						throw new PXSetPropertyException(BCMessages.MatrixFeatureRequired);
					}
			}
		}

		protected void FetchDataFromShopify(BCBindingShopify row)
		{
			if (row == null || string.IsNullOrEmpty(row.ShopifyApiBaseUrl) ||
				(string.IsNullOrWhiteSpace(row.ShopifyAccessToken) && (string.IsNullOrEmpty(row.ShopifyApiKey) || string.IsNullOrEmpty(row.ShopifyApiPassword))))
				return;

			StoreRestDataProvider restClient = new StoreRestDataProvider(SPConnector.GetRestClient(row.ShopifyApiBaseUrl, row.ShopifyApiKey, row.ShopifyApiPassword, row.ShopifyAccessToken, row.StoreSharedSecret, row.ApiCallLimit));
			try
			{
				var store = restClient.Get();
				CurrentBindingShopify.Cache.SetValueExt(row, nameof(row.ShopifyStoreUrl), store.Domain);
				CurrentBindingShopify.Cache.SetValueExt(row, nameof(row.ShopifySupportCurrencies), string.Join(",", store.EnabledPresentmentCurrencies));
				CurrentBindingShopify.Cache.SetValueExt(row, nameof(row.ShopifyStoreTimeZone), store.Timezone);
				CurrentBindingShopify.Cache.IsDirty = true;
				CurrentBindingShopify.Cache.Update(row);
				CurrentStore.Cache.SetValueExt<BCBindingExt.defaultStoreCurrency>(CurrentStore.Current, store.Currency);
				CurrentStore.Cache.Update(CurrentStore.Current);

			}
			catch (Exception ex)
			{
				//throw new PXException(ex.Message);
			}
		}
		#endregion
	}
}
