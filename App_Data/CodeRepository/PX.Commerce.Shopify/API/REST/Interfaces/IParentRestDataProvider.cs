using System.Collections.Generic;

namespace PX.Commerce.Shopify.API.REST
{
    public interface IParentRestDataProvider<T>  
		where T : class 
    {   
        T Create(T entity);
		T Update(T entity, string id);
		bool Delete(string id);

        IEnumerable<T> GetAll(IFilter filter = null);
		T GetByID(string id);
    }
}
