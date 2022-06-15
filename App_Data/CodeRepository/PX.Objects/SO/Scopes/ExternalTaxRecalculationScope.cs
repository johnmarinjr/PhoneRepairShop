using PX.Common;
using PX.Data;
using System;

namespace PX.Objects.SO
{
	public class ExternalTaxRecalculationScope : IDisposable
	{
		protected bool _disposed = false;

		public class Context
		{
			public int ReferenceCounter { get; set; }
		}

		public ExternalTaxRecalculationScope()
		{
			Context currentContext = PXContext.GetSlot<Context>();
			if (currentContext == null)
			{
				currentContext = new Context();
				PXContext.SetSlot(currentContext);
			}
			currentContext.ReferenceCounter++;
		}

		public void Dispose()
		{
			if (_disposed)
				throw new PXObjectDisposedException();

			_disposed = true;

			Context currentContext = PXContext.GetSlot<Context>();
			currentContext.ReferenceCounter--;

			if (currentContext.ReferenceCounter == 0)
				PXContext.SetSlot<Context>(null);
		}

		public static bool IsScoped()
		{
			var currentContext = PXContext.GetSlot<Context>();
			return currentContext?.ReferenceCounter > 0;
		}
	}
}
