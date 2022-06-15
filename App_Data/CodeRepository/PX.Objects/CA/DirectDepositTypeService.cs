using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.CA
{
	public class DirectDepositTypeService
	{
		private IEnumerable<IDirectDepositType> _directDepositTypes;
		public DirectDepositTypeService(IEnumerable<IDirectDepositType> directDepositTypes)
		{
			_directDepositTypes = directDepositTypes;
		}

		public IEnumerable<DirectDepositType> GetDirectDepositTypes()
		{
			foreach (var type in _directDepositTypes)
			{
				if (type.IsActive())
				{
					yield return type.GetDirectDepositType();
				}
			}
		}

		public IEnumerable<PaymentMethodDetail> GetDefaults(string code)
		{
			foreach (var type in _directDepositTypes)
			{
				if (type.IsActive())
				{
					var currentType = type.GetDirectDepositType();
					if (currentType.Code == code)
					{
						return type.GetDefaults();
					}
				}
			}
			return Enumerable.Empty<PaymentMethodDetail>();
		}

		public void SetPaymentMethodDefaults(PXCache cache)
		{
			PaymentMethod paymentMethod = (PaymentMethod)cache.Current;

			foreach (var type in _directDepositTypes)
			{
				if (type.IsActive())
				{
					var currentType = type.GetDirectDepositType();
					if (currentType.Code == paymentMethod.DirectDepositFileFormat)
					{
						type.SetPaymentMethodDefaults(cache);
						break;
					}
				}
			}
		}
	}
}
