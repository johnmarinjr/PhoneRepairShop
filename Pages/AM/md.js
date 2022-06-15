var SP = bryntum.schedulerpro;
var DH = SP.DateHelper;
var Scheduler = SP.SchedulerPro;
var ResourceHistogram = SP.ResourceHistogram;

var mScheduler,
    workCenterScheduler,
    machineScheduler,
    tempCont1,
    tempCont2,
    tempCont3,
    popupColumnConfiguration;
var shouldHighlight = true;

const Localizations = {
    CURRENT: {
        PRODUCTION_ORDERS: window.parent.ClientLocalizedStrings.AM.PRODUCTION_ORDERS || 'PRODUCTION ORDERS',
        WORK_CENTER: window.parent.ClientLocalizedStrings.AM.WORK_CENTER || 'WORK CENTERS',
        MACHINE: window.parent.ClientLocalizedStrings.AM.MACHINE || 'MACHINES',
        PERIOD: window.parent.ClientLocalizedStrings.AM.PERIOD || 'Period',
        OPERATION_DESCRIPTION: window.parent.ClientLocalizedStrings.AM.OPERATION_DESCRIPTION || 'Operation Description',
        PRODUCTION_ORDER_INFORMATION: window.parent.ClientLocalizedStrings.AM.PRODUCTION_ORDER_INFORMATION || 'Production Order Information',
        FULLSCREEN: window.parent.ClientLocalizedStrings.AM.FULLSCREEN || 'Fullscreen',
        MAXIMIZE: window.parent.ClientLocalizedStrings.AM.MAXIMIZE || 'Maximize',
        LATE_ORDERS: window.parent.ClientLocalizedStrings.AM.LATE_ORDERS || 'Late Orders',
        Column_Configuration: window.parent.ClientLocalizedStrings.AM.Column_Configuration || 'Column Configuration',
        Column_Configuration_filter: window.parent.ClientLocalizedStrings.AM.Column_Configuration_filter || 'filter',
        Available_Columns: window.parent.ClientLocalizedStrings.AM.Available_Columns || 'Available Columns',
        Selected_Columns: window.parent.ClientLocalizedStrings.AM.Selected_Columns || 'Selected Columns',
        Reset_To_Default: window.parent.ClientLocalizedStrings.AM.Reset_To_Default || 'Reset To Default',
        Confirm: window.parent.ClientLocalizedStrings.AM.Confirm || 'Confirm',
        Confirm_Reset_Text: window.parent.ClientLocalizedStrings.AM.Confirm_Reset_Text || 'Do you want to reset Columns Settings?',
        OK: window.parent.ClientLocalizedStrings.AM.OK || 'OK',
        Cancel: window.parent.ClientLocalizedStrings.AM.Cancel || 'CANCEL',
        NO_RECORDS_TO_DISPLAY: window.parent.ClientLocalizedStrings.AM.NO_RECORDS_TO_DISPLAY || 'No records to display',
        PRESET_NAME_hourAndDay: window.parent.ClientLocalizedStrings.AM.PRESET_NAME_hourAndDay || 'Hours',
        PRESET_NAME_weekAndDay: window.parent.ClientLocalizedStrings.AM.PRESET_NAME_weekAndDay || 'Days',
        PRESET_NAME_weekAndMonth: window.parent.ClientLocalizedStrings.AM.PRESET_NAME_weekAndMonth || 'Weeks',
        PRESET_NAME_monthAndYear: window.parent.ClientLocalizedStrings.AM.PRESET_NAME_monthAndYear || 'Months',
    },
};

const requiredPresetIds = {
        //my_hourAndDayPreset: 1,
        //my_weekAndDayPreset: 1,
        //my_monthAndYearPreset: 1,
        //secondAndMinute  : 1,
        //minuteAndHour: 1,
        hourAndDay: 1,
        weekAndDay: 1,
        weekAndMonth: 1,
        //weekAndDayLetter: 1,
        //weekDateAndMonth: 1,
        monthAndYear: 1,
        //year: 1,
        //manyYears: 1
    },
    presets = SP.PresetManager.records.filter((p) => requiredPresetIds[p.id]),
    presetStore = new SP.PresetStore({
        data: presets,
        zoomOrder: -1,
    }),
    presetsArr = [{
            base: "hourAndDay",
            id: "hourAndDay",
        },
        {
            base: "weekAndDay",
            id: "weekAndDay",
        },
        {
            base: "weekAndMonth",
            id: "weekAndMonth",
        },
        {
            base: "monthAndYear",
            id: "monthAndYear",
        },
    ];

if (top && top._dateFormatInfo) {
    SP.LocaleManager.locale.PresetManager.hourAndDay.topDateFormat = 'ddd ' + top._dateFormatInfo.shortDate.trim().toUpperCase().replace(/.y+/i, '');
}
SP.LocaleManager.locale.GridBase.noRows = Localizations.CURRENT.NO_RECORDS_TO_DISPLAY;
presetStore.forEach(p => {
    switch (p.id) {
        case "hourAndDay":
            p.data.name = Localizations.CURRENT.PRESET_NAME_hourAndDay;
            break;
        case "weekAndDay":
            p.data.name = Localizations.CURRENT.PRESET_NAME_weekAndDay;
            break;
        case "weekAndMonth":
            p.data.name = Localizations.CURRENT.PRESET_NAME_weekAndMonth;
            break;
        case "monthAndYear":
            p.data.name = Localizations.CURRENT.PRESET_NAME_monthAndYear;
            break;
    }
});

presetStore.forEach(p => {
    switch (p.id) {
        case "hourAndDay":
            p.headers[0].dateFormat = SP.LocaleManager.locale.PresetManager.hourAndDay.topDateFormat;
            break;
    }
});

