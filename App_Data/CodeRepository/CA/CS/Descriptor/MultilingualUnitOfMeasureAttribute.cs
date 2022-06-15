using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.Localizations.CA.CS
{
    public class MultilingualUnitOfMeasureAttribute : PXEventSubscriberAttribute, IPXRowPersistedSubscriber
    {
        public void RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
        {
            INUnit unit;
            PXEntryStatus status;
            UnitOfMeasureMaint graph;

            if (e.TranStatus == PXTranStatus.Completed)
            {
                unit = e.Row as INUnit;

                if ((unit != null) && ((unit.UnitType == INUnitType.Global) || (unit.UnitType == INUnitType.InventoryItem)))
                {
                    status = sender.GetStatus(unit);

                    if (status == PXEntryStatus.Inserted || status == PXEntryStatus.Updated)
                    {
#pragma warning disable PX1045 // A PXGraph instance cannot be created within an event handler
                        graph = PXGraph.CreateInstance<UnitOfMeasureMaint>();
#pragma warning restore PX1045

#pragma warning disable PX1043 // Changes cannot be saved to the database from the event handler
                        graph.AddNew(unit.FromUnit);
#pragma warning restore PX1043

                        if (unit.ToUnit != unit.FromUnit)
                        {
#pragma warning disable PX1043 // Changes cannot be saved to the database from the event handler
                            graph.AddNew(unit.ToUnit);
#pragma warning restore PX1043
                        }
                    }
                }
            }
        }
    }
}
