using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.BigCommerce.API.WebDAV;
using PX.Api;
using PX.Data;
using RestSharp;
using PX.Commerce.Core;
using PX.Objects.CA;
using PX.Commerce.Objects;
using PX.Objects.GL;
using PX.Objects.CS;

namespace PX.Commerce.BigCommerce
{
	public class BCBigCommerceStoreMaint : BCStoreMaint
	{
		public PXSelect<BCBindingBigCommerce, Where<BCBindingBigCommerce.bindingID, Equal<Current<BCBinding.bindingID>>>> CurrentBindingBigCommerce;

		private const string ADMINPATHEXT = "/manage";
		private const string WEBDAVPATHEXT = "/dav";

		public BCBigCommerceStoreMaint()
		{
			base.Bindings.WhereAnd<Where<BCBinding.connectorType, Equal<BCConnector.bcConnectorType>>>();
		}

		#region Actions
		public PXAction<BCBinding> TestConnection;
		[PXButton(IsLockedOnToolbar = true)]
		[PXUIField(DisplayName = "Test Connection", Enabled = false)]
		protected virtual IEnumerable testConnection(PXAdapter adapter)
		{
			Actions.PressSave();

			BCBinding binding = Bindings.Current;
			BCBindingBigCommerce bindingBigCommerce = CurrentBindingBigCommerce.Current ?? CurrentBindingBigCommerce.Select();
			if (binding == null || bindingBigCommerce == null || bindingBigCommerce.StoreBaseUrl == null || bindingBigCommerce.StoreWDAVServerUrl == null)
			{
				throw new PXException(BCMessages.TestConnectionFailedParameters);
			}

			PXLongOperation.StartOperation(this, delegate
			{
				StoreRestDataProvider restClient = new StoreRestDataProvider(BCConnector.GetRestClient(bindingBigCommerce));
				BCWebDavClient webDavClient = BCConnector.GetWebDavClient(bindingBigCommerce);
				BCBigCommerceStoreMaint graph = PXGraph.CreateInstance<BCBigCommerceStoreMaint>();
				graph.Bindings.Current = binding;
				graph.CurrentBindingBigCommerce.Current = bindingBigCommerce;
				try
				{
					var store = restClient.Get();
					if (store == null || store.Id == null) throw new PXException(BigCommerceMessages.TestConnectionStoreNotFound);

					var folder = webDavClient.GetFolder();
					if (folder == null) throw new PXException(BigCommerceMessages.TestConnectionFolderNotFound);

					CurrentStore.Cache.SetValueExt<BCBindingExt.defaultStoreCurrency>(CurrentStore.Current, store.Currency);
					CurrentStore.Cache.Update(CurrentStore.Current);
					graph.CurrentBindingBigCommerce.Cache.SetValueExt(binding, nameof(BCBindingBigCommerce.BigCommerceStoreTimeZone), store.Timezone?.Name);
					graph.CurrentBindingBigCommerce.Cache.IsDirty = true;
					graph.CurrentBindingBigCommerce.Cache.Update(bindingBigCommerce);

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

		public override void _(Events.RowSelected<BCBindingExt> e)
		{
			base._(e);

			BCBindingExt row = e.Row;
			if (row == null) return;
		}

		protected virtual void _(Events.RowPersisting<BCBindingExt> e)
		{
			BCBindingExt row = e.Row as BCBindingExt; 
			BCBinding binding = CurrentBinding.Current ?? CurrentBinding.Select();
			BCBindingBigCommerce bindingBigCommerce = CurrentBindingBigCommerce.Current ?? CurrentBindingBigCommerce.Select();
			if (row == null || bindingBigCommerce == null || binding == null || string.IsNullOrEmpty(bindingBigCommerce.StoreBaseUrl) || string.IsNullOrWhiteSpace(bindingBigCommerce.StoreXAuthClient) || string.IsNullOrWhiteSpace(bindingBigCommerce.StoreXAuthToken))
				return;

			StoreRestDataProvider restClient = new StoreRestDataProvider(BCConnector.GetRestClient(bindingBigCommerce));
			try
			{
				var store = restClient.Get();

				CurrentStore.Cache.SetValueExt<BCBindingExt.defaultStoreCurrency>(row, store.Currency);
				CurrentStore.Cache.Update(row);
				CurrentBindingBigCommerce.Cache.SetValueExt(bindingBigCommerce, nameof(bindingBigCommerce.BigCommerceStoreTimeZone), store.Timezone?.Name);
				CurrentBindingBigCommerce.Cache.IsDirty = true;
				CurrentBindingBigCommerce.Cache.Update(bindingBigCommerce);
				
			}
			catch (Exception ex)
			{
				//throw new PXException(ex.Message);
			}

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



		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(BCConnectorsAttribute), "DefaultConnector", BCConnector.TYPE)]
		public virtual void _(Events.CacheAttached<BCBinding.connectorType> e) { }

		public override void _(Events.RowSelected<BCBinding> e)
		{
			base._(e);

			BCBinding row = e.Row as BCBinding;
			if (row == null) return;

			//Actions
			TestConnection.SetEnabled(row.BindingID > 0 && row.ConnectorType == BCConnector.TYPE);
		}
		public override void _(Events.RowInserted<BCBinding> e)
		{
			base._(e);

			bool dirty = CurrentBindingBigCommerce.Cache.IsDirty;
			CurrentBindingBigCommerce.Insert();
			CurrentBindingBigCommerce.Cache.IsDirty = dirty;
		}

		public virtual void _(Events.FieldVerifying<BCBindingBigCommerce, BCBindingBigCommerce.storeBaseUrl> e)
		{
			string val = e.NewValue?.ToString();
			if (val != null)
			{
				val = val.TrimEnd('/');
				for (int i = 0; i < 10; i++)
				{
					string pattern = "/v" + i;
					if (val.EndsWith(pattern)) val = val.Substring(0, val.LastIndexOf(pattern) + 1);
				}
				if (!val.EndsWith("/")) val += "/";

				e.NewValue = val;
			}
		}

		public virtual void _(Events.FieldUpdated<BCBindingBigCommerce, BCBindingBigCommerce.storeWDAVServerUrl> e)
		{
			BCBindingBigCommerce row = e.Row;
			if (row == null) return;

			if (!String.IsNullOrEmpty(row.StoreWDAVServerUrl))
			{
				var uri = new Uri(row.StoreWDAVServerUrl);
				var baseUri = uri.GetLeftPart(System.UriPartial.Authority);
				if (string.IsNullOrEmpty(baseUri)) return;

				if (row.StoreWDAVServerUrl.Length - baseUri.Length == WEBDAVPATHEXT.Length)
				{
					var attempWebDAV = row.StoreWDAVServerUrl.Substring(baseUri.Length, WEBDAVPATHEXT.Length);
					if (string.Equals(attempWebDAV, WEBDAVPATHEXT, StringComparison.InvariantCultureIgnoreCase))
					{
						row.StoreAdminUrl = baseUri + ADMINPATHEXT;
						return;
					}
				}
				row.StoreAdminUrl = baseUri + ADMINPATHEXT;
				row.StoreWDAVServerUrl = baseUri + WEBDAVPATHEXT;
			}
		}
		public virtual void _(Events.FieldUpdated<BCBindingBigCommerce, BCBindingBigCommerce.storeAdminUrl> e)
		{
			BCBindingBigCommerce row = e.Row;
			if (row == null) return;

			if (!String.IsNullOrEmpty(row.StoreAdminUrl))
			{
				var uri = new Uri(row.StoreAdminUrl);
				var baseUri = uri.GetLeftPart(System.UriPartial.Authority);
				if (string.IsNullOrEmpty(baseUri)) return;

				if (row.StoreAdminUrl.Length - baseUri.Length == ADMINPATHEXT.Length)
				{
					var attempAdm = row.StoreAdminUrl.Substring(baseUri.Length, ADMINPATHEXT.Length);
					if (string.Equals(attempAdm, ADMINPATHEXT, StringComparison.InvariantCultureIgnoreCase))
					{
						row.StoreWDAVServerUrl = baseUri + WEBDAVPATHEXT;
						return;
					}
				}
				row.StoreAdminUrl = baseUri + ADMINPATHEXT;
				row.StoreWDAVServerUrl = baseUri + WEBDAVPATHEXT;
			}
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
		#endregion

		#region Cache Attaced
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(BCItemNotAvailModes.List))]
		[PXStringList(
			new[] { BCItemNotAvailModes.DoNothing, BCItemNotAvailModes.DisableItem, BCItemNotAvailModes.PreOrderItem },
			new[] { BCCaptions.DoNothing, BCCaptions.DisableItem, BCCaptions.PreOrderItem })]
		public virtual void BCBindingExt_NotAvailMode_CacheAttached(PXCache sender) { }
		#endregion
	}
}
