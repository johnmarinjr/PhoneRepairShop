using CommonServiceLocator;
using Newtonsoft.Json;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.BigCommerce.API.WebDAV;
using PX.Commerce.Core;
using PX.Commerce.Objects;
using PX.Data;
using PX.Objects.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.BigCommerce
{
	#region BCConnectorFactory
	/// <inheritdoc cref="IConnectorFactory"/>
	public class BCConnectorFactory : BaseConnectorFactory<BCConnector>, IConnectorFactory
	{
		/// <inheritdoc cref="IConnectorFactory.Type"/>
		public override string Type => BCConnector.TYPE;
		/// <inheritdoc cref="IConnectorFactory.Description"/>
		public override string Description => BCConnector.NAME;
		/// <inheritdoc cref="IConnectorFactory.Enabled"/>
		public override bool Enabled => CommerceFeaturesHelper.BigCommerceConnector;

		public BCConnectorFactory(IEnumerable<IProcessorsFactory> processors)
			: base(processors)
		{
		}

		public IConnectorDescriptor GetDescriptor()
		{
			return new BCConnectorDescriptor(_processors.Values.ToList());
		}
	}
	#endregion
	#region BCProcessorsFactory
	/// <inheritdoc cref="IProcessorsFactory"/>
	public class BCProcessorsFactory : IProcessorsFactory
	{
		public string ConnectorType => BCConnector.TYPE;

		public IEnumerable<KeyValuePair<Type, Int32>> GetProcessorTypes()
		{
			yield return new KeyValuePair<Type, int>(typeof(BCPriceClassProcessor), 10);
			yield return new KeyValuePair<Type, int>(typeof(BCCustomerProcessor), 20);
			yield return new KeyValuePair<Type, int>(typeof(BCLocationProcessor), 30);
			yield return new KeyValuePair<Type, int>(typeof(BCCategoryProcessor), 40);
			yield return new KeyValuePair<Type, int>(typeof(BCStockItemProcessor), 50);
			yield return new KeyValuePair<Type, int>(typeof(BCNonStockItemProcessor), 60);
			yield return new KeyValuePair<Type, int>(typeof(BCTemplateItemProcessor), 70);
			yield return new KeyValuePair<Type, int>(typeof(BCSalesPriceProcessor), 80);
			yield return new KeyValuePair<Type, int>(typeof(BCPriceListProcessor), 90);
			yield return new KeyValuePair<Type, int>(typeof(BCImageProcessor), 100);
			yield return new KeyValuePair<Type, int>(typeof(BCAvailabilityProcessor), 110);
			yield return new KeyValuePair<Type, int>(typeof(BCSalesOrderProcessor), 120);
			yield return new KeyValuePair<Type, int>(typeof(BCPaymentProcessor), 130);
			yield return new KeyValuePair<Type, int>(typeof(BCShipmentProcessor), 140);
			yield return new KeyValuePair<Type, int>(typeof(BCRefundsProcessor), 150);
		}
	}
	#endregion

	/// <inheritdoc cref="IConnector"/>
	public class BCConnector : BCConnectorBase<BCConnector>, IConnector
	{
		#region IConnector
		public const string TYPE = "BCC";
		public const string NAME = "BigCommerce";

		/// <inheritdoc cref="IConnector.ConnectorType"/>
		public override string ConnectorType { get => TYPE; }
		/// <inheritdoc cref="IConnector.ConnectorName"/>
		public override string ConnectorName { get => NAME; }
		public class bcConnectorType : PX.Data.BQL.BqlString.Constant<bcConnectorType>
		{
			public bcConnectorType() : base(TYPE) { }
		}

		public virtual IEnumerable<TInfo> GetExternalInfo<TInfo>(string infoType, int? bindingID)
			where TInfo : class
		{
			return new List<TInfo>();
		}

		public virtual IEnumerable<TInfo> GetDefaultShippingMethods<TInfo>(int? bindingID)
			where TInfo : class
		{
			BCBindingExt store = BCBindingExt.PK.Find(this, bindingID);
			BCBinding binding = BCBinding.PK.Find(this, ConnectorType, bindingID);
			if (binding == null || store == null) return null;
			try
			{
				List<TInfo> result = new List<TInfo>();
				var shippingZones = new List<ShippingZoneData>();

				List<BCDefaultShippingMapping> defaultShippingMethods = PXSelect<BCDefaultShippingMapping,
					Where<BCDefaultShippingMapping.connectorType, Equal<Required<BCDefaultShippingMapping.connectorType>>>>
					.Select(this, ConnectorType).Select(x => x.GetItem<BCDefaultShippingMapping>()).ToList();

				foreach (BCDefaultShippingMapping method in defaultShippingMethods)
				{
					var currentShippingZone = shippingZones.FirstOrDefault(x => x.Name == method.ShippingZone);
					if (currentShippingZone == null)
					{
						currentShippingZone = new ShippingZoneData { Enabled = true, Name = method.ShippingZone, ShippingMethods = new List<IShippingMethod>() };
						shippingZones.Add(currentShippingZone);
					}

					var shippingMethod = new ShippingMethod() { Name = method.ShippingMethod, Type = ConnectorName, Enabled = true };
					currentShippingZone.ShippingMethods.Add(shippingMethod);
				}

				result = shippingZones.Cast<TInfo>().ToList();
				return result;
			}
			catch (Exception ex)
			{
				LogError(new BCLogTypeScope(typeof(BCConnector)), ex);
			}

			return null;
		}

		public virtual IEnumerable<TInfo> GetDefaultPaymentMethods<TInfo>(int? bindingID)
			where TInfo : class
		{
			BCBindingExt store = BCBindingExt.PK.Find(this, bindingID);
			BCBinding binding = BCBinding.PK.Find(this, ConnectorType, bindingID);
			if (binding == null || store == null) return null;
			try
			{
				List<TInfo> result = new List<TInfo>();
				var defaultCurrency = store.DefaultStoreCurrency ?? (binding.BranchID != null ? PX.Objects.GL.Branch.PK.Find(this, binding.BranchID)?.BaseCuryID : null);
				if (defaultCurrency != null)
				{
					List<BCDefaultPaymentMapping> defaultPaymentMethods = PXSelect<BCDefaultPaymentMapping,
						Where<BCDefaultPaymentMapping.connectorType, Equal<Required<BCDefaultPaymentMapping.connectorType>>>>
						.Select(this, ConnectorType).Select(x => x.GetItem<BCDefaultPaymentMapping>()).ToList();

					foreach (BCDefaultPaymentMapping method in defaultPaymentMethods)
					{
						object paymentItem = new PaymentMethod() { Name = method.StorePaymentMethod, CreatePaymentfromOrder = method.CreatePaymentfromOrder ?? false, Currency = defaultCurrency };
						result.Add((TInfo)paymentItem);
					}
				}
				return result;
			}
			catch (Exception ex)
			{
				LogError(new BCLogTypeScope(typeof(BCConnector)), ex);
			}

			return null;
		}
		#endregion

		#region Navigation
		public void NavigateExtern(ISyncStatus status)
		{
			if (status?.ExternID == null) return;

			EntityInfo info = GetEntities().FirstOrDefault(e => e.EntityType == status.EntityType);
			BCBindingBigCommerce bCBindingBigCommerce = BCBindingBigCommerce.PK.Find(this, status.BindingID);

			if (string.IsNullOrEmpty(bCBindingBigCommerce?.StoreAdminUrl) || string.IsNullOrEmpty(info.URL)) return;

			string[] parts = status.ExternID.Split(new char[] { ';' });
			string url = string.Format(info.URL, parts.Length > 2 ? parts.Take(2).ToArray() : parts);
			string redirectUrl = bCBindingBigCommerce.StoreAdminUrl.TrimEnd('/') + "/" + url;

			throw new PXRedirectToUrlException(redirectUrl, PXBaseRedirectException.WindowMode.New, string.Empty);

		}
		#endregion

		#region Process
		public virtual ConnectorOperationResult Process(ConnectorOperation operation, Int32?[] syncIDs = null)
		{
			LogInfo(operation.LogScope(), BCMessages.LogConnectorStarted, PXMessages.LocalizeNoPrefix(NAME));

			EntityInfo info = GetEntities().FirstOrDefault(e => e.EntityType == operation.EntityType);
			using (IProcessor graph = (IProcessor)PXGraph.CreateInstance(info.ProcessorType))
			{
				graph.Initialise(this, operation);
				return graph.Process(syncIDs);
			}
		}

		public DateTime GetSyncTime(ConnectorOperation operation)
		{
			BCBindingBigCommerce binding = BCBindingBigCommerce.PK.Find(this, operation.Binding);

			//Big Commerce Time
			StoreTimeRestDataProvider storeTime = new StoreTimeRestDataProvider(GetRestClient(binding));
			DateTime syncTime = storeTime.Get()?.CurrentDateTime ?? default(DateTime);

			//Acumatica Time
			PXDatabase.SelectDate(out DateTime dtLocal, out DateTime dtUtc);
			dtLocal = PX.Common.PXTimeZoneInfo.ConvertTimeFromUtc(dtUtc, PX.Common.LocaleInfo.GetTimeZone());

			if (syncTime > dtLocal) syncTime = dtLocal;

			return syncTime;
		}
		#endregion

		#region Notifications
		public override void StartWebHook(String baseUrl, BCWebHook hook)
		{
			BCBinding store = BCBinding.PK.Find(this, hook.ConnectorType, hook.BindingID);
			BCBindingBigCommerce storeBigCommerce = BCBindingBigCommerce.PK.Find(this, hook.BindingID);

			WebHookRestDataProvider restClient = new WebHookRestDataProvider(GetRestClient(storeBigCommerce));

			//URL and HASH
			string url = new Uri(baseUrl, UriKind.RelativeOrAbsolute).ToString();
			if (url.EndsWith("/"))
				url = url.TrimEnd('/');
			url += hook.Destination;
			string hashcode = hook.ValidationHash ?? String.Concat(PX.Data.Update.PXCriptoHelper.CalculateSHA(Guid.NewGuid().ToString()).Select(b => b.ToString("X2")));

			//Searching for the existing hook
			if (hook.HookRef != null)
			{
				WebHookData data = restClient.GetByID(hook.HookRef.ToString());
				if (data != null)
				{
					if (data.IsActive != true
						|| data.StoreHash != hook.StoreHash
						|| data.Destination != url
						|| !data.Headers.TryGetValue("validation", out String validation) || validation != hashcode)
						restClient.Delete(Convert.ToInt32(hook.HookRef.Value));
					else
					{
						hook.IsActive = true;

						Hooks.Update(hook);
						Actions.PressSave();

						return;
					}
				}
			}
			else
			{
				foreach (WebHookData data in restClient.GetAll())
				{
					if (data.Scope == hook.Scope && data.Destination == url)
					{
						data.Headers.TryGetValue("validation", out String validation);

						if (data.IsActive == false || validation != hashcode)
							restClient.Delete(Convert.ToInt32(data.Id.Value));
						else
						{
							//Saving missing hook
							hook.IsActive = true;
							hook.HookRef = data.Id;
							hook.StoreHash = data.StoreHash;
							hook.ValidationHash = validation;

							Hooks.Update(hook);
							Actions.PressSave();

							return;
						}
					}
				}
			}

			//Create a new Hook
			WebHookData webHook = new WebHookData();
			webHook.Scope = hook.Scope;
			webHook.Destination = url;
			webHook.IsActive = hook.IsActive ?? false;

			String companyName = PXAccess.GetCompanyName();
			webHook.Headers = new Dictionary<string, string>();
			webHook.Headers["type"] = TYPE;
			webHook.Headers["validation"] = hashcode;
			if (!String.IsNullOrEmpty(companyName)) webHook.Headers["company"] = companyName;

			webHook = restClient.Create(webHook);

			//Saving
			hook.IsActive = true;
			hook.HookRef = webHook.Id;
			hook.StoreHash = webHook.StoreHash;
			hook.ValidationHash = hashcode;

			Hooks.Update(hook);
			Actions.PressSave();
		}
		public override void StopWebHook(String baseUrl, BCWebHook hook)
		{
			BCBinding store = BCBinding.PK.Find(this, hook.ConnectorType, hook.BindingID);
			BCBindingBigCommerce storeBigCommerce = BCBindingBigCommerce.PK.Find(this, hook.BindingID);

			WebHookRestDataProvider restClient = new WebHookRestDataProvider(GetRestClient(storeBigCommerce));

			if (hook.HookRef != null)
			{
				WebHookData data = restClient.GetByID(hook.HookRef.ToString());
				if (data != null)
				{
					restClient.Delete(Convert.ToInt32(hook.HookRef.Value));
				}
			}
			else if(baseUrl != null)
			{
				string url = new Uri(baseUrl, UriKind.RelativeOrAbsolute).ToString();
				if (url.EndsWith("/") && hook.Destination.StartsWith("/")) url = url.TrimEnd('/') + hook.Destination;

				foreach (WebHookData data in restClient.GetAll())
				{
					if (data.Scope == hook.Scope && data.Destination == url && data.StoreHash == hook.StoreHash)
					{
						restClient.Delete(data.Id.Value);
					}
				}
			}

			//Saving
			hook.IsActive = false;
			hook.HookRef = null;

			Hooks.Update(hook);
			Actions.PressSave();
		}

		public virtual void ProcessHook(IEnumerable<BCExternQueueMessage> messages)
		{
			Dictionary<RecordKey, RecordValue<String>> toProcess = new Dictionary<RecordKey, RecordValue<String>>();
			foreach (BCExternQueueMessage message in messages)
			{
				WebHookMessage jResult = JsonConvert.DeserializeObject<WebHookMessage>(message.Json);

				string scope = jResult.Scope;
				string producer = jResult.Producer;
				string data = jResult.Data;
				DateTime? created = jResult.DateCreatedUT.ToDate();
				String storehash = producer.Substring(producer.LastIndexOf("/") + 1);

				foreach (BCWebHook hook in PXSelect<BCWebHook,
					Where<BCWebHook.connectorType, Equal<BCConnector.bcConnectorType>,
						And<BCWebHook.storeHash, Equal<Required<BCWebHook.storeHash>>,
						And<BCWebHook.scope, Equal<Required<BCWebHook.scope>>>>>>.Select(this, storehash, scope))
				{
					if (hook.ValidationHash != message.Validation)
					{
						LogError(new BCLogTypeScope(typeof(BCConnector)), new PXException(BCMessages.WrongValidationHash, storehash ?? "", scope));
						continue;
					}

					foreach (EntityInfo info in this.GetEntities().Where(e => e.ExternRealtime.Supported && e.ExternRealtime.WebHookType != null && e.ExternRealtime.WebHooks.Contains(scope)))
					{
						BCBinding binding = BCBinding.PK.Find(this, TYPE, hook.BindingID.Value);
						BCEntity entity = BCEntity.PK.Find(this, TYPE, hook.BindingID.Value, info.EntityType);

						if (binding == null || !(binding.IsActive ?? false) || entity == null || !(entity.IsActive ?? false)
							|| entity?.ImportRealTimeStatus != BCRealtimeStatusAttribute.Run || entity.Direction == BCSyncDirectionAttribute.Export)
							continue;

						Object obj = JsonConvert.DeserializeObject(data, info.ExternRealtime.WebHookType);
						String id = obj?.ToString();
						if (obj == null || id == null) continue;

						toProcess[new RecordKey(entity.ConnectorType, entity.BindingID, entity.EntityType, id)] 
							= new RecordValue<String>((entity.RealTimeMode == BCSyncModeAttribute.PrepareAndProcess), (DateTime)created, message.Json);
					}
				}
			}

			Dictionary<Int32, ConnectorOperation> toSync = new Dictionary<int, ConnectorOperation>();
			foreach (KeyValuePair<RecordKey, RecordValue<String>> pair in toProcess)
			{
				//Trigger Provider
				ConnectorOperation operation = new ConnectorOperation();
				operation.ConnectorType = pair.Key.ConnectorType;
				operation.Binding = pair.Key.BindingID.Value;
				operation.EntityType = pair.Key.EntityType;
				operation.PrepareMode = PrepareMode.None;
				operation.SyncMethod = SyncMode.Changed;

				Int32? syncID = null;
				EntityInfo info = this.GetEntities().FirstOrDefault(e => e.EntityType == pair.Key.EntityType);

				//Performance optimization - skip push if no value for that
				BCSyncStatus status = null;
				if (pair.Value.Timestamp != null)
				{
					status = BCSyncStatus.ExternIDIndex.Find(this, operation.ConnectorType, operation.Binding, operation.EntityType, pair.Key.RecordID);
					//Let the processor decide if deleted entries should resync - do not filter out deleted statuses
					if (status != null && (status.LastOperation == BCSyncOperationAttribute.Skipped
						|| (status.ExternTS != null && pair.Value.Timestamp <= status.ExternTS)))
						continue;
				}

				if (status == null || status.PendingSync == null || status.PendingSync == false)
				{
					using (IProcessor graph = (IProcessor)PXGraph.CreateInstance(info.ProcessorType))
					{
						syncID = graph.ProcessHook(this, operation, pair.Key.RecordID, pair.Value.Timestamp, pair.Value.ExternalInfo, status);
					}
				}
				else if (status.SyncInProcess == false) syncID = status.SyncID;

				if (syncID != null && pair.Value.AutoSync) toSync[syncID.Value] = operation;
			}
			if (toSync.Count > 0)
			{
				PXLongOperation.StartOperation(this, delegate ()
				{
					foreach (KeyValuePair<Int32, ConnectorOperation> pair in toSync)
					{
						IConnector connector = ConnectorHelper.GetConnector(pair.Value.ConnectorType);
						try
						{
							connector.Process(pair.Value, new Int32?[] { pair.Key });
						}
						catch (Exception ex)
						{
							connector.LogError(pair.Value.LogScope(pair.Key), ex);
						}
					}
				});
				PXLongOperation.WaitCompletion(this);
			}

		}
		#endregion

		#region Public Static
		public static BCRestClient GetRestClient(BCBindingBigCommerce binding)
		{
			return GetRestClient(binding.StoreBaseUrl, binding.StoreXAuthClient, binding.StoreXAuthToken);
		}
		public static BCRestClient GetRestClient(String url, String clientID, String token)
		{
			RestOptions options = new RestOptions
			{
				BaseUri = url,
				XAuthClient = clientID,
				XAuthTocken = token
			};
			JsonSerializer serializer = new JsonSerializer
			{
				MissingMemberHandling = MissingMemberHandling.Ignore,
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore,
				DateFormatHandling = DateFormatHandling.IsoDateFormat,
				DateTimeZoneHandling = DateTimeZoneHandling.Unspecified,
                ContractResolver = new Core.REST.GetOnlyContractResolver()
            };
			RestJsonSerializer restSerializer = new RestJsonSerializer(serializer);
			BCRestClient client = new BCRestClient(restSerializer, restSerializer, options,
				ServiceLocator.Current.GetInstance<Serilog.ILogger>());

			return client;
		}
		public static BCWebDavClient GetWebDavClient(BCBindingBigCommerce binding)
		{
			WebDAVOptions options = new WebDAVOptions()
			{
				ServerHttpsUri = binding.StoreWDAVServerUrl,
				ClientUser = binding.StoreWDAVClientUser,
				ClientPassword = binding.StoreWDAVClientPass
			};

			BCWebDavClient client = new BCWebDavClient(options);

			return client;
		}

		#endregion
	}

	#region BCConnectorDescriptor
	/// <inheritdoc cref="IConnectorDescriptor"/>
	public class BCConnectorDescriptor : IConnectorDescriptor
	{
		protected IList<EntityInfo> _entities;

		public BCConnectorDescriptor(IList<EntityInfo> entities)
		{
			_entities = entities;
		}

		/// <inheritdoc cref="IConnectorDescriptor.GenerateExternID(BCExternNotification)"/>
		public virtual Guid? GenerateExternID(BCExternNotification message)
		{
			WebHookMessage jResult = JsonConvert.DeserializeObject<WebHookMessage>(message.Json);

			string scope = jResult.Scope;
			string producer = jResult.Producer;
			string data = jResult.Data;
			string storehash = producer.Substring(producer.LastIndexOf("/") + 1);
			EntityInfo info = _entities.FirstOrDefault(e => e.ExternRealtime.Supported && e.ExternRealtime.WebHookType != null && e.ExternRealtime.WebHooks.Contains(scope));
			Object obj = JsonConvert.DeserializeObject(data, info.ExternRealtime.WebHookType);
			String id = obj?.ToString();

			if (obj == null || id == null) return null;

			Byte[] bytes = new Byte[16];
			BitConverter.GetBytes(BCConnector.TYPE.GetHashCode()).CopyTo(bytes, 0); //Connector
			BitConverter.GetBytes(info.EntityType.GetHashCode()).CopyTo(bytes, 4); //EntityType
			BitConverter.GetBytes(storehash.GetHashCode()).CopyTo(bytes, 8); //Store
			BitConverter.GetBytes(id.GetHashCode()).CopyTo(bytes, 12); //ID

			return new Guid(bytes);
		}
		/// <inheritdoc cref="IConnectorDescriptor.GenerateLocalID(BCLocalNotification)"
		public virtual Guid? GenerateLocalID(BCLocalNotification message)
		{
            Guid? noteId = message.Fields.First(v => v.Key.EndsWith("NoteID", StringComparison.InvariantCultureIgnoreCase) && v.Value != null).Value.ToGuid();
            Byte[] bytes = new Byte[16];
            BitConverter.GetBytes(BCConnector.TYPE.GetHashCode()).CopyTo(bytes, 0); //Connector
            BitConverter.GetBytes(message.Entity.GetHashCode()).CopyTo(bytes, 4); //EntityType
            BitConverter.GetBytes(message.Binding.GetHashCode()).CopyTo(bytes, 8); //Store
            BitConverter.GetBytes(noteId.GetHashCode()).CopyTo(bytes, 12); //ID
            return new Guid(bytes);
        }

		/// <inheritdoc cref="IConnectorDescriptor.GetExternalFields(int?, string)"
		public List<Tuple<String, String, String>> GetExternalFields(String type, Int32? binding, String entity)
		{
			List<Tuple<String, String, String>> fieldsList = new List<Tuple<string, string, string>>();
			if (entity != BCEntitiesAttribute.Customer && entity != BCEntitiesAttribute.Address) return fieldsList;

			return fieldsList;
		}
	}
	#endregion
}
