using PX.Objects.AP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation
{
    public class ShowErrorOnlyParams : IErrorHandlingStrategyParams
    {
        private readonly string errorMessage;

        public ShowErrorOnlyParams(string errorMessage, object entity, params object[] args)
        {
            this.errorMessage = errorMessage;
            Entity = entity;
            Args = args;
        }

        public bool ShowError => true;
        public bool ThrowError => false;
        public bool WithFieldValue => false;
        public string ErrorMessage => errorMessage;
        public object Entity { get; }
        public object[] Args { get; }

    }

    public class ShowErrorOnlyWithFieldValueParams : IErrorHandlingStrategyParams
    {
        private readonly string errorMessage;

        public ShowErrorOnlyWithFieldValueParams(string errorMessage, object entity, object fieldValue, params object[] args)
        {
            this.errorMessage = errorMessage;
            Entity = entity;
            FieldValue = fieldValue;
            Args = args;
        }

        public bool ShowError => true;
        public bool ThrowError => false;
        public bool WithFieldValue => true;
        public string ErrorMessage => errorMessage;
        public object Entity { get; }
        public object FieldValue { get; }
        public object[] Args { get; }
    }

    public class ThrowErrorOnlyParams : IErrorHandlingStrategyParams
    {
        private readonly string errorMessage;

        public ThrowErrorOnlyParams(string errorMessage, string errorFieldName)
        {
            this.errorMessage = errorMessage;
            ErrorFieldName = errorFieldName;
        }

        public bool ShowError => false;
        public bool ThrowError => true;
        public bool WithFieldValue => false;
        public string ErrorMessage => errorMessage;
        public string ErrorFieldName { get; }
    }

    public class ShowAndThrowErrorParams : IErrorHandlingStrategyParams
    {
        private readonly string errorMessage;

        public ShowAndThrowErrorParams(string errorMessage, object entity, string errorFieldName, params object[] args)
        {
            this.errorMessage = errorMessage;
            Entity = entity;
            ErrorFieldName = errorFieldName;
            Args = args;
        }

        public bool ShowError => true;
        public bool ThrowError => true;
        public bool WithFieldValue => false;
        public string ErrorMessage => errorMessage;
        public object Entity { get; }
        public string ErrorFieldName { get; }
        public object[] Args { get; }
    }

    public class ShowAndThrowErrorWithFieldValueParams : IErrorHandlingStrategyParams
    {
        private readonly string errorMessage;

        public ShowAndThrowErrorWithFieldValueParams(string errorMessage, object entity, object fieldValue, string errorFieldName, params object[] args)
        {
            this.errorMessage = errorMessage;
            Entity = entity;
            FieldValue = fieldValue;
            ErrorFieldName = errorFieldName;
            Args = args;
        }

        public bool ShowError => true;
        public bool ThrowError => true;
        public bool WithFieldValue => true;
        public string ErrorMessage => errorMessage;
        public object Entity { get; }
        public object FieldValue { get; }
        public string ErrorFieldName { get; }
        public object[] Args { get; }
    }
}
