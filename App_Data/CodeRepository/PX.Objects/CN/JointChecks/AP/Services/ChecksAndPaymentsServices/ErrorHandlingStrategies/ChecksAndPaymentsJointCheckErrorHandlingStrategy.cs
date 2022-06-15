using PX.Data;
using PX.Objects.AP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation
{
    public class ChecksAndPaymentsJointCheckErrorHandlingStrategy : IJointCheckErrorHandlingStrategy
    {
        private readonly APPaymentEntry graph;

		public ChecksAndPaymentsJointCheckErrorHandlingStrategy(APPaymentEntry graph)
		{
			this.graph = graph;
		}

		public APPaymentEntry Graph => graph;

		public void HandleError<TField>(IErrorHandlingStrategyParams parameters) where TField : IBqlField
		{
			if (parameters.ThrowError && parameters.ShowError && parameters.WithFieldValue)
			{
				var paramObject = (ShowAndThrowErrorWithFieldValueParams)parameters;
				ShowErrorMessageWithField<TField>(paramObject.Entity, paramObject.FieldValue, paramObject.ErrorMessage, paramObject.Args);
				ThrowGeneralError(paramObject.ErrorFieldName);
			}
			if (parameters.ThrowError && parameters.ShowError)
			{
				var paramObject = (ShowAndThrowErrorParams)parameters;
				ShowErrorMessage<TField>(paramObject.Entity, paramObject.ErrorMessage, paramObject.Args);
				ThrowGeneralError(paramObject.ErrorFieldName);
			}
			if (parameters.ShowError && parameters.WithFieldValue)
			{
				var paramObject = (ShowErrorOnlyWithFieldValueParams)parameters;
				ShowErrorMessageWithField<TField>(paramObject.Entity, paramObject.FieldValue, paramObject.ErrorMessage, paramObject.Args);
			}
			if (parameters.ShowError)
			{
				var paramObject = (ShowErrorOnlyParams)parameters;
				ShowErrorMessage<TField>(paramObject.Entity, paramObject.ErrorMessage, paramObject.Args);
			}
            else
			{
				var paramObject = (ThrowErrorOnlyParams)parameters;
				ThrowGeneralError(paramObject.ErrorFieldName);
			}
		}

		private void ShowErrorMessage<TField>(object entity, string format, params object[] args)
			where TField : IBqlField
		{
			var cache = Graph.Caches[entity.GetType()];
			var fieldValue = cache.GetValue<TField>(entity);
			ShowErrorMessageWithField<TField>(entity, fieldValue, format, args);
		}

		private void ShowErrorMessageWithField<TField>(object entity, object fieldValue, string format, params object[] args)
			where TField : IBqlField
		{
			var cache = Graph.Caches[entity.GetType()];
			var exception = new PXSetPropertyException(format, args);
			cache.RaiseExceptionHandling<TField>(entity, fieldValue, exception);
		}

		private static void ThrowGeneralError(string cacheDisplayName)
		{
			throw new PXException(ErrorMessages.RecordRaisedErrors, null, cacheDisplayName);
		}
    }
}