var HELPERS = {};
HELPERS.UI = {
        KEYS: {
            mScheduler: "productionOrders",
            workCenterScheduler: "workCenters",
            machineScheduler: "machines",
        },
        columnChoiceDialog: {
            editor: { readOnly: true },
            field: "columnChoiceDialog",
            id: "columnChoiceDialog",
            hidden: false,
            icon: "b-fa b-fa-wrench",
            type: "column",
            flex: 0,
            width: 27,
            minWidth: 27,
            hideable: false,
            resizable: false,
            searchable: false,
            showColumnPicker: false,
            enableHeaderContextMenu: false,
            isNewService: true,
            /*headerRenderer: ({ record }) => {
            	return `<button class="columnChoiceDialog b-transparent b-button b-fa b-fa-wrench"></button>`;
            }*/
        },
        persistColumns: function(sch, def) {
            var changedColumns = sch && sch.columns ? sch.columns.allRecords : null;
            if (changedColumns) {
                changedColumns.pop();
                changedColumns = changedColumns.map(function(n) {
                    var nn = n.originalData;
                    nn.width = n.width;
                    nn.hidden = n.hidden;
                    return nn;
                });
            } else changedColumns = def;
            return changedColumns;
        },

        sendColumnsToServer: function(prm) {
            if (!prm.changes) return;
            if (prm.type != "update") return;
            if (prm.record.type == "timeAxis") return;
            if (prm.record.field == "columnChoiceDialog") return;
            if (prm.changes.width && prm.changes.width.value > 1000) return;
            if (prm.changes.hidden || prm.changes.parentIndex || prm.changes.width) {
                if (!HELPERS.UI.t2) HELPERS.UI.t2 = [];
                HELPERS.UI.t2.push(Date.now());
                setTimeout(function() {
                    HELPERS.UI.t2.pop();
                    if (HELPERS.UI.t2.length) {
                        return;
                    }
                    var records = prm.source.records;
                    var columns = [];
                    records.forEach(function(r) {
                        if (r.field != "columnChoiceDialog")
                            columns.push({ field: r.field, hidden: r.hidden, width: r.width });
                    });
                    window.parent.postMessage({
                        cmd: "updateColumns",
                        newData: {
                            source: HELPERS.UI.KEYS[prm.source.grid.ref],
                            columns: columns,
                        },
                    });
                }, 2200);
            }
        },

        debounce: function(func, wait, immediate) {
            var timeout;
            return function executedFunction() {
                const context = this;
                const args = arguments;
                const later = function() {
                    timeout = null;
                    if (!immediate) func.apply(context, args);
                };
                const callNow = immediate && !timeout;
                clearTimeout(timeout);
                timeout = setTimeout(later, wait);
                if (callNow) func.apply(context, args);
            };
        },

        getResourceDataByKey: function(sch, resKey) {
            var rsOut;
            sch.resourceStore.forEach(function(rs) {
                if (rs.id == resKey) {
                    rsOut = rs;
                }
            });
            return rsOut;
        },

        getWCEvent: function(ordRef) {
            var eventFromStore;
            if (!workCenterScheduler) return null;
            if (!workCenterScheduler.eventStore) return null;
            workCenterScheduler.eventStore.forEach(function(evt) {
                if (evt.ordRef == ordRef) {
                    eventFromStore = evt;
                }
            });
            return eventFromStore;
        },

        getEvent: function(sch, key, resourceId) {
            var eventFromStore;
            sch.eventStore.forEach(function(evt) {
                if (evt.resourceId == resourceId && evt.name == key) {
                    eventFromStore = evt;
                }
            });
            return eventFromStore;
        },

        selectEvent: function(sch, key, resourceId, doScroll, doActive) {
            var eventFromStore = HELPERS.UI.getEvent(sch, key, resourceId);
            sch.select(eventFromStore, false);
            if (doScroll && sch.isVisible) {
                sch.scrollEventIntoView(eventFromStore, { focus: doActive });
            }
            /*
		if (doActive && sch.isVisible) {
			var els = sch.getElementsFromEventRecord(eventFromStore);
			if (els.length) els[0].parentNode.classList.add('b-active');
		}*/
        },

        filter: function(sch, lst) {
            if (lst.length) {
                //var key = lst[0].get('name'), ordNum = lst[0].get('resourceId');
                sch.eventStore.filter({
                    filters: function(event) {
                        return lst.filter(function(l1) {
                            return (
                                event.name == l1.get("name") &&
                                event.ordRef == l1.get("resourceId")
                            );
                        }).length;
                        //return event.name.match(new RegExp(key, 'i')) && event.ordNum.match(new RegExp(ordNum, 'i'));
                    },
                    replace: true,
                });
            } else
                sch.eventStore.filter({
                    filters: function(event) {
                        return event;
                    },
                    replace: true,
                });
        },

        highlighting: function(sch, lst, doScroll) {
            var alreadyScroll = 0;
            sch.eventStore.forEach((task) => {
                const taskClassList = new SP.DomClassList(task.cls);

                if (
                    lst.length &&
                    lst.filter(function(st) {
                        return (
                            st.key != "" &&
                            task.ordRef &&
                            task.name == st.get("name") &&
                            task.ordRef == st.get("resourceId")
                        );
                    }).length
                ) {
                    taskClassList.add("b-match");
                    if (doScroll && !alreadyScroll && sch.isVisible) {
                        sch.scrollEventIntoView(task);
                        alreadyScroll = 1;
                    }
                } else {
                    taskClassList.remove("b-match");
                }

                task.cls = taskClassList.value;
            });

            sch.element
                .querySelectorAll(".b-sch-event-wrap.b-sch-style-plain")
                .forEach(function(el) {
                    el.classList.remove("b-custom-select");
                });
            sch.element.querySelectorAll(".b-match").forEach(function(el) {
                el.parentNode.classList.add("b-custom-select");
            });

            sch.element.classList[lst.length > 0 ? "add" : "remove"]("b-highlighting");
        },

        getResourceColorDiff: function(eventRecord, resourceRecord) {
            var color = HELPERS.UI.colors.milestones.green;
            switch (HELPERS.DATA.myParams.colorCoding.resources) {
                case HELPERS.UI.colors.colorCodingResources.Status:
                case HELPERS.UI.colors.colorCodingResources.ScheduleStatus:
                    Object.keys(HELPERS.UI.colors.statuses).forEach(function(key) {
                        var x = HELPERS.UI.colors.statuses[key];
                        mScheduler.resourceStore.data.forEach(function(data) {
                            if (data.id == eventRecord.ordRef)
                                if (key.toLowerCase() == data.ordStatus.toLowerCase()) {
                                    color = x.color;
                                }
                        });
                    });
                    break;
                case HELPERS.UI.colors.colorCodingResources.ByOrderType:
                    Object.keys(HELPERS.UI.colors.orderTypes).forEach(function(key) {
                        var x = HELPERS.UI.colors.orderTypes[key];
                        mScheduler.resourceStore.data.forEach(function(data) {
                            if (data.id == eventRecord.ordRef)
                                if (key.toLowerCase() == data.ordType.toLowerCase()) {
                                    color = x.color;
                                }
                        });
                    });
                    break;
            }
            return color;
        },

        getResourceColorOne: function(eventRecord, resourceRecord) {
            var color = HELPERS.UI.colors.milestones.green;
            var mrr = this.getResourceDataByKey(mScheduler, eventRecord.ordRef);
            if (!mrr) return color;
            switch (HELPERS.DATA.myParams.colorCoding.orders) {
                case HELPERS.UI.colors.colorCodingOrders.WorkCenter:
                    var ind = HELPERS.UI.getWorkCenterIndex(
                        mrr.get("id"),
                        eventRecord.get("name")
                    );
                    if (ind != -1)
                        color = HELPERS.UI.colors.dispatchPriority[ind > 10 ? 10 : ind];
                    break;
                case HELPERS.UI.colors.colorCodingOrders.Status:
                    if (mrr) {
                        var tColor = HELPERS.UI.colors.statuses[mrr.ordStatus];
                        if (tColor) color = tColor.color;
                    }
                    break;
                case HELPERS.UI.colors.colorCodingOrders.ByOrderType:
                    if (mrr) {
                        var tColor = HELPERS.UI.colors.orderTypes[mrr.ordType];
                        if (tColor) color = tColor.color;
                    }
                    break;
                case HELPERS.UI.colors.colorCodingOrders.DispatchPriority:
                    var pr = mrr.priority || 0;
                    if (pr > 11) pr = 12;
                    if (pr < 0) pr = 12;
                    pr--;
                    color = HELPERS.UI.colors.dispatchPriority[pr];
                    break;
                case HELPERS.UI.colors.colorCodingOrders.FirmSchedule:
                    color = HELPERS.UI.colors.dispatchPriority[mrr.firmSchedule ? 1 : 0];
                    break;
            }
            return color;
        },

        getResourceColor: function(eventRecord, resourceRecord) {
            return this.getResourceColorOne(eventRecord, resourceRecord);
        },
        getColumn: function(sch, field) {
            var t = sch.columns.data.filter(function(nn) {
                return nn.field == field;
            });
            if (t.length) return t[0];
            return null;
        },
        getColumnText: function(sch, field) {
            var t = this.getColumn(sch, field);
            if (t) return t.text;
            return "";
        },
        getTooltipPart: function(sch, resource, field, defaultLabel) {
            var t = `<span class="sch-tooltip-subtitle">${
			HELPERS.UI.getColumnText(sch, field) || defaultLabel
		}:</span> <span>${resource.get(field) || "-"}</span> `;
            return t;
        },
        beforePresetChange: function(cfg) {
            return false;
        },
        getWorkCenterIndex: function(ordNumber, name) {
            var _ind = -1;
            for (var i = 0; i < HELPERS.DATA.orderWcList.length; i++) {
                var el = HELPERS.DATA.orderWcList[i];
                var isElFound = el.orderNumbers.filter(function(el1) {
                    return el1.ordRef == ordNumber && el1.name == name;
                }).length;
                if (isElFound) {
                    _ind = i;
                    return _ind;
                }
            }
            return _ind;
        },

        saveState: function(viewPreset) {
            if (mScheduler && !isNaN(mScheduler.viewportCenterDate.getTime())) {
                HELPERS.UI.ScrollDate = mScheduler.viewportCenterDate; // mScheduler.timeView.startDate;
                HELPERS.UI.ScrollLeft = mScheduler.scrollLeft;
                HELPERS.UI.ScrollTop = mScheduler.scrollTop;
                HELPERS.UI.gridWidth = mScheduler.subGrids.locked.width;
                HELPERS.UI.viewPreset = viewPreset ?
                    viewPreset.id :
                    mScheduler.viewPreset.id;
                HELPERS.UI.startDate = mScheduler.startDate;
                HELPERS.UI.endDate = mScheduler.endDate;
                HELPERS.UI.bottomActiveTab = tempCont3.items[0].activeTab;
                HELPERS.UI.tempCont3isVisible = tempCont3.isVisible;
                HELPERS.UI.tempCont2isVisible = tempCont2.isVisible;
            }
        },
        restoreState: function() {
            if (mScheduler && HELPERS.UI.ScrollDate) {
                mScheduler.startDate = HELPERS.UI.startDate;
                mScheduler.endDate = HELPERS.UI.endDate;
                mScheduler.subGrids.locked.width = HELPERS.UI.gridWidth;
                mScheduler.scrollLeft = HELPERS.UI.ScrollLeft;
                mScheduler.scrollTop = HELPERS.UI.ScrollTop;
            }
        },
        clearState: function() {
            HELPERS.UI.ScrollDate = undefined;
            HELPERS.UI.ScrollLeft = undefined;
            HELPERS.UI.ScrollTop = undefined;
            HELPERS.UI.gridWidth = undefined;
            HELPERS.UI.viewPreset = undefined;
            HELPERS.UI.startDate = undefined;
            HELPERS.UI.endDate = undefined;
            HELPERS.UI.tempCont3isVisible = undefined;
            HELPERS.UI.tempCont2isVisible = undefined;
        },

        paintBody: function(viewPreset) {
            if (HELPERS.UI.isPainting) return;
            HELPERS.UI.isPainting = true;
            HELPERS.UI.saveState(viewPreset);
            if (tempCont1) tempCont1.destroy();
            tempCont1 = new SP.Container({
                appendTo: "mainContainer",
                items: [{
                    type: "tabpanel",
                    id: "mainTab",
                    items: [{
                        title: Localizations.CURRENT.PRODUCTION_ORDERS,
                        items: [{
                            type: "widget",
                            id: "OrdersContainer",
                            style: "display: flex; flex:1; padding:0; margin:0;",
                        }, ],
                    }, ],
                }, ],
                flex: 1,
                style: "padding: 0; background: var(--background-color, #F5F7F8)",
            });

            if (tempCont2) tempCont2.destroy();
            tempCont2 = new SP.Splitter({
                appendTo: "mainContainer",
                scrollable: true,
            });

            if (tempCont3) tempCont3.destroy();
            tempCont3 = new SP.Container({
                appendTo: "mainContainer",
                items: [{
                    type: "tabpanel",
                    animateTabChange: false,
                    activeTab: HELPERS.UI.bottomActiveTab || 0,
                    listeners: {
                        tabChange: function({
                            prevActiveItem,
                            prevActiveIndex,
                            activeItem,
                            activeIndex,
                        }) {
                            if (HELPERS.UI.isPainting) return;
                            var selEvent = mScheduler.selectedEvents.length ?
                                mScheduler.selectedEvents[0] :
                                null;
                            HELPERS.UI.clearState();
                            HELPERS.UI.saveState();

                            setTimeout(function() {
                                if (
                                    workCenterScheduler &&
                                    workCenterScheduler.destroy &&
                                    !workCenterScheduler.isDestroying
                                ) {
                                    try {
                                        workCenterScheduler.destroy();
                                        workCenterScheduler = null;
                                    } catch (ex) {}
                                }
                                if (
                                    machineScheduler &&
                                    machineScheduler.destroy &&
                                    !machineScheduler.isDestroying
                                ) {
                                    try {
                                        machineScheduler.destroy();
                                        machineScheduler = null;
                                    } catch (ex) {}
                                }
                                setTimeout(function() {
                                    HELPERS.UI.paintBody();
                                    setTimeout(function() {
                                        if (HELPERS.UI.timeAxis.haveActual) {
                                            mScheduler.setStartDate(
                                                HELPERS.UI.timeAxis.actualStartDate
                                            );
                                            mScheduler.setEndDate(HELPERS.UI.timeAxis.actualEndDate);
                                            if (selEvent)
                                                mScheduler.scrollEventIntoView(selEvent, {
                                                    focus: true,
                                                });
                                        }
                                    }, 50);
                                }, 50);
                            }, 50);
                        },
                    },
                    items: [{
                            title: Localizations.CURRENT.WORK_CENTER,
                            items: [{
                                type: "widget",
                                id: "WorkCenterContainer",
                                style: "display: flex; flex:1; padding:0; margin:0;",
                            }, ],
                        },
                        {
                            title: Localizations.CURRENT.MACHINE,
                            items: [{
                                type: "widget",
                                id: "MachineContainer",
                                style: "display: flex; flex:1; padding:0; margin:0;",
                            }, ],
                        },
                    ],
                }, ],
                flex: 1,
                style: "padding: 0; background: var(--background-color, #F5F7F8)",
            });

            var mainToolbarDiv = document.createElement("DIV");
            mainToolbarDiv.setAttribute("id", "mainToolbarDiv");
            var mainTabPanel = document.querySelector("#mainTab .b-tabpanel-tabs");
            if (mainTabPanel)
                mainTabPanel.insertAdjacentElement("beforeend", mainToolbarDiv);

            var mainToolbar = new SP.Toolbar({
                appendTo: mainToolbarDiv,
                cls: "ffa",
                items: [{
                        type: "button",
                        ref: "fullMscreen",
                        cls: "b-tool1",
                        icon: "b-icon-fullscreen",
                        tooltip: Localizations.CURRENT.FULLSCREEN,
                        onAction: function() {
                            window.parent.postMessage({ cmd: "fullscreen", newData: null });
                        },
                    },
                    {
                        type: "button",
                        ref: "btnMaximize",
                        toggleable: true,
                        cls: "b-tool1",
                        icon: "b-fa-arrows-alt-v",
                        tooltip: Localizations.CURRENT.MAXIMIZE,
                        onAction: function() {
                            tempCont3.isVisible ? tempCont3.hide() : tempCont3.show();
                            tempCont2.isVisible ? tempCont2.hide() : tempCont2.show();
                        },
                    },
                    {
                        type: "button",
                        ref: "btnLateOrders",
                        toggleable: true,
                        cls: "b-tool1",
                        //icon: 'b-fa-sign-out-alt',
                        tooltip: Localizations.CURRENT.LATE_ORDERS,
                        text: Localizations.CURRENT.LATE_ORDERS,
                        onAction: function() {
                            mScheduler.resourceStore.clearFilters();
                            if (!this.pressed) return;
                            mScheduler.resourceStore.filter({
                                id: "filterLateOrders",
                                filterBy: function(a) {
                                    if (!a.get("requestedOn")) return false;
                                    if (!a.get("endDate")) return false;
                                    return (
                                        a.get("requestedOn").getTime() < a.get("endDate").getTime()
                                    );
                                },
                            });
                        },
                    },
                    {
                        type: "combo",
                        width: 120,
                        ref: "presetCombo",
                        placeholder: "Preset",
                        editable: false,
                        store: presetStore,
                        valueField: "id",
                        displayField: "name",
                        value: HELPERS.UI.viewPreset || "weekAndDay",
                        picker: {
                            maxHeight: 500,
                        },
                        onChange({ source: combo }) {
                            if (HELPERS.UI.isPainting) return;
                            var selEvent = mScheduler.selectedEvents.length ?
                                mScheduler.selectedEvents[0] :
                                null;
                            HELPERS.UI.clearState();
                            HELPERS.UI.saveState(combo.selected);

                            setTimeout(function() {
                                if (
                                    workCenterScheduler &&
                                    workCenterScheduler.destroy &&
                                    !workCenterScheduler.isDestroying
                                ) {
                                    try {
                                        workCenterScheduler.destroy();
                                        workCenterScheduler = null;
                                    } catch (ex) {}
                                }
                                if (
                                    machineScheduler &&
                                    machineScheduler.destroy &&
                                    !machineScheduler.isDestroying
                                ) {
                                    try {
                                        machineScheduler.destroy();
                                        machineScheduler = null;
                                    } catch (ex) {}
                                }
                                setTimeout(function() {
                                    HELPERS.UI.paintBody(combo.selected);
                                    setTimeout(function() {
                                        if (HELPERS.UI.timeAxis.haveActual) {
                                            mScheduler.setStartDate(
                                                HELPERS.UI.timeAxis.actualStartDate
                                            );
                                            mScheduler.setEndDate(HELPERS.UI.timeAxis.actualEndDate);
                                            if (selEvent)
                                                mScheduler.scrollEventIntoView(selEvent, { focus: true });
                                        }
                                    }, 50);
                                }, 50);
                            }, 50);
                        },
                    },
                    {
                        type: "button",
                        ref: "zoomInButton",
                        cls: "b-tool1",
                        icon: "b-icon-search-plus",
                        style: "display:none;",
                        tooltip: "Zoom in",
                        onAction: function() {
                            HELPERS.UI.clearState();
                            mScheduler.zoomIn();
                            if (HELPERS.UI.timeAxis.haveActual) {
                                mScheduler.setStartDate(HELPERS.UI.timeAxis.actualStartDate);
                                mScheduler.setEndDate(HELPERS.UI.timeAxis.actualEndDate);
                            }
                        },
                    },
                    {
                        type: "button",
                        ref: "zoomOutButton",
                        cls: "b-tool1",
                        icon: "b-icon b-icon-search-minus",
                        style: "display:none;",
                        tooltip: "Zoom out",
                        onAction: function() {
                            HELPERS.UI.clearState();
                            mScheduler.zoomOut();
                            if (HELPERS.UI.timeAxis.haveActual) {
                                mScheduler.setStartDate(HELPERS.UI.timeAxis.actualStartDate);
                                mScheduler.setEndDate(HELPERS.UI.timeAxis.actualEndDate);
                            }
                        },
                    },
                    {
                        type: "checkbox",
                        label: "Highlight/Filter",
                        checked: shouldHighlight,
                        contentElementCls: "b-checkbox-user",
                        style: "display:none;",
                        onChange({ checked }) {
                            return;
                            HELPERS.UI.highlighting(workCenterScheduler, "");
                            HELPERS.UI.highlighting(machineScheduler, "");

                            HELPERS.UI.filter(workCenterScheduler, "");
                            HELPERS.UI.filter(machineScheduler, "");

                            shouldHighlight = checked;
                            if (mScheduler.selectedEvents.length) {
                                //var key = mScheduler.selectedEvents[0].name;
                                //var ordNum = mScheduler.selectedEvents[0].resourceId;
                                if (shouldHighlight) {
                                    HELPERS.UI.highlighting(
                                        workCenterScheduler,
                                        mScheduler.selectedEvents
                                    );
                                    HELPERS.UI.highlighting(
                                        machineScheduler,
                                        mScheduler.selectedEvents
                                    );
                                } else {
                                    HELPERS.UI.filter(
                                        workCenterScheduler,
                                        mScheduler.selectedEvents
                                    );
                                    HELPERS.UI.filter(machineScheduler, mScheduler.selectedEvents);
                                }
                            }
                        },
                    },
                ],
            });

            popupColumnConfiguration = new SP.Popup({
                header: Localizations.CURRENT.Column_Configuration,
                autoShow: false,
                centered: true,
                closable: true,
                modal: true,
                closeAction: "hide",
                width: "50em",
                minHeight: "30em",
                listeners: {
                    beforeShow: (aa) => {
                        //debugger;
                        popupColumnConfiguration.widgetMap.pcfListAvailableColumns.store =
                            popupColumnConfiguration.for.columns.chain(
                                (record) => record.text && record.text != "X" && record.hidden
                            );

                        popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store =
                            popupColumnConfiguration.for.columns.chain(
                                (record) => record.text && record.text != "X" && !record.hidden
                            );

                        popupColumnConfiguration.widgetMap.pcfTfFilterAvailableColumns.store =
                            popupColumnConfiguration.widgetMap.pcfListAvailableColumns.store;
                        popupColumnConfiguration.widgetMap.pcfTfFilterAvailableColumns.field =
                            "text";

                        popupColumnConfiguration.widgetMap.pcfTfFilterSelectedColumns.store =
                            popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store;
                        popupColumnConfiguration.widgetMap.pcfTfFilterSelectedColumns.field =
                            "text";
                    },
                },
                bbar: [{
                        type: "container",
                        width: "100%",
                        style: "padding-top: 1em",
                        items: [{
                            type: "button",
                            ref: "pcfBtnResetToDefault",
                            text: Localizations.CURRENT.Reset_To_Default,
                            onAction: () => {
                                SP.MessageDialog.confirm({
                                    title: Localizations.CURRENT.Confirm,
                                    message: Localizations.CURRENT.Confirm_Reset_Text,
                                }).then(
                                    function(ss) {
                                        if (!ss) return;
                                        window.parent.postMessage({
                                            cmd: "resetColumns",
                                            newData: {
                                                source: HELPERS.UI.KEYS[popupColumnConfiguration.for.ref],
                                            },
                                        });
                                    },
                                    function(ss) {}
                                );
                            },
                        }, ],
                    },
                    {
                        type: "container",
                        width: "100%",
                        style: "width: 100%; padding-top: 1em; justify-content: flex-end;",
                        items: [{
                                type: "button",
                                text: Localizations.CURRENT.Cancel,
                                minWidth: 100,
                                style: "margin-right: 20px;",
                                ref: "pcfBtnCancel",
                                onAction: "up.close",
                            },
                            {
                                type: "button",
                                text: Localizations.CURRENT.OK,
                                minWidth: 100,
                                cls: "b-raised b-blue",
                                ref: "pcfBtnOk",
                                onAction: () => {
                                    popupColumnConfiguration.hide();
                                },
                            },
                        ],
                    },
                ],
                items: [{
                    type: "container",
                    layoutStyle: {
                        flexDirection: "row",
                        flexWrap: "nowrap",
                        alignItems: "flex-start",
                    },
                    items: [{
                            type: "container",
                            width: "46%",
                            items: [{
                                    type: "container",
                                    html: Localizations.CURRENT.Available_Columns,
                                },
                                {
                                    type: "filterfield",
                                    clearable: true,
                                    placeholder: Localizations.CURRENT.Column_Configuration_filter,
                                    ref: "pcfTfFilterAvailableColumns",
                                    listeners: {
                                        change(aa) {
                                            if (!aa.value) return;
                                            popupColumnConfiguration.widgetMap.pcfListAvailableColumns.selected.clear();
                                        },
                                    },
                                },
                                {
                                    type: "list",
                                    width: "100%",
                                    height: 225,
                                    scrollable: true,
                                    ref: "pcfListAvailableColumns",
                                    itemTpl: (item) => `${item.text}`,
                                    listeners: {
                                        item(aa) {
                                            popupColumnConfiguration.widgetMap.pcfBtnRight.disabled =
                                                aa.source.count == 0;
                                        },
                                    },
                                    selected: {
                                        listeners: {
                                            change(aa) {
                                                popupColumnConfiguration.widgetMap.pcfBtnRight.disabled =
                                                    aa.source.count == 0;
                                            },
                                        },
                                    },
                                },
                            ],
                        },
                        {
                            type: "container",
                            cls: "dlg-colconf-container-vert",
                            items: [{
                                    type: "button",
                                    cls: "b-transparent",
                                    icon: "b-fa-arrow-right",
                                    ref: "pcfBtnRight",
                                    disabled: true,
                                    onAction: (aa) => {
                                        popupColumnConfiguration.widgetMap.pcfListAvailableColumns.selected.first.hidden = false;
                                        popupColumnConfiguration.widgetMap.pcfListAvailableColumns.store.fillFromMaster();
                                        popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store.fillFromMaster();
                                        popupColumnConfiguration.widgetMap.pcfTfFilterAvailableColumns.value =
                                            "";
                                        popupColumnConfiguration.widgetMap.pcfTfFilterSelectedColumns.value =
                                            "";
                                        popupColumnConfiguration.widgetMap.pcfBtnRight.disabled = true;
                                        popupColumnConfiguration.widgetMap.pcfListAvailableColumns.selected.clear();
                                    },
                                },
                                {
                                    type: "button",
                                    cls: "b-transparent",
                                    icon: "b-fa-arrow-left",
                                    ref: "pcfBtnLeft",
                                    disabled: true,
                                    onAction: (aa) => {
                                        popupColumnConfiguration.widgetMap.pcfListSelectedColumns.selected.first.hidden = true;
                                        popupColumnConfiguration.widgetMap.pcfListAvailableColumns.store.fillFromMaster();
                                        popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store.fillFromMaster();
                                        popupColumnConfiguration.widgetMap.pcfTfFilterAvailableColumns.value =
                                            "";
                                        popupColumnConfiguration.widgetMap.pcfTfFilterSelectedColumns.value =
                                            "";
                                        popupColumnConfiguration.widgetMap.pcfBtnLeft.disabled = true;
                                        popupColumnConfiguration.widgetMap.pcfListSelectedColumns.selected.clear();
                                    },
                                },
                            ],
                        },
                        {
                            type: "container",
                            width: "46%",
                            items: [{
                                    type: "container",
                                    html: Localizations.CURRENT.Selected_Columns,
                                },
                                {
                                    type: "filterfield",
                                    clearable: true,
                                    placeholder: Localizations.CURRENT.Column_Configuration_filter,
                                    ref: "pcfTfFilterSelectedColumns",
                                    listeners: {
                                        change(aa) {
                                            if (!aa.value) return;
                                            popupColumnConfiguration.widgetMap.pcfListSelectedColumns.selected.clear();
                                        },
                                    },
                                },
                                {
                                    type: "list",
                                    width: "100%",
                                    height: 225,
                                    scrollable: true,
                                    ref: "pcfListSelectedColumns",
                                    listeners: {
                                        item(aa) {
                                            popupColumnConfiguration.widgetMap.pcfBtnLeft.disabled =
                                                aa.source.count == 0;
                                        },
                                    },
                                    selected: {
                                        listeners: {
                                            change(aa) {
                                                var isDis = aa.source.count == 0;
                                                popupColumnConfiguration.widgetMap.pcfBtnLeft.disabled =
                                                    isDis;
                                                popupColumnConfiguration.widgetMap.pcfBtnUp.disabled =
                                                    isDis;
                                                popupColumnConfiguration.widgetMap.pcfBtnDown.disabled =
                                                    isDis;
                                            },
                                        },
                                    },
                                },
                            ],
                        },
                        {
                            type: "container",
                            cls: "dlg-colconf-container-vert",
                            items: [{
                                    type: "button",
                                    cls: "b-transparent",
                                    icon: "b-fa-arrow-up",
                                    ref: "pcfBtnUp",
                                    disabled: true,
                                    onAction: (aa) => {
                                        var str =
                                            popupColumnConfiguration.widgetMap.pcfListSelectedColumns
                                            .store;
                                        var record =
                                            popupColumnConfiguration.widgetMap.pcfListSelectedColumns
                                            .selected.first;
                                        if (!record) return;
                                        var prev = str.getPrev(record);
                                        popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store.masterStore.remove(
                                            record
                                        );
                                        if (!prev) {
                                            popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store.add(
                                                record
                                            );
                                        } else {
                                            popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store.masterStore.insert(
                                                prev.parentIndex,
                                                record
                                            );
                                        }
                                        popupColumnConfiguration.for.columns.trigger("update", {
                                            store: popupColumnConfiguration.for.columns,
                                            record,
                                            changes: { parentIndex: 100 },
                                        });
                                        //popupColumnConfiguration.widgetMap.pcfListAvailableColumns.store.fillFromMaster();
                                        //popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store.fillFromMaster();
                                    },
                                },
                                {
                                    type: "button",
                                    cls: "b-transparent",
                                    icon: "b-fa-arrow-down",
                                    ref: "pcfBtnDown",
                                    disabled: true,
                                    onAction: (aa) => {
                                        var str =
                                            popupColumnConfiguration.widgetMap.pcfListSelectedColumns
                                            .store;
                                        var record =
                                            popupColumnConfiguration.widgetMap.pcfListSelectedColumns
                                            .selected.first;
                                        if (!record) return;
                                        var next = str.getNext(record);
                                        next = str.getNext(next);
                                        popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store.masterStore.remove(
                                            record
                                        );
                                        if (!next) {
                                            popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store.add(
                                                record
                                            );
                                        } else {
                                            popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store.masterStore.insert(
                                                next.parentIndex,
                                                record
                                            );
                                        }
                                        popupColumnConfiguration.for.columns.trigger("update", {
                                            store: popupColumnConfiguration.for.columns,
                                            record,
                                            changes: { parentIndex: 100 },
                                        });
                                        //popupColumnConfiguration.widgetMap.pcfListAvailableColumns.store.fillFromMaster();
                                        //popupColumnConfiguration.widgetMap.pcfListSelectedColumns.store.fillFromMaster();
                                    },
                                },
                            ],
                        },
                    ],
                }, ],
            });

            mScheduler = HELPERS.UI.paintMScheduler();
            workCenterScheduler = HELPERS.UI.paintWorkCenterScheduler().then(function(
                histogram
            ) {
                histogram.columns.on({
                    update: HELPERS.UI.sendColumnsToServer,
                });

                machineScheduler = HELPERS.UI.paintMachinesScheduler().then(function(
                    histogram
                ) {
                    histogram.columns.on({
                        update: HELPERS.UI.sendColumnsToServer,
                    });

                    setTimeout(function() {
                        mScheduler.subGrids.locked.width = HELPERS.UI.gridWidth || 500;
                        if (HELPERS.UI.ScrollLeft)
                            mScheduler.scrollLeft = HELPERS.UI.ScrollLeft;
                        if (HELPERS.UI.ScrollTop) mScheduler.scrollTop = HELPERS.UI.ScrollTop;
                        if (HELPERS.UI.gridWidth)
                            mScheduler.subGrids.locked.width = HELPERS.UI.gridWidth;

                        if (HELPERS.UI.tempCont3isVisible != undefined) {
                            !HELPERS.UI.tempCont3isVisible ? tempCont3.hide() : tempCont3.show();
                            !HELPERS.UI.tempCont2isVisible ? tempCont2.hide() : tempCont2.show();
                        }
                        HELPERS.UI.isPainting = undefined;
                    }, 50);
                });
            });
            //setTimeout(function () { try { console.clear(); } catch (ee) { } }, 300);
        },

        paintMScheduler: function() {
                var e = HELPERS.DATA.myParams.json;
                var changedColumns = HELPERS.UI.persistColumns(
                    mScheduler,
                    e.productionOrders.columns
                );
                return new Scheduler({
                            appendTo: "OrdersContainer",
                            resourceStore: {
                                data: e.productionOrders.resources,
                                listeners: {
                                    update(prm) {
                                        if (prm.changes && prm.changes.selected) {
                                            var obj = {
                                                OrderNumber: prm.record.get("ordNum"),
                                                OrderType: prm.record.get("ordType"),
                                                Selected: prm.changes.selected.value,
                                            };
                                            window.parent.postMessage({
                                                cmd: "updateOrderResourceSelected",
                                                newData: obj,
                                            });
                                            return;
                                        }
                                        var obj = {
                                            OrderNumber: prm.record.get("ordNum"),
                                            OrderType: prm.record.get("ordType"),
                                            DispatchPriority: prm.record.get("priority"),
                                            Constraint: DH.add(
                                                prm.record.get("constraint"), -1 * prm.record.get("constraint").getTimezoneOffset(),
                                                "m"
                                            ),
                                            FirmSchedule: prm.record.get("firmSchedule"),
                                            Selected: prm.record.get("selected"),
                                        };
                                        window.parent.postMessage({
                                            cmd: "updateOrderResource",
                                            newData: obj,
                                        });
                                    },
                                },
                            },
                            events: e.productionOrders.events,
                            columns: {
                                data: changedColumns,
                                listeners: {
                                    update(prm) {
                                        if (!prm.changes) return;
                                        if (prm.type != "update") return;
                                        if (prm.record.type == "timeAxis") return;
                                        if (prm.record.field == "columnChoiceDialog") return;
                                        if (prm.changes.width && prm.changes.width.value > 1000) return;
                                        if (
                                            prm.changes.hidden ||
                                            prm.changes.parentIndex ||
                                            prm.changes.width
                                        ) {
                                            if (!HELPERS.UI.t1) HELPERS.UI.t1 = [];
                                            HELPERS.UI.t1.push(Date.now());
                                            setTimeout(function() {
                                                HELPERS.UI.t1.pop();
                                                if (HELPERS.UI.t1.length) {
                                                    return;
                                                }
                                                var records = prm.source.records;
                                                var columns = [];
                                                records.forEach(function(r) {
                                                    if (r.field != "columnChoiceDialog")
                                                        columns.push({
                                                            field: r.field,
                                                            hidden: r.hidden,
                                                            width: r.width,
                                                        });
                                                });
                                                window.parent.postMessage({
                                                    cmd: "updateColumns",
                                                    newData: { source: "productionOrders", columns: columns },
                                                });
                                            }, 2200);
                                        }
                                    },
                                },
                            },
                            startDate: HELPERS.UI.timeAxis.haveActual ?
                                HELPERS.UI.timeAxis.actualStartDate : HELPERS.UI.startDate || e.productionOrders.startDate,
                            endDate: HELPERS.UI.timeAxis.haveActual ?
                                HELPERS.UI.timeAxis.actualEndDate : HELPERS.UI.endDate || e.productionOrders.endDate,

                            ref: "mScheduler",
                            readOnly: false,

                            subGridConfigs: {
                                normal: { flex: 1 },
                                locked: { width: HELPERS.UI.gridWidth || 500 },
                            },

                            horizontalEventSorterFn: function(a, b) {
                                if (a.isMilestone) return 1;
                                return a.name.localeCompare(b.name);
                            },
                            presets: presetsArr,
                            viewPreset: HELPERS.UI.viewPreset || "weekAndDay",
                            rowHeight: 40,
                            barMargin: 3,
                            multiEventSelect: true,

                            features: {
                                stripe: true,
                                cellEdit: true,
                                cellMenu: false,
                                eventMenu: false,
                                eventDragCreate: false,
                                enableDeleteKey: false,
                                eventEdit: false,
                                simpleEventEdit: false,
                                eventResize: false,
                                scheduleMenu: false,

                                headerMenu: {
                                    items: {
                                        customItem: {
                                            text: "Reset Columns",
                                            icon: "b-fa b-fa-recycle",
                                            cls: "b-separator color",
                                            weight: 80,
                                            name: "custom",
                                            onItem: ({ item, column }) => {
                                                SP.MessageDialog.confirm({
                                                    title: Localizations.CURRENT.Confirm,
                                                    message: Localizations.CURRENT.Confirm_Reset_Text,
                                                }).then(
                                                    function(ss) {
                                                        if (!ss) return;
                                                        window.parent.postMessage({
                                                            cmd: "resetColumns",
                                                            newData: { source: "productionOrders" },
                                                        });
                                                    },
                                                    function(ss) {}
                                                );
                                            },
                                        },
                                    },
                                },

                                eventDrag: {
                                    constrainDragToResource: true,
                                    validatorFn: function({ draggedRecords, newResource }) {
                                        const evt = draggedRecords[0],
                                            isValid = !evt.isMilestone;

                                        return {
                                            valid: isValid,
                                            message: "",
                                        };
                                    },
                                },
                                scheduleTooltip: false,
                                eventTooltip: {
                                    header: {
                                        titleAlign: "start",
                                    },
                                    template: function(data) {
                                            var dt = data.eventRecord.endDate;
                                            var wcEvt = HELPERS.UI.getWCEvent(data.eventRecord.resourceId);
                                            var wc = "";
                                            if (wcEvt && wcEvt.get("resourceId")) {
                                                wc = wcEvt.get("resourceId").split("-")[0];
                                            }
                                            if (DH.format(dt, "LT") == "12:00 AM" && data.eventRecord.duration)
                                                dt = DH.add(dt, -1, "d");
                                            var s = `<dl>
					<dt>${data.eventRecord.name} ${
							wc
								? "(" +
								  wc +
								  " / " +
								  data.eventRecord.resources[0].get("ordNum") +
								  ")"
								: "(" + data.eventRecord.resources[0].get("ordNum") + ")"
						}</dt>
					<dt style='display: inline;'>${Localizations.CURRENT.PERIOD}:</dt>
					<dd style='display: inline;'>
						${DH.format(
							data.eventRecord.startDate,
							HELPERS.DATA.formats.dateFormatL
						)} - ${DH.format(dt, HELPERS.DATA.formats.dateFormatL)}
					</dd>
					${
						data.eventRecord.get("descr")
							? `<dt>${Localizations.CURRENT.OPERATION_DESCRIPTION}:</dt><dd>${data.eventRecord.descr}</dd>`
							: ""
					}
					<dt>${Localizations.CURRENT.PRODUCTION_ORDER_INFORMATION}:</dt>
					<dd style='display: block;
	margin-inline-start: 0px;
	white-space: normal;'>
	${HELPERS.UI.getTooltipPart(
		mScheduler,
		data.eventRecord.resources[0],
		"ordNum",
		"Order Nbr."
	)}
	${HELPERS.UI.getTooltipPart(
		mScheduler,
		data.eventRecord.resources[0],
		"invId",
		"Inventory ID"
	)}
	${HELPERS.UI.getTooltipPart(
		mScheduler,
		data.eventRecord.resources[0],
		"ordTypeDescr",
		"Order Type"
	)}
	${HELPERS.UI.getTooltipPart(
		mScheduler,
		data.eventRecord.resources[0],
		"ordStatus_description",
		"Status"
	)}
	${HELPERS.UI.getTooltipPart(
		mScheduler,
		data.eventRecord.resources[0],
		"customerId",
		"Customer ID"
	)}
	${HELPERS.UI.getTooltipPart(
		mScheduler,
		data.eventRecord.resources[0],
		"customerName",
		"Customer"
	)}
					</dd>
				</dl>
				`;
						return s;
					},
				},
			},

			listeners: {
				beforeCellEditStart(obj) {
					return !obj.editorContext.record.get("readOnly");
				},
				beforePresetChange: function (cfg) {
					return HELPERS.UI.beforePresetChange(cfg);
				},
				paint({ firstPaint }) {
					if (firstPaint) {
						SP.EventHelper.on({
							element: this.element,
							delegate: 'div.b-grid-header[data-column="columnChoiceDialog"]',
							click: (event) => {
								event.stopPropagation();
								popupColumnConfiguration.for = mScheduler;
								popupColumnConfiguration.show();
							},
							capture: true,
							thisObj: this,
						});
					}
				},
				scheduleDblClick: function (cfg) {
					cfg.event.preventDefault();
					return false;
				},
				selectionChange: function (cfg) {
					HELPERS.UI.mrowwasselected1 = true;
				},
				cellClick: function (cfg) {
					if (cfg.column.type != "timeAxis") return;
					cfg.grid.clearEventSelection();
					cfg.grid.element
						.querySelectorAll("." + mScheduler.eventSelectedCls)
						.forEach(function (el) {
							el.classList.remove(mScheduler.eventSelectedCls);
						});
					workCenterScheduler.clearEventSelection();
					machineScheduler.clearEventSelection();

					if (!HELPERS.UI.mrowwasselected1) {
						cfg.source.deselectRow(cfg.record);
					} else {
						if (cfg.record.events) {
							var evt = cfg.record.events.reduce(function (p, v) {
								return p.startDate.getTime() < v.startDate.getTime() ? p : v;
							});
						}
						/*
						var evts = cfg.record.events.sort(function (p, v) {
							if (p.startDate.getTime() == v.startDate.getTime()) return 0;
							if (p.startDate.getTime() < v.startDate.getTime()) return 1;
							if (p.startDate.getTime() > v.startDate.getTime()) return -1;
						  });
						*/
						mScheduler.selectEvents(cfg.record.events);

						if (evt) {
							setTimeout(function () {
								mScheduler.scrollEventIntoView(evt, { focus: false });
								mScheduler.selectedEvents.forEach((bb) =>
									mScheduler.repaintEvent(bb)
								);
								cfg.grid.element
									.querySelectorAll(
										'[data-resource-id="' + cfg.cellSelector.id + '"]>div'
									)
									.forEach((gg) => gg.classList.add("b-sch-event-selected"));
							}, 70);
						}
					}
					HELPERS.UI.mrowwasselected1 = false;
					/*
					var allSelElements = cfg.grid.element.querySelectorAll('.b-custom-select');
					var currentElement = cfg.target.closest('.b-grid-row');
					var currentIsSelected = currentElement.classList.contains("b-custom-select");

					var currentElementElement = cfg.grid.element.querySelectorAll('[data-region="locked"][data-ref="locked"]>[data-id="' + cfg.cellSelector.id + '"]')
					if (currentElementElement) currentElementElement = currentElementElement[0];

					allSelElements.forEach(function (el) { el.classList.remove('b-custom-select'); });

					if (currentIsSelected) {
						return;
					}
					else {
						currentElement.classList.add("b-custom-select");
						currentElementElement.classList.add("b-custom-select");
						//mScheduler.selectEvents(cfg.record.events);
					}
					*/
				},
				scheduleClick: function (data) {
					return;
					if (shouldHighlight) {
						HELPERS.UI.highlighting(workCenterScheduler, "");
						HELPERS.UI.highlighting(machineScheduler, "");
					} else {
						HELPERS.UI.filter(workCenterScheduler, "");
						HELPERS.UI.filter(machineScheduler, "");
					}
				},
				eventClick: function (cfg) {
					/*var allSelElements = document.querySelectorAll('.b-custom-select');
					allSelElements.forEach(function (el) { el.classList.remove('b-custom-select'); });
					if (allSelElements.length) {*/
					mScheduler.deselectAll();
					mScheduler.clearEventSelection();
					mScheduler.selectEvent(cfg.eventRecord);
				},
				eventSelectionChange: function ({
					action,
					selected,
					deselected,
					selection,
				}) {
					return;
					workCenterScheduler.clearEventSelection();
					machineScheduler.clearEventSelection();
					if (shouldHighlight) {
						HELPERS.UI.highlighting(workCenterScheduler, selection, 1);
						HELPERS.UI.highlighting(machineScheduler, selection, 1);
					} else {
						HELPERS.UI.filter(workCenterScheduler, selection, 1);
						HELPERS.UI.filter(machineScheduler, selection, 1);
					}
				},
			},
			eventRenderer: function ({ eventRecord, resourceRecord, renderData }) {
				var color = HELPERS.UI.colors.milestones.green;
				switch (HELPERS.DATA.myParams.colorCoding.orders) {
					case HELPERS.UI.colors.colorCodingOrders.WorkCenter:
						var ind = HELPERS.UI.getWorkCenterIndex(
							resourceRecord.get("id"),
							eventRecord.get("name")
						);
						if (ind != -1)
							color = HELPERS.UI.colors.dispatchPriority[ind > 10 ? 10 : ind];
						break;
					case HELPERS.UI.colors.colorCodingOrders.Status:
						Object.keys(HELPERS.UI.colors.statuses).forEach(function (key) {
							var x = HELPERS.UI.colors.statuses[key];
							if (key.toLowerCase() == resourceRecord.ordStatus.toLowerCase()) {
								color = x.color;
							}
						});
						break;
					case HELPERS.UI.colors.colorCodingOrders.ByOrderType:
						Object.keys(HELPERS.UI.colors.orderTypes).forEach(function (key) {
							var x = HELPERS.UI.colors.orderTypes[key];
							if (key.toLowerCase() == resourceRecord.ordType.toLowerCase()) {
								color = x.color;
							}
						});
						break;
					case HELPERS.UI.colors.colorCodingOrders.DispatchPriority:
						var pr = resourceRecord.priority || 0;
						if (pr > 11) pr = 12;
						if (pr < 0) pr = 12;
						pr--;
						color = HELPERS.UI.colors.dispatchPriority[pr];
						break;
					case HELPERS.UI.colors.colorCodingOrders.FirmSchedule:
						color =
							HELPERS.UI.colors.dispatchPriority[
								resourceRecord.firmSchedule ? 1 : 0
							];
						break;
				}
				/*
				if (eventRecord.isMilestone)
					renderData.cls += ' bb-milestone';
				*/
				var addIconCls = "";
				if (!eventRecord.isMilestone) {
					if (eventRecord.get("outside"))
						renderData.iconCls.add("b-fa b-fa-circle");
					if (eventRecord.get("lackOfMaterials"))
						addIconCls += '<i class="b-fa b-fa-bolt"></i>';
				}

				if (eventRecord.isMilestone && resourceRecord.requestedOn) {
					var rDt = DH.startOf(resourceRecord.requestedOn, "day");
					var eDt = DH.startOf(resourceRecord.endDate, "day");
					if (rDt.getTime() > eDt.getTime())
						color = HELPERS.UI.colors.milestones.green;
					else if (rDt.getTime() == eDt.getTime())
						color = HELPERS.UI.colors.milestones.yellow;
					else color = HELPERS.UI.colors.milestones.red;
				} else if (eventRecord.isMilestone) {
					color = HELPERS.UI.colors.milestones.yellow;
				}
				renderData.eventColor = color;
				/*if (eventRecord.isMilestone) { // no work
					if (color==HELPERS.UI.colors.milestones.green){
						//renderData.iconCls.add('b-fa b-fa-circle');
					}
					if (color==HELPERS.UI.colors.milestones.yellow){
						//renderData.iconCls.add('b-fa b-fa-circle');
					}
					if (color==HELPERS.UI.colors.milestones.red){
						//renderData.iconCls.add('b-fa b-fa-times');
					}
				}*/

				// Event contents
				//return SP.StringHelper.xss`<div>${eventRecord.name}</div>`;
				var innerText = "";
				if (!eventRecord.isMilestone) {
					innerText = eventRecord.name;
					if (resourceRecord.get("ordStatus_description"))
						innerText += " " + resourceRecord.get("ordStatus_description");
					if (resourceRecord.get("qtyP"))
						innerText += " " + resourceRecord.get("qtyP");
					var wcEvt = HELPERS.UI.getWCEvent(resourceRecord.get("id"));
					if (wcEvt && wcEvt.get("resourceId")) {
						innerText += " " + wcEvt.get("resourceId").split("-")[0];
					}
				}
				return addIconCls + SP.StringHelper.encodeHtml(innerText);
			},
		});
	},

	paintWorkCenterScheduler: function () {
		var e = HELPERS.DATA.myParams.json;
		var changedColumns = HELPERS.UI.persistColumns(
			undefined,
			e.workCenters.columns
		);
		var pr = new SP.ProjectModel();
		var inlineData = {};

		inlineData.resourcesData = e.workCenters.resources;
		inlineData.eventsData = e.workCenters.events.map((g) => {
			return {
				id: g.id,
				name: g.name,
				startDate: g.startDate /*new Date(g.startDate - 3 * 60 * 60 * 1000)*/,
				endDate: g.endDate /*new Date(g.endDate - 3 * 60 * 60 * 1000)*/,
			};
		});
		inlineData.assignmentsData = e.workCenters.events.map((r) => {
			return { event: r.id, resource: r.resourceId };
		});
		inlineData.calendarsData = e.calendarsData;

		pr.calendar = "workweek";
		// https://www.bryntum.com/docs/scheduler-pro/#SchedulerPro/model/ProjectModel#field-hoursPerDay
		pr.hoursPerDay = 8;
		pr.daysPerWeek = 5;
		pr.dependenciesCalendar = "Project";

		pr.inlineData = inlineData;
		return pr.commitAsync().then(function () {
			workCenterScheduler = new ResourceHistogram({
				appendTo: "WorkCenterContainer",
				partner: mScheduler,
				columns: changedColumns,
				project: pr,
				presets: presetsArr,
				startDate: undefined,
				endDate: undefined,
				ref: "workCenterScheduler",
				rowHeight: 60,
				barMargin: 4,
				showBarTip: true,
				multiEventSelect: false,
				getRectClass(series, rectConfig, datum, index) {
					if (series.id === "effort") {
						if (datum.effot > datum.maxEeffot) return "b-overallocated";
						else if (datum.effort < datum.maxEffort) return "b-underallocated";
					}
					return "";
				},

				getBarTip(series, rectConfig, datum, index) {
					if (!datum.effort) return;
					const resourceHistogram = this.owner,
						{ showBarTip, timeAxis, project } = resourceHistogram;
					var result = "";
					//debugger

					const unit = resourceHistogram.getBarTipEffortUnit(...arguments),
						allocated =
							project.convertDuration(
								datum.effort,
								SP.TimeUnit.Millisecond,
								unit
							) *
								(60 / e.parameters.blockSizeInMinutes) +
							"BL",
						//resourceHistogram.getEffortText(datum.effort, unit),
						available =
							project.convertDuration(
								datum.maxEffort,
								SP.TimeUnit.Millisecond,
								unit
							) *
								(60 / e.parameters.blockSizeInMinutes) +
							"BL";
					//resourceHistogram.getEffortText(datum.maxEffort, unit);
					//if (datum.resource.code == 'WC30') debugger

					var _tArr = [];
					datum.assignments.forEach((ff) => _tArr.push(ff.data.eventId));
					var _evtArr = HELPERS.DATA.myParams.json.workCenters.events
						.filter((f) => _tArr.indexOf(f.id) != -1)
						.map((m) => {
							return { name: m.name, ordNum: m.ordNum };
						});
					var _s = "<table>";
					_s +=
						_evtArr
							.map(
								(m) =>
									"<tr><td>" + m.ordNum + "</td><td>" + m.name + "</td></tr>"
							)
							.join("") + "</table>";
					//effortInUnits = project.convertDuration(effort, TimeUnit.Millisecond, unit);
					var dateFormat = HELPERS.DATA.formats.dateFormatL,
						resultFormat = resourceHistogram.L("L{barTipInRange}");

					if (DH.compareUnits(timeAxis.unit, "day") == 0) {
						resultFormat = resourceHistogram.L("L{barTipOnDate}");
					} else if (DH.compareUnits(timeAxis.unit, "second") <= 0) {
						dateFormat = "HH:mm:ss A";
					} else if (DH.compareUnits(timeAxis.unit, "hour") <= 0) {
						dateFormat = "LT";
					}
					result = resultFormat
						.replace(
							"{resource}",
							datum.resource.code + " " + datum.resource.shift
						)
						.replace("{startDate}", DH.format(datum.tick.startDate, dateFormat))
						.replace("{endDate}", DH.format(datum.tick.endDate, dateFormat))
						.replace("{allocated}", allocated)
						.replace("{available}", available);
					return result + _s;
				},
				features: {
					stripe: false,
					cellEdit: false,
					cellMenu: false,
					eventMenu: false,
					eventDragCreate: false,
					enableDeleteKey: false,
					eventEdit: false,
					eventResize: false,
					simpleEventEdit: false,
					scheduleMenu: false,

					headerMenu: {
						items: {
							customItem: {
								text: "Reset Columns",
								icon: "b-fa b-fa-recycle",
								cls: "b-separator color",
								weight: 80,
								name: "custom",
								onItem: ({ item, column }) => {
									SP.MessageDialog.confirm({
										title: Localizations.CURRENT.Confirm,
										message: Localizations.CURRENT.Confirm_Reset_Text,
									}).then(
										function (ss) {
											if (!ss) return;
											window.parent.postMessage({
												cmd: "resetColumns",
												newData: { source: "workCenters" },
											});
										},
										function (ss) {}
									);
								},
							},
						},
					},

					timeRanges: {
						showHeaderElements: true,
						showCurrentTimeLine: false,
					},
					scheduleTooltip: false,
				},

				//workCenterScheduler
				listeners: {
					beforePresetChange: function (cfg) {
						return HELPERS.UI.beforePresetChange(cfg);
					},
					paint({ firstPaint }) {
						if (firstPaint) {
							SP.EventHelper.on({
								element: this.element,
								delegate: 'div.b-grid-header[data-column="columnChoiceDialog"]',
								click: (event) => {
									event.stopPropagation();
									popupColumnConfiguration.for = workCenterScheduler;
									popupColumnConfiguration.show();
								},
								capture: true,
								thisObj: this,
							});
						}
					},
					scheduleDblClick: function (cfg) {
						cfg.event.preventDefault();
						return false;
					},
					cellClick: function (cfg) {
						cfg.source.deselectRow(cfg.record);
					},
					eventClick: function (source, eventRecord, assignmentRecord, event) {
						mScheduler.deselectAll();
						HELPERS.UI.selectEvent(
							mScheduler,
							source.eventRecord.name,
							source.eventRecord.data.ordRef,
							1,
							1
						);
					},
				},
			});
			return workCenterScheduler;
		});
	},

	paintMachinesScheduler: function () {
		var e = HELPERS.DATA.myParams.json;
		var changedColumns = HELPERS.UI.persistColumns(
			machineScheduler,
			e.machines.columns
		);

		var pr = new SP.ProjectModel();
		var inlineData = {};

		inlineData.resourcesData = e.machines.resources;
		inlineData.eventsData = e.machines.events.map((g) => {
			return {
				id: g.id,
				name: g.name,
				startDate: g.startDate /*new Date(g.startDate - 3 * 60 * 60 * 1000)*/,
				endDate: g.endDate /*new Date(g.endDate - 3 * 60 * 60 * 1000)*/,
			};
		});
		inlineData.assignmentsData = e.machines.events.map((r) => {
			return { event: r.id, resource: r.resourceId };
		});
		inlineData.calendarsData = e.calendarMachineData;

		pr.calendar = "workweek";
		// https://www.bryntum.com/docs/scheduler-pro/#SchedulerPro/model/ProjectModel#field-hoursPerDay
		pr.hoursPerDay = 8;
		pr.daysPerWeek = 5;
		pr.dependenciesCalendar = "Project";

		pr.inlineData = inlineData;

		return pr.commitAsync().then(function () {
			machineScheduler = new ResourceHistogram({
				appendTo: "MachineContainer",
				partner: mScheduler,
				columns: changedColumns,
				project: pr,
				presets: presetsArr,
				startDate: undefined,
				endDate: undefined,
				ref: "machineScheduler",
				rowHeight: 40,
				barMargin: 4,
				showBarTip: true,
				multiEventSelect: false,

				getRectClass(series, rectConfig, datum, index) {
					if (series.id === "effort") {
						if (datum.effot > datum.maxEeffot) return "b-overallocated";
						else if (datum.effort < datum.maxEffort) return "b-underallocated";
					}
					return "";
				},
				getBarTip(series, rectConfig, datum, index) {
					if (!datum.effort) return;
					const resourceHistogram = this.owner,
						{ showBarTip, timeAxis, project } = resourceHistogram;
					var result = "";

					const unit = resourceHistogram.getBarTipEffortUnit(...arguments),
						allocated =
							project.convertDuration(
								datum.effort,
								SP.TimeUnit.Millisecond,
								unit
							) *
								(60 / e.parameters.blockSizeInMinutes) +
							"BL",
						available =
							project.convertDuration(
								datum.maxEffort,
								SP.TimeUnit.Millisecond,
								unit
							) *
								(60 / e.parameters.blockSizeInMinutes) +
							"BL";

					var _tArr = [];
					datum.assignments.forEach((ff) => _tArr.push(ff.data.eventId));
					var _evtArr = HELPERS.DATA.myParams.json.machines.events
						.filter((f) => _tArr.indexOf(f.id) != -1)
						.map((m) => {
							return { name: m.name, ordNum: m.ordNum };
						});
					var _s = "<table>";
					_s +=
						_evtArr
							.map(
								(m) =>
									"<tr><td>" + m.ordNum + "</td><td>" + m.name + "</td></tr>"
							)
							.join("") + "</table>";
					var dateFormat = HELPERS.DATA.formats.dateFormatL,
						resultFormat = resourceHistogram.L("L{barTipInRange}");

					if (DH.compareUnits(timeAxis.unit, "day") == 0) {
						resultFormat = resourceHistogram.L("L{barTipOnDate}");
					} else if (DH.compareUnits(timeAxis.unit, "second") <= 0) {
						dateFormat = "HH:mm:ss A";
					} else if (DH.compareUnits(timeAxis.unit, "hour") <= 0) {
						dateFormat = "LT";
					}
					result = resultFormat
						.replace(
							"{resource}",
							datum.resource.code + " " + datum.resource.shift
						)
						.replace("{startDate}", DH.format(datum.tick.startDate, dateFormat))
						.replace("{endDate}", DH.format(datum.tick.endDate, dateFormat))
						.replace("{allocated}", allocated)
						.replace("{available}", available);
					return result + _s;
				},
				features: {
					stripe: false,
					cellEdit: false,
					cellMenu: false,
					eventMenu: false,
					eventDragCreate: false,
					enableDeleteKey: false,
					eventEdit: false,
					eventResize: false,
					simpleEventEdit: false,
					scheduleMenu: false,

					headerMenu: {
						items: {
							customItem: {
								text: "Reset Columns",
								icon: "b-fa b-fa-recycle",
								cls: "b-separator color",
								weight: 80,
								name: "custom",
								onItem: ({ item, column }) => {
									SP.MessageDialog.confirm({
										title: Localizations.CURRENT.Confirm,
										message: Localizations.CURRENT.Confirm_Reset_Text,
									}).then(
										function (ss) {
											if (!ss) return;
											window.parent.postMessage({
												cmd: "resetColumns",
												newData: { source: "machines" },
											});
										},
										function (ss) {}
									);
								},
							},
						},
					},

					timeRanges: {
						showHeaderElements: true,
						showCurrentTimeLine: false,
					},
					scheduleTooltip: false,
				},

				listeners: {
					beforePresetChange: function (cfg) {
						return HELPERS.UI.beforePresetChange(cfg);
					},
					paint({ firstPaint }) {
						if (firstPaint) {
							SP.EventHelper.on({
								element: this.element,
								delegate: 'div.b-grid-header[data-column="columnChoiceDialog"]',
								click: (event) => {
									event.stopPropagation();
									popupColumnConfiguration.for = machineScheduler;
									popupColumnConfiguration.show();
								},
								capture: true,
								thisObj: this,
							});
						}
					},
					cellClick: function (cfg) {
						cfg.source.deselectRow(cfg.record);
					},
					eventClick: function (source, eventRecord, assignmentRecord, event) {
						mScheduler.deselectAll();
						HELPERS.UI.selectEvent(
							mScheduler,
							source.eventRecord.name,
							source.eventRecord.data.ordRef,
							1,
							1
						);
					},
				},
			});
			return machineScheduler;
		});
	},
};

