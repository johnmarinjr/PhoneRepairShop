using PX.Commerce.Core;
using PX.Commerce.Core.Model;
using PX.Common;
using PX.Data;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace PX.Commerce.BigCommerce.API.REST
{
	public abstract class RestDataProviderBase
	{
		protected const int BATCH_SIZE = 10;
		protected const string ID_STRING = "id";
		protected const string PARENT_ID_STRING = "parent_id";
		protected const string OTHER_PARAM = "other_param";
		protected IBigCommerceRestClient _restClient;

		protected abstract string GetListUrl { get; }
		protected abstract string GetSingleUrl { get; }

		public RestDataProviderBase()
		{
		}

		public virtual T Create<T>(T entity, UrlSegments urlSegments = null)
			where T : class, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", entity)
				.Verbose("{CommerceCaption}: BigCommerce REST API - Creating new {EntityType} entity with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, typeof(T).ToString(), urlSegments?.ToString() ?? "none");

			String.Format("BigCommerce REST API - Creating new {0} entity with parameters {1}", typeof(T).ToString(), urlSegments?.ToString() ?? "none");
			var request = _restClient.MakeRequest(GetListUrl, urlSegments?.GetUrlSegments());

			return _restClient.Post(request, entity);
		}
		public virtual TE Create<T, TE>(T entity, UrlSegments urlSegments = null)
			where T : class, new()
			where TE : IEntityResponse<T>, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", entity)
				.Verbose("{CommerceCaption}: BigCommerce REST API - Creating new {EntityType} entity with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, typeof(T).ToString(), urlSegments?.ToString() ?? "none");
				
			var request = _restClient.MakeRequest(GetListUrl, urlSegments?.GetUrlSegments());
			
			TE result = _restClient.Post<T, TE>(request, entity);
			
			return result;

		}
		public virtual TE Create<T, TE>(List<T> entities, UrlSegments urlSegments = null)
			where T : class, new()
			where TE : IEntitiesResponse<T>, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", entities)
				.Verbose("{CommerceCaption}: BigCommerce REST API - Creating new {EntityType} entity with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, typeof(T).ToString(), urlSegments?.ToString() ?? "none");

			var request = _restClient.MakeRequest(GetListUrl, urlSegments?.GetUrlSegments());
			
			TE result = _restClient.Post<T, TE>(request, entities);
			
			return result;
		}

		public virtual T Update<T>(T entity, UrlSegments urlSegments)
			where T : class, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", entity)
				.Verbose("{CommerceCaption}: BigCommerce REST API - Creating new {EntityType} entity with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, typeof(T).ToString(), urlSegments?.ToString() ?? "none");

			var request = _restClient.MakeRequest(GetSingleUrl, urlSegments.GetUrlSegments());
			
			T result = _restClient.Put(request, entity);
			
			return result;
		}

		public virtual TE Update<T, TE>(T entity, UrlSegments urlSegments)
			where T : class, new()
			where TE : class, IEntityResponse<T>, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", entity)
				.Verbose("{CommerceCaption}: BigCommerce REST API - Updating {EntityType} entity with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, typeof(T).ToString(), urlSegments?.ToString() ?? "none");
			
			var request = _restClient.MakeRequest(GetSingleUrl, urlSegments?.GetUrlSegments());
			
			TE result = _restClient.Put<T, TE>(request, entity);
			
			return result;
		}
		public virtual TE Update<T, TE>(List<T> entities, UrlSegments urlSegments = null)
			where T : class, new()
			where TE : class, IEntitiesResponse<T>, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", entities)
				.Verbose("{CommerceCaption}: BigCommerce REST API - Updating {EntityType} entity with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, typeof(T).ToString(), urlSegments?.ToString() ?? "none");

			var request = _restClient.MakeRequest(GetSingleUrl, urlSegments?.GetUrlSegments());

			return _restClient.Put<T, TE>(request, entities);
		}

		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public virtual bool Delete(UrlSegments urlSegments)
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.Verbose("{CommerceCaption}: BigCommerce REST API - Deleting {EntityType} entry with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, this.GetType().ToString(), urlSegments?.ToString() ?? "none");
			
			var request = _restClient.MakeRequest(GetSingleUrl, urlSegments.GetUrlSegments());
			
			var result = _restClient.Delete(request);
			
			return result;
		}

		protected static UrlSegments MakeUrlSegments(string id)
		{
			var segments = new UrlSegments();
			segments.Add(ID_STRING, id);
			return segments;
		}

		protected static UrlSegments MakeParentUrlSegments(string parentId)
		{
			var segments = new UrlSegments();
			segments.Add(PARENT_ID_STRING, parentId);

			return segments;
		}


		protected static UrlSegments MakeUrlSegments(string id, string parentId)
		{
			var segments = new UrlSegments();
			segments.Add(PARENT_ID_STRING, parentId);
			segments.Add(ID_STRING, id);
			return segments;
		}
		protected static UrlSegments MakeUrlSegments(string id, string parentId, string param)
		{
			var segments = new UrlSegments();
			segments.Add(PARENT_ID_STRING, parentId);
			segments.Add(ID_STRING, id);
			segments.Add(OTHER_PARAM, param);
			return segments;
		}
	}

	public abstract class RestDataProviderV2 : RestDataProviderBase
	{
		public RestDataProviderV2() : base()
		{

		}

		protected virtual List<T> Get<T>(IFilter filter = null, UrlSegments urlSegments = null)
			where T : class, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.Verbose("{CommerceCaption}: BigCommerce REST API - Getting of {EntityType} entry with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, this.GetType().ToString(), urlSegments?.ToString() ?? "none");

			var request = _restClient.MakeRequest(GetListUrl, urlSegments?.GetUrlSegments());
			filter?.AddFilter(request);

			var entity = _restClient.Get<List<T>>(request);
			return entity;
		}

		public virtual IEnumerable<T> GetAll<T>(IFilter filter = null, UrlSegments urlSegments = null)
			where T : class, new()
		{
			var localFilter = filter ?? new Filter();
			var needGet = true;

			localFilter.Page = 1;
			localFilter.Limit = 50;

			while (needGet)
			{
				List<T> entities = Get<T>(localFilter, urlSegments);

				if (entities == null) yield break;
				foreach (T entity in entities)
				{
					yield return entity;
				}
				localFilter.Page++;
				needGet = localFilter.Limit == entities.Count;
			}
		}

		public virtual T GetByID<T>(UrlSegments urlSegments) where T : class, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.Verbose("{CommerceCaption}: BigCommerce REST API - Getting by ID {EntityType} entry with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, this.GetType().ToString(), urlSegments?.ToString() ?? "none");

			var request = _restClient.MakeRequest(GetSingleUrl, urlSegments.GetUrlSegments());

			var entity = _restClient.Get<T>(request);

			return entity;
		}
	}

	public abstract class RestDataProviderV3 : RestDataProviderBase
	{
		public RestDataProviderV3() : base()
		{

		}

		protected virtual TE Get<T, TE>(IFilter filter = null, UrlSegments urlSegments = null)
			where T : class, new()
			where TE : IEntitiesResponse<T>, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.Verbose("{CommerceCaption}: BigCommerce REST API - Getting {EntityType} entry with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, this.GetType().ToString(), urlSegments?.ToString() ?? "none");

			var request = _restClient.MakeRequest(GetListUrl, urlSegments?.GetUrlSegments());
			filter?.AddFilter(request);

			var response = _restClient.GetList<T, TE>(request);
			return response;
		}

		public virtual IEnumerable<T> GetAll<T, TE>(IFilter filter = null, UrlSegments urlSegments = null)
			where T : class, new()
			where TE : IEntitiesResponse<T>, new()
		{
			var localFilter = filter ?? new Filter();
			var needGet = true;

			localFilter.Page = 1;
			localFilter.Limit = 50;
			TE entity = default;
			while (needGet)
			{
				entity = Get<T, TE>(localFilter, urlSegments);

				if (entity?.Data == null) yield break;
				foreach (T data in entity.Data)
				{
					yield return data;
				}

				needGet = localFilter.Page < entity.Meta.Pagination.TotalPages;
				localFilter.Page++;
			}
		}

		public virtual TE GetByID<T, TE>(UrlSegments urlSegments, IFilter filter = null)
			where T : class, new()
			where TE : IEntityResponse<T>, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.Verbose("{CommerceCaption}: BigCommerce REST API - Getting by ID {EntityType} entry with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, typeof(T).ToString(), urlSegments?.ToString() ?? "none");

			var request = _restClient.MakeRequest(GetSingleUrl, urlSegments.GetUrlSegments());
			if (filter != null)
				filter.AddFilter(request);

			TE result = _restClient.Get<T, TE>(request);

			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", APIHelper.DesctructObject(result), destructureObjects: true)
				.Verbose("{CommerceCaption}: BigCommerce REST API - Returned By ID", BCCaptions.CommerceLogCaption);

			return result;
		}

		public virtual TE Create<T, TE>(TE entity, UrlSegments urlSegments = null)
			where T : class, new()
			where TE : IEntityResponse<T>, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", APIHelper.DesctructObject(entity), destructureObjects: true)
				.Verbose("{CommerceCaption}: BigCommerce REST API - Creating of ID {EntityType} entry with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, typeof(T).ToString(), urlSegments?.ToString() ?? "none");

			var request = _restClient.MakeRequest(GetListUrl, urlSegments?.GetUrlSegments());

			TE result = _restClient.Post<T, TE>(request, entity);

			return result;
		}

		public virtual TE Update<T, TE>(TE entity, UrlSegments urlSegments = null)
			where T : class, new()
			where TE : IEntityResponse<T>, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", APIHelper.DesctructObject(entity), destructureObjects: true)
				.Verbose("{CommerceCaption}: BigCommerce REST API - Updating of {EntityType} entry with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, typeof(T).ToString(), urlSegments?.ToString() ?? "none");

			var request = _restClient.MakeRequest(GetSingleUrl, urlSegments.GetUrlSegments());

			TE result = _restClient.Put<T, TE>(request, entity);

			return result;
		}

		public virtual TE UpdateAll<T, TE>(TE entities, UrlSegments urlSegments = null)
			where T : class, new()
			where TE : IEntitiesResponse<T>, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", APIHelper.DesctructObject(entities), destructureObjects: true)
				.Verbose("{CommerceCaption}: BigCommerce REST API - Updating of {EntityType} entry with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, typeof(T).ToString(), urlSegments?.ToString() ?? "none");

			var request = _restClient.MakeRequest(GetSingleUrl, urlSegments.GetUrlSegments());

			return _restClient.PutList<T, TE>(request, entities);
		}
		public virtual void UpdateAll<T, TE>(TE entities, UrlSegments urlSegments, Action<ItemProcessCallback<T>> callback)
			where T : class, new()
			where TE : IEntitiesResponse<T>, new()
		{
			TE batch = new TE();
			batch.Meta = entities.Meta;

			int index = 0;
			for (; index < entities.Data.Count; index++)
			{
				if (index % BATCH_SIZE == 0 && batch.Data.Count > 0)
				{
					UpdateBatch<T, TE>(batch, urlSegments, index - batch.Data.Count, callback);

					batch.Data.Clear();
				}
				batch.Data.Add(entities.Data[index]);
			}
			if (batch.Data.Count > 0)
			{
				UpdateBatch<T, TE>(batch, urlSegments, index - batch.Data.Count, callback);
			}
		}

		protected void UpdateBatch<T, TE>(TE batch, UrlSegments urlSegments, Int32 startIndex, Action<ItemProcessCallback<T>> callback)
			where T : class, new()
			where TE : IEntitiesResponse<T>, new()
		{
			_restClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", APIHelper.DesctructObject(batch), destructureObjects: true)
				.Verbose("{CommerceCaption}: BigCommerce REST API - Batch Updating of {EntityType} entry with parameters {UrlSegments}", BCCaptions.CommerceLogCaption, typeof(T).ToString(), urlSegments?.ToString() ?? "none");

			while (true)
				try
				{
					RestRequest request = _restClient.MakeRequest(GetListUrl, urlSegments.GetUrlSegments());

					TE response = _restClient.PutList<T, TE>(request, batch);
					if (response == null) return;
					for (int i = 0; i < response.Data.Count; i++)
					{
						T item = response.Data[i];
						callback(new ItemProcessCallback<T>(startIndex + i, item));
					}
					break;
				}
				catch (RestException ex)
				{
					if (ex?.ResponceStatusCode == default(HttpStatusCode).ToString())
					{
						throw;
					}
					else
					{
						for (int i = 0; i < batch.Data.Count; i++)
						{
							T item = batch.Data[i];
							callback(new ItemProcessCallback<T>(startIndex + i, ex, batch.Data));
						}
						break;
					}
				}
		}

		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public virtual bool Delete(IFilter filter = null)
		{
			var request = _restClient.MakeRequest(GetSingleUrl);
			filter?.AddFilter(request);

			var response = _restClient.Delete(request);
			return response;
		}
	}
}
