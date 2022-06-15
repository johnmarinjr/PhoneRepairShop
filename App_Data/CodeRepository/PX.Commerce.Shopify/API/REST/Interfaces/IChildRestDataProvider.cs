using System.Collections.Generic;

namespace PX.Commerce.Shopify.API.REST
{
    public interface IChildReadOnlyRestDataProvider<T> where T : class
    {
        IEnumerable<T> GetAll(string parentId, IFilter filter = null);
        IEnumerable<T> GetAllWithoutParent(IFilter filter = null);
		T GetByID(string parentId, string id);
	}

    public interface IChildRestDataProvider<T> : IChildReadOnlyRestDataProvider<T> where T : class
    {
		T Create(T entity, string parentId);
		T Update(T entity, string parentId, string id);
		bool Delete(string parentId, string id);
	}
}
