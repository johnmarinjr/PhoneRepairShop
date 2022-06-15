using System;
using PX.Data;
using PX.Objects.Common;

namespace PX.Objects.AM.Attributes
{
    [PXDBString(30, IsUnicode = true)]
    [PXUIField(DisplayName = "Ref Nbr")]
    public class RefNbrFieldAttribute : PX.Objects.GL.AcctSubAttribute
    {
        protected Type RefNoteIDField;

        public RefNbrFieldAttribute(Type refNoteIDField) : base()
        {
            RefNoteIDField = refNoteIDField;
        }

        public static string FormatFieldNbr(params string[] vals)
        {
            return vals == null ? null : string.Join(", ", vals);
        }

        public static string GetKeyString(PXGraph graph, Guid? refNoteId)
        {
            if (refNoteId == null)
            {
                return null;
            }

            var helper = new EntityHelper(graph);
            var entity = helper.GetEntityRow(refNoteId);
            if (entity == null)
            {
                return null;
            }

            var keys = helper.GetEntityRowKeys(entity.GetType(), entity);
            if (keys == null || keys.Length == 0)
            {
                return null;
            }

            return string.Join(", ", keys);
        }

        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);

            PXButtonDelegate del = delegate (PXAdapter adapter)
            {
                PXCache cache = adapter.View.Graph.Caches[sender.GetItemType()];
                if (cache.Current != null)
                {
                    object val = cache.GetValueExt(cache.Current, RefNoteIDField.Name);

                    EntityHelper helper = new EntityHelper(sender.Graph);
                    var state = val as PXRefNoteBaseAttribute.PXLinkState;
                    if (state != null)
                    {
                        helper.NavigateToRow(state.target.FullName, state.keys, PXRedirectHelper.WindowMode.NewWindow);
                    }
                    else
                    {
                        helper.NavigateToRow((Guid?)cache.GetValue(cache.Current, RefNoteIDField.Name), PXRedirectHelper.WindowMode.NewWindow);
                    }
                }

                return adapter.Get();
            };

            string ActionName = sender.GetItemType().Name + "$" + _FieldName + "$Link";
            sender.Graph.Actions[ActionName] = (PXAction)Activator.CreateInstance(typeof(PXNamedAction<>).MakeGenericType(sender.GetItemType()), new object[] { sender.Graph, ActionName, del, new PXEventSubscriberAttribute[] { new PXUIFieldAttribute { MapEnableRights = PXCacheRights.Select } } });
        }
    }
}