//////////////

HELPERS.UI.colorsSchemas = {
	schemaLine1: [
		"#5EB2CC",
		"#0B7922",
		"#AB7556",
		"#9458CD",
		"#FF4F00",
		"#9E9E9E",
		"#8E0404",
		"#4200EF",
		"#48DA00",
		"#E400B3",
		"#FF8282",
		"#10EADD",
	],
};
HELPERS.UI.colors = {
	statuses: {
		R: { color: HELPERS.UI.colorsSchemas.schemaLine1[0] },
		P: { color: HELPERS.UI.colorsSchemas.schemaLine1[1] },
		I: { color: HELPERS.UI.colorsSchemas.schemaLine1[2] },
		H: { color: HELPERS.UI.colorsSchemas.schemaLine1[3] },
		X: { color: HELPERS.UI.colorsSchemas.schemaLine1[4] },
		M: { color: HELPERS.UI.colorsSchemas.schemaLine1[5] },
		C: { color: HELPERS.UI.colorsSchemas.schemaLine1[6] },
		D: { color: HELPERS.UI.colorsSchemas.schemaLine1[7] },
	},
	orderTypes: {
		DA: { color: HELPERS.UI.colorsSchemas.schemaLine1[0] },
		PJ: { color: HELPERS.UI.colorsSchemas.schemaLine1[1] },
		PL: { color: HELPERS.UI.colorsSchemas.schemaLine1[2] },
		PM: { color: HELPERS.UI.colorsSchemas.schemaLine1[3] },
		RO: { color: HELPERS.UI.colorsSchemas.schemaLine1[4] },
		SP: { color: HELPERS.UI.colorsSchemas.schemaLine1[5] },
	},
	dispatchPriority: HELPERS.UI.colorsSchemas.schemaLine1,
	milestones: {
		red: "#FF0000",
		green: "#00E841",
		yellow: "#FEF400",
	},
	colorCodingOrders: {
		ByOrderType: "OT",
		WorkCenter: "WC",
		Status: "ST",
		DispatchPriority: "DP",
		FirmSchedule: "FS",
	},
	colorCodingResources: {
		ByOrderType: "OT",
		Status: "ST",
		ScheduleStatus: "SS",
	},
};

