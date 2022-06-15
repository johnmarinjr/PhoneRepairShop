using PX.Data;
using System;

namespace PX.Objects.Localizations.CA
{
    public class SynchronizeFieldAttribute : PXEventSubscriberAttribute, IPXFieldUpdatedSubscriber
    {
        protected Type _syncToField;

        public SynchronizeFieldAttribute(Type withField)
        {
            if (withField == null)
            {
                throw new ArgumentNullException("withField");
            }

            _syncToField = withField;
        }

        public void FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            object newValue = sender.GetValue(e.Row, _FieldName);

            sender.SetValue(e.Row, _syncToField.Name, newValue);
        }
    }
}
