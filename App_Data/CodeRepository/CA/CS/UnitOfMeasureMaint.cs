using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.Localizations.CA.CS
{
    // Override the primary graph to be able to set the current record in the form.    
    [PXPrimaryGraph(new Type[] { typeof(INUnit) },
                    new Type[] { typeof(Select<UnitOfMeasure,
                                               Where<UnitOfMeasure.unit, Equal<Current<INUnit.fromUnit>>>>)})]
    public class UnitOfMeasureMaint : PXGraph<UnitOfMeasureMaint, UnitOfMeasure>
    {
        public PXSelect<UnitOfMeasure> UnitOfMeasures;

        public PXSelect<INUnit,
            Where2<Where<INUnit.unitType, Equal<INUnitType.global>,
                     And<INUnit.fromUnit, Equal<Current<UnitOfMeasure.unit>>>>,
               And<Where<Not<INUnit.toUnit, Equal<Current<UnitOfMeasure.unit>>,
                           And<INUnit.unitRate, Equal<decimal1>>>>>>,
            OrderBy<Asc<INUnit.toUnit>>> Units;


        #region Event Handlers

        protected virtual void UnitOfMeasure_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            UnitOfMeasure uom = e.Row as UnitOfMeasure;
            if (uom == null)
            {
                return;
            }

            bool enabled = uom.Unit != null;

            PXUIFieldAttribute.SetEnabled(Units.Cache, null, enabled);
            Units.Cache.AllowInsert = enabled;
            Units.Cache.AllowDelete = enabled;
        }

        protected virtual void UnitOfMeasure_RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
        {
            if (e.TranStatus != PXTranStatus.Completed)
            {
                return;
            }

            UnitOfMeasure uom = e.Row as UnitOfMeasure;
            if (uom == null)
            {
                return;
            }


            PXEntryStatus status = sender.GetStatus(uom);
            if (status == PXEntryStatus.Inserted || status == PXEntryStatus.Updated)
            {
                INUnit unit = PXSelect<INUnit,
                    Where<INUnit.unitType, Equal<INUnitType.global>,
                        And<INUnit.fromUnit, Equal<Required<INUnit.fromUnit>>>>>.Select(this, uom.Unit);
                if (unit == null)
                {
                    unit = (INUnit)Units.Cache.CreateInstance();
                    unit.UnitType = INUnitType.Global;
                    unit.FromUnit = uom.Unit;
                    unit.ToUnit = uom.Unit;
                    unit.UnitRate = 1m;
                    unit.UnitMultDiv = MultDiv.Multiply;

                    // Create an instance of INUnitMaint graph to insert the one by one unit to the database. 
                    //
                    // Using the local Units data view to insert the one by one unit generate an error 
                    // 'To unit xxx cannot be found in the system' because of the FieldVerifying event 
                    // that is raised during the insertion.
                    //

#pragma warning disable PX1045 // A PXGraph instance cannot be created within an event handler
                    INUnitMaint graph = PXGraph.CreateInstance<INUnitMaint>();
#pragma warning restore PX1045

                    graph.Unit.Insert(unit);

#pragma warning disable PX1043 // Changes cannot be saved to the database from the event handler
                    graph.Actions.PressSave();
#pragma warning restore PX1043
                }
            }
        }

        protected virtual void UnitOfMeasure_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
        {
            UnitOfMeasure uom = e.Row as UnitOfMeasure;
            if (uom == null)
            {
                return;
            }

            PXResultset<INUnit> query = PXSelect<INUnit,
                Where2<Where<INUnit.unitType, Equal<INUnitType.global>>,
                   And<Where<INUnit.fromUnit, Equal<Required<INUnit.fromUnit>>,
                          Or<INUnit.toUnit, Equal<Required<INUnit.toUnit>>>>>>>.Select(this, uom.Unit, uom.Unit);
            foreach (PXResult<INUnit> item in query)
            {
                INUnit unit = (INUnit)item;
                Units.Delete(unit);
            }
        }

        // Manually truncate the description to 6 characters until Acumatica fix the problem.
        protected virtual void UnitOfMeasure_DescrTranslations_FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
        {
            string[] translations = e.NewValue as string[];
            if (translations == null)
            {
                return;
            }

            for (int i = 0; i < translations.Length; i++)
            {
                if (!string.IsNullOrEmpty(translations[i]) && translations[i].Length > 6)
                {
                    translations[i] = translations[i].Substring(0, 6);
                }
            }
        }


        protected virtual void INUnit_ToUnit_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            UnitOfMeasure uom = UnitOfMeasures.Current;
            if (uom == null)
            {
                return;
            }

            INUnit unit = e.Row as INUnit;
            if (unit == null)
            {
                return;
            }

            if (unit.FromUnit == null)
            {
                // set the property using the e.Row data record because the FromUnit field 
                // is defined after the ToUnit field in the DAC (T200 - p124).
                unit.FromUnit = uom.Unit;
            }
        }

        protected virtual void INUnit_UnitRate_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
        {
            Decimal? factor = (Decimal?)e.NewValue;
            if (factor == 0m && (string)sender.GetValue<INUnit.unitMultDiv>(e.Row) == MultDiv.Divide)
            {
                throw new PXSetPropertyException(PX.Objects.CS.Messages.Entry_NE, "0");
            }
        }

        #endregion

        #region Cache Attached
        // Override the PXSelector of the ToUnit field to restrict the selection to existing units of UnitOfMeasure.
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [INUnit(IsKey = true, DisplayName = "To Unit", Visibility = PXUIVisibility.Visible),
            PXSelector(typeof(Search<UnitOfMeasure.unit, Where<UnitOfMeasure.unit, NotEqual<Current<UnitOfMeasure.unit>>>>),
                       typeof(UnitOfMeasure.unit))]
        protected virtual void INUnit_ToUnit_CacheAttached(PXCache sender)
        {
        }
        #endregion

        #region Public methods
        public void AddNew(string unit)
        {
            if (!string.IsNullOrEmpty(unit))
            {
                UnitOfMeasure uom = PXSelect<UnitOfMeasure,
                    Where<UnitOfMeasure.unit,
                        Equal<Required<UnitOfMeasure.unit>>>>.Select(this, unit);
                if (uom == null)
                {
                    uom = (UnitOfMeasure)UnitOfMeasures.Cache.CreateInstance();
                    uom.Unit = unit;
                    // set the value because the description field cannot be empty.
                    uom.Descr = unit;

                    // then get back the new data record inserted into the cache object (IMPORTANT!!!)
                    uom = UnitOfMeasures.Insert(uom) as UnitOfMeasure;

                    // then set the description again in order to set the value of
                    // the multi-language field in the current locale correctly.
                    UnitOfMeasures.Cache.SetValueExt<UnitOfMeasure.descr>(uom, unit);

                    Actions.PressSave();
                }
            }
        }
        #endregion
    }
}