HELPERS.UI.LocaleHelper = {
	applyLocale: function () {
		var locale = { DateHelper: {} };
		if (
			(HELPERS.DATA.myParams.json.options.culture || "").toLowerCase() !=
			"en-us"
		) {
			locale.DateHelper = {
				locale: HELPERS.DATA.myParams.json.options.culture,
			};
			locale.NumberFormat = {
				locale: HELPERS.DATA.myParams.json.options.culture,
			};
		}
		// todo: apply de/ru locales
		if (top) {
			if (top.__PXCalendar && top.__PXCalendar.firstDayOfWeek)
				locale.DateHelper.weekStartDay = top.__PXCalendar.firstDayOfWeek;
			if (top._dateFormatInfo) {
				locale.DateHelper.parsers = {
					L: top._dateFormatInfo.shortDate.toUpperCase().trim(),
					LT: top._dateFormatInfo.shortTime.replace("tt", "A").trim(),
				};
			}
		}
		setTimeout(() => SP.LocaleManager.extendLocale("En", locale));
	},
}

HELPERS.DATA = {
	orderWcList: [],
	formats: {
		dateFormat:
			top && top._dateFormatInfo
				? (
						top._dateFormatInfo.shortDate.toUpperCase() +
						" " +
						top._dateFormatInfo.shortTime.replace("tt", "A")
				  ).trim()
				: "YYYY-MM-DD H:mm",
		dateFormatL:
			top && top._dateFormatInfo
				? top._dateFormatInfo.shortDate.toUpperCase().trim()
				: "YYYY-MM-DD",
		dateFormatWithoutYear:
			top && top._dateFormatInfo
				? top._dateFormatInfo.shortDate.toUpperCase().trim().replace(/.y+/i,'')
				: "MM-DD",
		numberFormat:
			top && top._numbFormatInfo
				? "9" +
				  (top._numbFormatInfo.number.groupSeparator ? "," : "") +
				  "999" +
				  (top._numbFormatInfo.number.decimalSeparator ? "." : ".") +
				  "##"
				: "9,999.99##",
		intFormat:
			top && top._numbFormatInfo
				? "9" + (top._numbFormatInfo.number.groupSeparator ? "," : "") + "999"
				: "9,999",
	},
	myParams: {
		colorCoding: {
			orders: HELPERS.UI.colors.colorCodingOrders.Status,
			resources: HELPERS.UI.colors.colorCodingResources.Status,
		},
		displayNonWorkingDays: true,
		json: null,
	},
	prepareData: function (data, options) {
		if (!data.productionOrders) data.productionOrders = {};
		if (!data.productionOrders.resources) data.productionOrders.resources = [];
		/*if (!data.productionOrders.resources.length) {
			data.productionOrders.resources.push({"ordType": "QT", "ordNum": "AM000088", selected: false});
		}*/
		if (!data.productionOrders.columns) data.productionOrders.columns = [];
		if (!data.productionOrders.events) data.productionOrders.events = [];

		if (!data.workCenters) data.workCenters = {};
		if (!data.workCenters.resources) data.workCenters.resources = [];
		if (!data.workCenters.columns) data.workCenters.columns = [];
		if (!data.workCenters.events) data.workCenters.events = [];

		if (!data.machines) data.machines = {};
		if (!data.machines.resources) data.machines.resources = [];
		if (!data.machines.columns) data.machines.columns = [];
		if (!data.machines.events) data.machines.events = [];

		HELPERS.DATA.orderWcList = [];
		data.workCenters.events.forEach(function (wce) {
			var a1 = data.workCenters.resources.filter(function (r) {
				return r.id == wce.resourceId;
			});
			if (a1.length) {
				var code = a1[0].code;
				var a2 = HELPERS.DATA.orderWcList.filter(function (r) {
					return r.wcCode == code;
				});
				if (a2.length) {
					a2[0].orderNumbers.push({ ordRef: wce.ordRef, name: wce.name });
				} else {
					HELPERS.DATA.orderWcList.push({
						wcCode: code,
						orderNumbers: [{ ordRef: wce.ordRef, name: wce.name }],
					});
				}
			}
		});
		//debugger
		var internalDateFormat = "YYYY-MM-DDTH:mm";
		data.productionOrders.resources.forEach(function (r) {
			r.requestedOn = DH.parse(r.requestedOn, internalDateFormat);
			r.startDate = DH.parse(r.startDate, internalDateFormat);
			r.endDate = DH.parse(r.endDate, internalDateFormat);
			r.ordDate = DH.parse(r.ordDate, internalDateFormat);
			r.constraint = DH.parse(r.constraint, internalDateFormat);
		});
		var minD = new Date(2050, 1, 1),
			maxD = new Date(2000, 1, 1);
		data.productionOrders.events.forEach(function (r) {
			r.startDate = DH.parse(r.startDate, internalDateFormat);
			r.endDate = DH.parse(r.endDate, internalDateFormat);
			r.draggable = false;
			if (r.startDate < minD) minD = r.startDate;
			if (r.endDate > maxD) maxD = r.endDate;
		});
		HELPERS.UI.timeAxis = {};
		if (minD.getTime() != new Date(2050, 1, 1).getTime()) {
			HELPERS.UI.timeAxis.haveActual = true;
			HELPERS.UI.timeAxis.actualStartDate = minD;
			HELPERS.UI.timeAxis.actualEndDate = DH.add(maxD, 1, "d");
		}
		data.productionOrders.resources.forEach(function (r) {
			if (r.requestedOn) {
				data.productionOrders.events.push({
					descr: "",
					endDate: r.requestedOn,
					id: data.productionOrders.events.length + 1,
					name: "",
					outside: false,
					resourceId: r.id,
					startDate: r.requestedOn,
				});
			}
		});
		data.workCenters.events.forEach(function (r) {
			r.startDate = DH.parse(r.startDate, internalDateFormat);
			r.endDate = DH.parse(r.endDate, internalDateFormat);
			r.draggable = false;
		});
		data.machines.events.forEach(function (r) {
			r.startDate = DH.parse(r.startDate, internalDateFormat);
			r.endDate = DH.parse(r.endDate, internalDateFormat);
			r.draggable = false;
		});
		if (!HELPERS.UI.startDate) {
			data.productionOrders.startDate = data.parameters.startDate
				? DH.parse(data.parameters.startDate)
				: DH.add(new Date(), -3, "d");
			data.productionOrders.endDate = data.parameters.endDate
				? DH.parse(data.parameters.endDate)
				: DH.add(new Date(), 3, "d");
		} else {
			data.productionOrders.startDate = HELPERS.UI.startDate;
			data.productionOrders.endDate = HELPERS.UI.endDate;
		}
		var columnNormalize = function (clm) {
			switch (clm.type) {
				case "Boolean":
					clm.type = "check";
					break;
				case "String":
				case "DBNull":
				case "Object":
				case "Empty":
				case "Char":
					clm.type = "column";
					break;
				case "SByte":
				case "Byte":
				case "Int16":
				case "UInt16":
				case "Int32":
				case "UInt32":
				case "Int64":
				case "UInt64":
					clm.type = "number";
					clm.format = HELPERS.DATA.formats.intFormat;
					break;
				case "Single":
				case "Double":
				case "Decimal":
					clm.type = "number";
					clm.format = HELPERS.DATA.formats.numberFormat;
					break;
				case "DateTime":
					clm.type = "date";
					clm.format = HELPERS.DATA.formats.dateFormat;
					break;
			}

			clm.field = clm.field.slice(0, 1).toLowerCase() + clm.field.slice(1);
			clm.id = clm.field;

			if (clm.field == "ordNum") {
				clm.type = "template";
				clm.editor = false;
				clm.template = function (data) {
					data.value = `<a href="${
						top ? top.location.pathname : "/Main"
					}?ScreenId=AM201500&OrderType=${data.record.get(
						"ordType"
					)}&ProdOrdID=${data.record.get(
						"ordNum"
					)}" target="_blank">${data.record.get("ordNum")}</a>`;
					return data.value;
				};
			}

			if (clm.field == "hold") {
				clm.type = "template";
				clm.editor = false;
				clm.template = function (data) {
					data.value = `<input type='checkbox' class='b-user-holdcheckbox' disabled ${
						data.record.get("hold") ? "checked" : ""
					}>`;
					return data.value;
				};
			}
		};

		data.productionOrders.columns.unshift(HELPERS.UI.columnChoiceDialog);
		data.productionOrders.columns.forEach(function (clm) {
			columnNormalize(clm);
			if (
				!(
					clm.field == "constraint" ||
					clm.field == "priority" ||
					clm.field == "selected"
				)
			) {
				clm.editor = { readOnly: true };
			}
			if (clm.field == "priority") {
				clm.min = 1;
				clm.max = 10;
				clm.editor = { required: true };
			}
			if (clm.field == "firmSchedule") {
				clm.listeners = {
					beforeToggle: function(obj){
						return false;
					}
				}
			}
		});

		data.workCenters.columns.unshift(HELPERS.UI.columnChoiceDialog);
		data.workCenters.columns.forEach(function (clm) {
			columnNormalize(clm);
		});

		data.machines.columns.unshift(HELPERS.UI.columnChoiceDialog);
		data.machines.columns.forEach(function (clm) {
			columnNormalize(clm);
		});

		var wwCalendar = data.calendarsData.filter((f) => f.id == "workweek");

		if (!wwCalendar.length) {
			data.calendarsData.push({
				id: "workweek",
				name: "work Week",
				unspecifiedTimeIsWorking: false,
				intervals: [
					{
						recurrentStartDate: "at 0:00 on Mon",
						recurrentEndDate: "at 0:00 on Sat",
						isWorking: true,
					},
				],
			});
		} else {
			wwCalendar[0].unspecifiedTimeIsWorking = false;
		}

		wwCalendar = data.calendarMachineData.filter((f) => f.id == "workweek");

		if (!wwCalendar.length) {
			data.calendarMachineData.push({
				id: "workweek",
				name: "work Week",
				unspecifiedTimeIsWorking: false,
				intervals: [
					{
						recurrentStartDate: "at 0:00 on Mon",
						recurrentEndDate: "at 0:00 on Sat",
						isWorking: true,
					},
				],
			});
		} else {
			wwCalendar[0].unspecifiedTimeIsWorking = false;
		}

		data.workCenters.resources.sort((a, b) => {
			return a.id.localeCompare(b.id);
		});
		data.machines.resources.sort((a, b) => {
			return a.id.localeCompare(b.id);
		});

		if (!data.parameters.blockSizeInMinutes)
			data.parameters.blockSizeInMinutes = 30;
		if (!data.parameters.workCentreCalendarType)
			data.parameters.workCentreCalendarType = "ByShifts";
		switch (data.parameters.workCentreCalendarType) {
			case "ByShifts":
				data.workCenters.resources = data.workCenters.resources.map((r) => {
					return {
						...r,
						calendar: r.shiftCode || r.id,
					};
				});
				data.machines.resources = data.machines.resources.map((r) => {
					return {
						...r,
						calendar: r.id,
					};
				});
				break;
			case "Common":
			default:
				break;
		}

		HELPERS.DATA.myParams.json = data;
		HELPERS.DATA.myParams.json.options = options;
		HELPERS.DATA.myParams.displayNonWorkingDays =
			data.parameters.displayNonWorkingDays ||
			HELPERS.DATA.myParams.displayNonWorkingDays;
		HELPERS.DATA.myParams.colorCoding.orders =
			data.parameters.colorCodingOrders ||
			HELPERS.DATA.myParams.colorCoding.orders;
		HELPERS.DATA.myParams.colorCoding.resources =
			data.parameters.colorCodingResources ||
			HELPERS.DATA.myParams.colorCoding.resources;
	},
};