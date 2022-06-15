'use strict';

const boxModeClass = 'mode-box';
const preRowModeClass = 'mode-prerow';
const rowModeClass = 'mode-row';
const columnModeClass = 'mode-column';
const exitTableDefiningButtonClass = 'exitTableDefiningButton';
const addNewCommandName = 'AddNew';

function RecognizedValueMapper(viewerContainer, fieldBoundFeedbackControl, tableRelatedFeedbackControl, vendorFieldName, dumpTableFeedbackCallback,
    poNumberFieldName, poNumberJsonFieldName, linesHintSingleLine, linesHintMultipleLines,
    linesHintSelectTextPrefix, linesHintSelectTextSingleLine, linesHintSelectTextMultipleLines,
    linesHintButonText, columnsExcludedFromColumnMapping) {
    this.modes = [boxModeClass, preRowModeClass, rowModeClass, columnModeClass];

    this.recognizedValues = [];
    this.recognizedWordValuesByPageWord = [];
    this.recognizedTables = [];

    this.selectedControl = null;
    this.selectedCell = null;
    this.selectedTable = null;
    this.selectedRowIndex = null;
    this.selectedRowIndexForAddColumnOption = null;
    this.isUserInput = true;
    this.rowsToUpdate = null;
    this.columnsToUpdateMap = null;
    this.isAddColumnOptionOn = false;

    this.formMappingByField = [];
    this.gridMappingByFieldRow = [];
    this.fieldRowSeparator = '*';
    this.gridDeletedRowRegexp = /^_\d+$/;
    this.columnsExcludedFromColumnMapping = columnsExcludedFromColumnMapping;
    this.columnsCommitChanges = [];

    this.viewerContainer = viewerContainer;
    this.formControl = null;
    this.gridControl = null;
    this.addNewButton = null;
    this.exitTableDefiningButton = null;
    this.mappingOptionsButton = null;
    this.updateColumnMappingMenuItem = null;
    this.addColumnsMenuItem = null;

    const that = this;
    const onButtonClickCallback = function () { that._switchToColumnMode(); }
    const onSelectAllLinesCallback = function () { that._allowSelectMoreRows(false); };
    const onSelectAllLinesPrevCallback = function () { that._allowSelectMoreRows(true); };
    this.linesHint = new LinesHint(this.viewerContainer.parentElement, linesHintSingleLine, linesHintMultipleLines,
        linesHintSelectTextPrefix, linesHintSelectTextSingleLine, linesHintSelectTextMultipleLines,
        linesHintButonText, onButtonClickCallback, onSelectAllLinesCallback, onSelectAllLinesPrevCallback);
    this.feedbackCollector = new FeedbackCollector(dumpTableFeedbackCallback, fieldBoundFeedbackControl, tableRelatedFeedbackControl, vendorFieldName);
    this.scroller = new RecognizedValueScroller();

    this.poNumberFieldName = poNumberFieldName;
    this.poNumberJsonFieldName = poNumberJsonFieldName;

    this._switchToBoxMode(false, false, false);
}

RecognizedValueMapper.prototype.addVendorSearchTerm = function (vendorTerm, vendorControlFieldName, vendorFieldName) {
    const mapping = this.formMappingByField[vendorControlFieldName];
    if (!mapping || !mapping.recognizedValues || mapping.recognizedValues.length !== 1) {
        return;
    }

    if (vendorTerm !== null) {
        const termRecognizedValue = mapping.recognizedValues[0];
        termRecognizedValue.addSearchTerm(vendorTerm);
    }

    for (let controlKey in this.formControl.controls) {
        let control = this.formControl.controls[controlKey];

        if (this._getFieldFromControl(control) !== vendorFieldName) {
            continue;
        }

        if (control.elemFocus) {
            control.elemFocus.focus();
        }

        break;
    }
}

RecognizedValueMapper.prototype._allowSelectMoreRows = function (allow) {
    this.recognizedTables.forEach(function (t) {
        t.allowSelectMoreRows(allow);
    });
}

RecognizedValueMapper.prototype.removeEventListeners = function () {
    this.recognizedTables.forEach(function (t) {
        t.removeEventListeners();
    });
}

RecognizedValueMapper.prototype.clearDetailsMapping = function () {
    this._clearSelectedCell();
    this._clearGridMapping();
}

RecognizedValueMapper.prototype.clear = function () {
    this.recognizedValues = [];
    this.recognizedTables = [];

    this._clearFormMapping();
    this._clearGridMapping();

    this.handleExitTableDefining(true);
}

RecognizedValueMapper.prototype._switchToMode = function (mode) {
    const modesToRemove = this.modes.filter(function (m) {
        return m !== mode;
    });

    const that = this;
    modesToRemove.forEach(function (m) {
        that.viewerContainer.classList.remove(m);
    });

    this.viewerContainer.classList.add(mode);
}

RecognizedValueMapper.prototype._isMode = function (mode) {
    return this.viewerContainer.classList.contains(mode);
}

RecognizedValueMapper.prototype._switchToBoxMode = function (skipGridEnable, reset, enableGridAfterAddColumn) {
    const toggleControlsVisibility = this._isRowMode() || this._isColumnMode();

    this._switchToMode(boxModeClass);

    if ((reset === false && toggleControlsVisibility === true) || enableGridAfterAddColumn === true) {
        this._setFormEnabled(true);

        if (skipGridEnable !== true) {
            this._setGridEnabled(true);
        }
    }

    this.linesHint.setVisible(false);

    if (this.exitTableDefiningButton !== null) {
        this.exitTableDefiningButton.setVisible(false);
    }

    this._cleanTables();
}

RecognizedValueMapper.prototype._isBoxMode = function () {
    return this._isMode(boxModeClass);
}

RecognizedValueMapper.prototype._switchToPreRowMode = function (skipGridEnable) {
    const toggleControlsVisibility = this._isRowMode() || this._isColumnMode();

    this._switchToMode(preRowModeClass);

    if (toggleControlsVisibility === true && this.isAddColumnOptionOn !== true) {
        this._setFormEnabled(true);

        if (skipGridEnable !== true) {
            this._setGridEnabled(true);
        }

        this.exitTableDefiningButton.setVisible(false);
        this.linesHint.setVisible(false);
    }

    if (this.isAddColumnOptionOn === true) {
        const selectLinesCount = this.gridControl.rows.items.length - this.selectedRowIndex;
        this.linesHint.setSelectMode(true, selectLinesCount);
        this.linesHint.setVisible(true);

        this._markCellAsNotMapped(this.selectedCell);
    }

    this._cleanTables();
}

RecognizedValueMapper.prototype._cleanTables = function () {
    this.recognizedTables.forEach(function (table) {
        table.reset();
    });

    const that = this;
    if (this.gridControl && this.gridControl.rows && this.gridControl.rows.items) {
        this.gridControl.rows.items.forEach(function (row) {
            if (row.cells) {
                row.cells.forEach(function (cell) {
                    if (cell !== that.selectedCell) {
                        that._markCellAsNotMapped(cell);
                    }
                });
            }
        });
    }
}

RecognizedValueMapper.prototype._isPreRowMode = function () {
    return this._isMode(preRowModeClass);
}

RecognizedValueMapper.prototype._isRowMode = function () {
    return this._isMode(rowModeClass);
}

RecognizedValueMapper.prototype._isColumnMode = function () {
    return this._isMode(columnModeClass);
}

RecognizedValueMapper.prototype._switchToRowMode = function () {
    this.selectedRowIndex = this.isAddColumnOptionOn === true ? this.selectedRowIndexForAddColumnOption : this.gridControl.activeRow.getIndex();

    this._switchToMode(rowModeClass);

    this._setFormEnabled(false);
    this._setGridEnabled(false);
    this.exitTableDefiningButton.setVisible(true);
    this.exitTableDefiningButton.setEnabled(true);

    if (this.isAddColumnOptionOn !== true) {
        this.linesHint.reset();
        this.linesHint.setVisible(true);
    }

    const that = this;
    this.recognizedTables.forEach(function (t) {
        if (t !== that.selectedTable) {
            t.hideRowsInRowMode();
            t.hideColumnsInColumnMode();
        }
    });
}

RecognizedValueMapper.prototype._switchToColumnMode = function () {
    if (this.selectedRowIndex === null && this.gridControl.activeRow) {
        this.selectedRowIndex = this.gridControl.activeRow.getIndex();
    }

    this.gridControl.batchUpdate = true;

    this.gridControl.activeRow.cells.forEach(function (c) {
        if (c.editor) {
            c.editor.hide();
        }
    });

    if (this.gridControl.activeCell) {
        this.gridControl.editMode = false;
        this.gridControl.activeCell.activate();
    }

    this._setGridEnabled(false);
    this.exitTableDefiningButton.setEnabled(true);

    this._switchToMode(columnModeClass);
    this.linesHint.setVisible(false);
    this.isUserInput = false;

    if (this.columnsCommitChanges.length === 0) {
        const that = this;

        this.gridControl.levels[0].columns.forEach(function (c) {
            that.columnsCommitChanges[c.index] = c.commitChanges;
            c.commitChanges = false;
        });
    }

    this.selectedTable.showSelectedColumnCells();
}

RecognizedValueMapper.prototype._navigateToMappedRect = function (control, getMappingValues) {
    const recognizedValues = getMappingValues(control);
    if (!recognizedValues) {
        return;
    }

    recognizedValues.forEach(function (rv) {
        rv.markAsMapped();
    });

    if (recognizedValues.length > 0) {
        const firstRv = recognizedValues[0];

        this.scroller.scrollToRecognizedValue(this.viewerContainer, firstRv);
    }
}

RecognizedValueMapper.prototype.trackFormControls = function (form) {
    this.formControl = form;
    this.feedbackCollector.formViewName = form.dataMember;
    const that = this;

    for (let controlName in form.controls) {
        let c = form.controls[controlName];

        c.events.addEventHandler('focus', function (control) {
            that.handleFormControlFocus(control);
        });

        c.events.addEventHandler('blur', function (control) {
            that.handleFormControlBlur(control);
        })

        c.events.addEventHandler('valueChanged', function (control) {
            that.handleFormControlValueChanged(control);
        });

        this._initFormMappingByControl(c);
    }
}

RecognizedValueMapper.prototype.handleFormControlFocus = function (control) {
    this._clearSelectedCell();

    this.selectedControl = control;

    this._navigateToFormRect(control);
}

RecognizedValueMapper.prototype._navigateToFormRect = function (control) {
    const that = this;
    const getMappingValues = function (control) {
        return that._getFormMappingValuesByControl(control);
    };

    this._navigateToMappedRect(control, getMappingValues);
}

RecognizedValueMapper.prototype.handleFormControlBlur = function (control) {
    this.selectedControl = null;

    const recognizedValues = this._getFormMappingValuesByControl(control);
    if (!recognizedValues) {
        return;
    }

    recognizedValues.forEach(function (rv) {
        rv.markAsNotMapped();
    });
}

RecognizedValueMapper.prototype.handleFormControlValueChanged = function (control) {
    if (this.isUserInput === false) {
        return;
    }

    const recognizedValues = this._getFormMappingValuesByControl(control);
    if (!recognizedValues) {
        return;
    }

    this._correctFormMapping(control, null, false);
}

RecognizedValueMapper.prototype._getFieldFromRecognizedValue = function (recognizedValue) {
    const fieldName = recognizedValue.fieldName;

    const indexOfDot = fieldName.indexOf('.');
    if (indexOfDot == -1) {
        return fieldName;
    }

    const length = fieldName.length;
    if (indexOfDot + 1 === length) {
        return fieldName;
    }

    return fieldName.slice(indexOfDot + 1);
}

RecognizedValueMapper.prototype._getPageWord = function (pageIndex, wordIndex) {
    return this._getFieldRow(pageIndex, wordIndex);
}

RecognizedValueMapper.prototype._getFieldRow = function (fieldName, rowIndex) {
    return fieldName + this.fieldRowSeparator + rowIndex;
}

RecognizedValueMapper.prototype._getFieldRowInfo = function (fieldRow) {
    const info = fieldRow.split(this.fieldRowSeparator);

    return {
        fieldName: info[0],
        rowIndex: info[1]
    };
}

RecognizedValueMapper.prototype._getFieldRowFromRecognizedValue = function (recognizedValue) {
    const fieldName = this._getFieldFromRecognizedValue(recognizedValue);

    return this._getFieldRow(fieldName, recognizedValue.rowIndex);
}

RecognizedValueMapper.prototype._getFieldRowFromCell = function (cell) {
    const fieldName = this._getFieldFromCell(cell);
    const rowIndex = cell.row.getIndex();

    return this._getFieldRow(fieldName, rowIndex);
}

RecognizedValueMapper.prototype._getFieldFromControl = function (control) {
    return control.serverID;
}

RecognizedValueMapper.prototype._getFieldFromCell = function (cell) {
    return cell.column.dataField;
}

RecognizedValueMapper.prototype._clearFormMapping = function () {
    for (let field in this.formMappingByField) {
        const mapping = this.formMappingByField[field];

        mapping.recognizedValues = [];
    }
}

RecognizedValueMapper.prototype._initFormMappingByControl = function (control) {
    const field = this._getFieldFromControl(control);
    const mapping = {
        control: control,
        recognizedValues: []
    };

    this.formMappingByField[field] = mapping;
}

RecognizedValueMapper.prototype._setFormMapping = function (control, recognizedValue, appendValue) {
    const field = this._getFieldFromControl(control);
    let mapping = this.formMappingByField[field];
    const rvArray = recognizedValue === null ? [] : [recognizedValue];
    let collectFeedback = true;

    if (!mapping) {
        mapping = {
            control: control,
            recognizedValues: rvArray
        };

        if (rvArray.length === 0) {
            collectFeedback = false;
        }
    }
    else if (appendValue === true) {
        mapping.recognizedValues.push(recognizedValue);
    }
    else {
        if (mapping.recognizedValues.length === 0 && rvArray.length === 0) {
            collectFeedback = false;
        }

        mapping.recognizedValues = rvArray;
    }

    this.formMappingByField[field] = mapping;

    if (collectFeedback === true) {
        const that = this;
        setTimeout(function () {
            const isUserInputPrev = that.isUserInput;

            try {
                that.isUserInput = false;
                that.feedbackCollector.collectFormFeedback(field, mapping.recognizedValues);
            }
            finally {
                that.isUserInput = isUserInputPrev;
            }
        }, 10);
    }
}

RecognizedValueMapper.prototype._setFormMappingByValue = function (recognizedValue) {
    const field = this._getFieldFromRecognizedValue(recognizedValue);

    const mapping = this.formMappingByField[field];
    if (!mapping) {
        return;
    }

    mapping.recognizedValues = [recognizedValue];
}

RecognizedValueMapper.prototype._getFormMappingValuesByControl = function (control) {
    const field = this._getFieldFromControl(control);
    const mapping = this.formMappingByField[field];

    return mapping.recognizedValues;
}

RecognizedValueMapper.prototype._correctMapping = function (control, recognizedValue, getMappingByControl, setMapping, appendValue, markValueAsMapped) {
    const prevRecognizedValues = getMappingByControl(control);

    setMapping(control, recognizedValue);

    if (prevRecognizedValues) {
        if (appendValue === false) {
            prevRecognizedValues.forEach(function (rv) {
                rv.markAsNotMapped();
            });
        }
        else {
            prevRecognizedValues.forEach(function (rv) {
                rv.markAsMapped();
            });
        }
    }

    if (recognizedValue && markValueAsMapped === true) {
        recognizedValue.markAsMapped();
    }
}

RecognizedValueMapper.prototype._correctFormMapping = function (control, recognizedValue, appendValue) {
    const that = this;
    const getMappingByControl = function (control) {
        return that._getFormMappingValuesByControl(control);
    };
    const setMapping = function (control, recognizedValue) {
        that._setFormMapping(control, recognizedValue, appendValue);
    };

    this._correctMapping(control, recognizedValue, getMappingByControl, setMapping, appendValue, true);
}

RecognizedValueMapper.prototype._correctGridMapping = function (cell, recognizedValue, appendValue, markValueAsMapped, column) {
    const that = this;
    const getMappingByControl = function (cell) {
        return that._getGridMappingValuesByCell(cell);
    };
    const setMapping = function (cell, recognizedValue) {
        that._setGridMapping(cell, recognizedValue, appendValue, column);
    };

    this._correctMapping(cell, recognizedValue, getMappingByControl, setMapping, appendValue, markValueAsMapped);
}

RecognizedValueMapper.prototype.trackGridControls = function (grid, exitTableModeButtonKey, mappingOptionsButtonKey,
    updateColumnMappingMenuIndex, addColumnsMenuIndex) {
    this.feedbackCollector.gridViewName = grid.dataMember.toLowerCase();
    this.gridControl = grid;
    this._initTableModeButtons(grid, exitTableModeButtonKey, mappingOptionsButtonKey, updateColumnMappingMenuIndex, addColumnsMenuIndex);

    const that = this;

    grid.events.addEventHandler('startCellEdit', function (g, e) {
        that._handleStartCellEdit(e);
    });

    grid.events.addEventHandler('endCellEdit', function (g, e) {
        that._handleEndCellEdit(e);
    });

    grid.events.addEventHandler('beforeCellUpdate', function (g, e) {
        that._handleBeforeCellUpdate(e);
    });

    grid.events.addEventHandler('beforeCellChange', function (g, e) {
        that._handleBeforeCellChange(e);
    });

    grid.events.addEventHandler('afterCellChange', function (g, e) {
        that._handleAfterCellChange(e);
    });

    grid.events.addEventHandler('beforeRowDelete', function (g, e) {
        that._handleBeforeRowDelete(e.row, e.rows);
    });

    grid.events.addEventHandler('afterRowDelete', function () {
        that._setMappingOptionsEnabled();
    })

    grid.events.addEventHandler('afterRepaintRow', function (g, e) {
        that._handleAfterRepaintRow(e.row);
    });

    grid.events.addEventHandler('beforeRowChange', function (g, e) {
        that._handleBeforeRowChange(e);
    });

    grid.events.addEventHandler('afterRowChange', function () {
        that._handleAfterRowChange();
    });

    grid.events.addEventHandler('toolsButtonClick', function (g, e) {
        if (that.exitTableDefiningButton === e.button) {
            that.handleExitTableDefining(false);
        }
    });

    grid.events.addEventHandler('afterRepaint', function () {
        that._handleAfterGridRepaint();
    });

    grid.events.addEventHandler('initialize', function () {
        that._setMappingOptionsEnabled();
    });
}

RecognizedValueMapper.prototype._handleAfterGridRepaint = function () {
    this._setMappingOptionsEnabled();
    this._clearGridErrorsMapping();
}

RecognizedValueMapper.prototype._setMappingOptionsEnabled = function () {
    const enabled = this.gridControl.rows.items && this.gridControl.rows.items.length > 0 &&
        this.addNewButton && this.addNewButton.getEnabled() === true;

    this.mappingOptionsButton.setEnabled(enabled);
}

RecognizedValueMapper.prototype._handleBeforeRowChange = function (e) {
    // Allow row change from code to update grid from selected table
    const isFromCode = e.eventType === null;
    const insertedRow = this.selectedRowIndex && e.row.getIndex() >= this.selectedRowIndex;

    e.cancel = (this._isRowMode() || this._isColumnMode()) && isFromCode === false && insertedRow === false;
}

RecognizedValueMapper.prototype._handleAfterRowChange = function () {
    this._setGridActionsStateInColumnMode();
}

RecognizedValueMapper.prototype._initTableModeButtons = function (grid, exitTableModeButtonKey, mappingOptionsButtonKey,
    updateColumnMappingMenuIndex, addColumnsMenuIndex) {
    if (this.exitTableDefiningButton !== null && this.mappingOptionsButton !== null &&
        this.updateColumnMappingMenuItem !== null && this.addColumnsMenuItem !== null &&
        this.addNewButton !== null) {
        return;
    }

    if (!grid.actionsTop || !grid.actionsTop.toolBar || !grid.actionsTop.toolBar.items) {
        return;
    }

    const exitTableDefiningButtons = grid.actionsTop.toolBar.items.filter(function (button) {
        return button.key && button.key.toLowerCase() === exitTableModeButtonKey.toLowerCase();
    });

    if (exitTableDefiningButtons.length === 1) {
        this.exitTableDefiningButton = exitTableDefiningButtons[0];
        this.exitTableDefiningButton.element.classList.add(exitTableDefiningButtonClass);
    }

    const mappingOptionsButtons = grid.actionsTop.toolBar.items.filter(function (button) {
        return button.key && button.key.toLowerCase() === mappingOptionsButtonKey.toLowerCase();
    });

    if (mappingOptionsButtons.length === 1) {
        this.mappingOptionsButton = mappingOptionsButtons[0];

        const menu = this.mappingOptionsButton.getMenu();
        const that = this;
        menu.events.addEventHandler('itemClick', function (m, e) {
            that._handleTableOptionItemClick(m, e);
        });

        menu.events.addEventHandler('activate', function () {
            that._handleTableOptionActivate();
        });

        this.updateColumnMappingMenuItem = menu.items.items[updateColumnMappingMenuIndex];
        this.addColumnsMenuItem = menu.items.items[addColumnsMenuIndex];
    }

    const addNewButtons = grid.actionsTop.toolBar.items.filter(function (button) {
        return button.commandName == addNewCommandName;
    });

    if (addNewButtons.length === 1) {
        this.addNewButton = addNewButtons[0];
    }
}

RecognizedValueMapper.prototype._handleTableOptionActivate = function () {
    this._initUpdateColumnMapping();
}

RecognizedValueMapper.prototype._handleTableOptionItemClick = function (menu, e) {
    if (e.item === this.updateColumnMappingMenuItem) {
        this._updateColumnOption();
    }
    else if (e.item === this.addColumnsMenuItem) {
        this._addColumnOption();
    }
}

RecognizedValueMapper.prototype._updateColumnOption = function () {
    const firstRow = this.gridControl.rows.items[0];
    firstRow.performActivate();

    this._setFormEnabled(false);
    this._setGridEnabled(false);
    this.exitTableDefiningButton.setVisible(true);
    this.exitTableDefiningButton.setEnabled(true);

    const that = this;
    this.recognizedTables.forEach(function (t) {
        if (t !== that.selectedTable) {
            t.hideColumnsInColumnMode();
        }
    });

    if (this.rowsToUpdate !== null) {
        this.selectedTable.activateRows(this.rowsToUpdate);
        this.rowsToUpdate = null;
    }

    this._switchToColumnMode();

    if (this.columnsToUpdateMap !== null) {
        this.selectedTable.activateColumns(this.columnsToUpdateMap);
        this.columnsToUpdateMap = null;
    }
}

RecognizedValueMapper.prototype._addColumnOption = function () {
    this.isAddColumnOptionOn = true;

    if (this._isBoxMode()) {
        this.selectedRowIndexForAddColumnOption = 0;
        this._setFormEnabled(false);
        this._setGridEnabled(false);
        this.exitTableDefiningButton.setVisible(true);
        this.exitTableDefiningButton.setEnabled(true);

        this._switchToPreRowMode();
    }
    else if (this._isColumnMode()) {
        this.selectedRowIndexForAddColumnOption = this.selectedRowIndex;
        this._switchToPreRowMode();
    }
}

RecognizedValueMapper.prototype._getMappingInfoFromCells = function () {
    let anyMappedCell = false;
    let mappedPage = null;
    let mappedTable = null;
    let mappedRows = [];
    let mappedColumnsInfoMap = new Map();

    for (let r = 0; r < this.gridControl.rows.items.length; r++) {
        const gridRow = this.gridControl.rows.items[r];
        if (!gridRow.cells) {
            continue;
        }

        let mappedRow = null;

        for (let c = 0; c < gridRow.cells.length; c++) {
            const gridCell = gridRow.cells[c];
            if (!gridCell || gridCell.getReadOnly() === true || gridCell.column.getVisible() === false) {
                continue;
            }

            const fieldRow = this._getFieldRowFromCell(gridCell);
            const mapping = this.gridMappingByFieldRow[fieldRow];

            if (!mapping || !mapping.recognizedValues) {
                continue;
            }

            if (mapping.recognizedValues.length === 0) {
                continue;
            }

            for (let v = 0; v < mapping.recognizedValues.length; v++) {
                const rv = mapping.recognizedValues[v];
                rv.fillCellInfo();

                if (rv.cellInfo.pageIndex === null || rv.cellInfo.tableIndex === null || rv.cellInfo.rowIndex === null) {
                    return null;
                }

                if (mappedPage === null) {
                    mappedPage = rv.cellInfo.pageIndex;
                }
                else if (mappedPage !== rv.cellInfo.pageIndex) {
                    return null;
                }

                if (mappedTable === null) {
                    mappedTable = rv.cellInfo.tableIndex;
                }
                else if (mappedTable !== rv.cellInfo.tableIndex) {
                    return null;
                }

                if (mappedRow === null) {
                    mappedRow = rv.cellInfo.rowIndex;
                }
                else if (mappedRow !== rv.cellInfo.rowIndex) {
                    return null;
                }

                anyMappedCell = true;
            }
        }

        if (mappedRow !== null) {
            mappedRows.push(mappedRow);
        }
    }

    if (anyMappedCell === false) {
        return null;
    }

    for (let c = 0; c < this.gridControl.levels[0].columns.length; c++) {
        const column = this.gridControl.levels[0].columns[c];
        if (column.getVisible() === false) {
            continue;
        }

        let mappedColumn = null;

        for (let r = 0; r < this.gridControl.rows.items.length; r++) {
            const gridRow = this.gridControl.rows.items[r];
            if (!gridRow.cells) {
                continue;
            }

            const gridCell = gridRow.cells[c];
            if (!gridCell || gridCell.getReadOnly() === true) {
                continue;
            }

            const fieldRow = this._getFieldRowFromCell(gridCell);
            const mapping = this.gridMappingByFieldRow[fieldRow];

            if (!mapping || !mapping.recognizedValues) {
                continue;
            }

            for (let v = 0; v < mapping.recognizedValues.length; v++) {
                const rv = mapping.recognizedValues[v];

                if (rv.cellInfo.columnIndex === null) {
                    return null;
                }

                if (mappedColumn === null) {
                    mappedColumn = rv.cellInfo.columnIndex;
                }
                else if (mappedColumn !== rv.cellInfo.columnIndex) {
                    return null;
                }
            }

            let columnMappingInfo = mappedColumnsInfoMap.get(mappedColumn);
            if (!columnMappingInfo) {
                columnMappingInfo = {
                    gridColumnIndex: c,
                    mappings: []
                };
                mappedColumnsInfoMap.set(mappedColumn, columnMappingInfo);
            }
            const info = {
                fieldRow: fieldRow,
                mapping: mapping
            };
            columnMappingInfo.mappings.push(info);
        }
    }

    return {
        pageIndex: mappedPage,
        tableIndex: mappedTable,
        rowIndices: mappedRows,
        columnInfoMap: mappedColumnsInfoMap
    };
}

RecognizedValueMapper.prototype.handleExitTableDefining = function (reset) {
    let skipGridEnable = false;

    if (this._isColumnMode() === true) {
        const that = this;

        this.gridControl.levels[0].columns.forEach(function (c) {
            c.commitChanges = that.columnsCommitChanges[c.index];
        });

        if (reset === false && this.selectedRowIndex !== null) {
            skipGridEnable = true;

            for (let i = this.selectedRowIndex; i < this.gridControl.rows.items.length; i++) {
                const row = this.gridControl.rows.getRow(i);
                row.dataChanged = true;
            }

            this.gridControl.update();
        }

        this.gridControl.batchUpdate = false;
    }

    let enableGridAfterAddColumn = skipGridEnable === false && this.isAddColumnOptionOn === true;

    this.isUserInput = true;
    this.isAddColumnOptionOn = false;

    if (this.selectedCell !== null && this.selectedCell.row.isServerNew() === true) {
        this._switchToPreRowMode(skipGridEnable);
    }
    else {
        this._switchToBoxMode(skipGridEnable, reset, enableGridAfterAddColumn);
    }
}

RecognizedValueMapper.prototype._initUpdateColumnMapping = function () {
    const mappedInfo = this._getMappingInfoFromCells();

    if (mappedInfo !== null) {
        this.updateColumnMappingMenuItem.setEnabled(true);

        const tables = this.recognizedTables.filter(function (t) {
            return t.pageIndex === mappedInfo.pageIndex && t.tableIndex === mappedInfo.tableIndex;
        });
        this.selectedTable = tables[0];

        this.rowsToUpdate = mappedInfo.rowIndices;
        this.columnsToUpdateMap = mappedInfo.columnInfoMap;
    }
    else {
        this.updateColumnMappingMenuItem.setEnabled(false);
    }
}

RecognizedValueMapper.prototype._handleAfterRepaintRow = function (row) {
    this._clearGridErrorsMapping();

    if (this._isPreRowMode() && row.isNew() !== true) {
        this._switchToBoxMode(false, false, false);
    }
}

RecognizedValueMapper.prototype._gridMappingHasError = function (mapping) {
    if (!mapping || !mapping.cell) {
        return false;
    }

    if (mapping.cell.hasError() === true) {
        return true;
    }

    const rowIndex = mapping.cell.row.getIndex();
    const row = this.gridControl.rows.getRow(rowIndex);
    if (row === null) {
        return false;
    }

    const cellIndex = mapping.cell.getIndex();
    const cell = row.getCell(cellIndex);
    if (cell === null) {
        return false;
    }

    return cell.hasError() === true;
}

RecognizedValueMapper.prototype._clearGridErrorsMapping = function () {
    const adjustedGridMappingbyFieldRow = [];

    for (let fieldRow in this.gridMappingByFieldRow) {
        const mapping = this.gridMappingByFieldRow[fieldRow];

        if (this._gridMappingHasError(mapping) === true) {
            if (mapping.recognizedValues) {
                mapping.recognizedValues.forEach(function (rv) {
                    rv.markAsNotMapped();
                });
            }

            continue;
        }

        adjustedGridMappingbyFieldRow[fieldRow] = mapping;
    }

    this.gridMappingByFieldRow = adjustedGridMappingbyFieldRow;
}

RecognizedValueMapper.prototype._handleBeforeRowDelete = function (row, rows) {
    const that = this;
    setTimeout(function () {
        that.feedbackCollector.dumpGridFeedback();
    }, 10);

    this._clearSelectedCell();

    let rowsArray;

    if (row) {
        rowsArray = [row];
    }
    else if (rows) {
        rowsArray = [];

        for (let r in rows) {
            if (this.gridDeletedRowRegexp.test(r) !== true) {
                continue;
            }

            rowsArray.push(rows[r]);
        }
    }
    else {
        return;
    }

    this._adjustGridMappingsBeforeRowDelete(rowsArray);
}

RecognizedValueMapper.prototype._adjustGridMappingsBeforeRowDelete = function (rowsToDelete) {
    const adjustedGridMappingbyFieldRow = [];

    for (let fieldRow in this.gridMappingByFieldRow) {
        const mapping = this.gridMappingByFieldRow[fieldRow];
        const fieldRowInfo = this._getFieldRowInfo(fieldRow);
        const fieldRowIndex = fieldRowInfo.rowIndex;
        const fieldRowName = fieldRowInfo.fieldName;
        let keepRow = true;
        let rowIndexDecrement = 0;

        for (let i = 0; i < rowsToDelete.length; i++) {
            let rowIndexToDelete = rowsToDelete[i].getIndex();

            if (fieldRowIndex == rowIndexToDelete) {
                keepRow = false;
                break;
            }

            if (fieldRowIndex > rowIndexToDelete) {
                rowIndexDecrement++;
            }
        }

        if (keepRow === false) {
            continue;
        }

        if (rowIndexDecrement > 0) {
            const adjustedRowIndex = fieldRowIndex - rowIndexDecrement;
            const adjustedFieldRow = this._getFieldRow(fieldRowName, adjustedRowIndex);

            adjustedGridMappingbyFieldRow[adjustedFieldRow] = mapping;
        }
        else {
            adjustedGridMappingbyFieldRow[fieldRow] = mapping;
        }
    }

    this.gridMappingByFieldRow = adjustedGridMappingbyFieldRow;
}

RecognizedValueMapper.prototype._navigateToCellRect = function (cell) {
    const that = this;
    const getMappingValues = function (control) {
        return that._getGridMappingValuesByCell(control);
    };

    this._navigateToMappedRect(cell, getMappingValues);
}

RecognizedValueMapper.prototype._clearSelectedCell = function () {
    if (this.selectedCell === null) {
        return;
    }

    this._markCellAsNotMapped(this.selectedCell);

    this.selectedCell = null;
}

RecognizedValueMapper.prototype._markCellAsNotMapped = function (cell) {
    const recognizedValues = this._getGridMappingValuesByCell(cell);
    if (recognizedValues === null) {
        return;
    }

    recognizedValues.forEach(function (rv) {
        rv.markAsNotMapped();
    });
}

RecognizedValueMapper.prototype._handleBeforeCellChange = function (e) {
    if (this._isColumnMode() === false) {
        return;
    }

    const skipColumn = e.cell && e.cell.column && e.cell.column.dataField &&
        this.columnsExcludedFromColumnMapping.indexOf(e.cell.column.dataField) !== -1;
    if (skipColumn === true) {
        e.cancel = true;
    }
}

RecognizedValueMapper.prototype._handleAfterCellChange = function (e) {
    if (this.isUserInput === false) {
        return;
    }

    this._clearSelectedCell();

    this.selectedControl = null;
    this.selectedCell = e.cell;

    this._navigateToCellRect(e.cell);
}

RecognizedValueMapper.prototype._handleStartCellEdit = function (e) {
    if (this._isBoxMode() && e.cell.row.isNew()) {
        this._switchToPreRowMode();
    }

    if (this.selectedCell === e.cell) {
        return;
    }

    if (this.isUserInput !== false) {
        this._clearSelectedCell();
    }

    this.selectedControl = null;
    this.selectedCell = e.cell;
}

RecognizedValueMapper.prototype._handleEndCellEdit = function (e) {
    if (this.isUserInput === false) {
        return;
    }

    if (this._isPreRowMode()) {
        this._switchToBoxMode(false, false, false);
    }

    if (this.selectedCell !== e.cell) {
        return;
    }

    this._clearSelectedCell();

    if (this._isBoxMode() === true || this._isPreRowMode() === true) {
        this._setMappingOptionsEnabled();
    }
}

RecognizedValueMapper.prototype._handleBeforeCellUpdate = function (e) {
    if (this.isUserInput === false) {
        return;
    }

    this._correctGridMapping(e.cell, null, false, false, null);
}

RecognizedValueMapper.prototype._clearGridMapping = function () {
    for (let fieldRow in this.gridMappingByFieldRow) {
        const mapping = this.gridMappingByFieldRow[fieldRow];

        mapping.recognizedValues = [];
    }
}

RecognizedValueMapper.prototype._initGridMappingByValue = function (recognizedValue) {
    const fieldName = this._getFieldFromRecognizedValue(recognizedValue);

    const row = this.gridControl.rows.getRow(recognizedValue.rowIndex);
    if (!row) {
        return;
    }

    const cell = row.getCell(fieldName);
    if (!cell) {
        return;
    }

    const mapping = {
        cell: cell,
        recognizedValues: [recognizedValue]
    };

    const fieldRow = this._getFieldRowFromRecognizedValue(recognizedValue);
    this.gridMappingByFieldRow[fieldRow] = mapping;
}

RecognizedValueMapper.prototype._getGridMappingValuesByCell = function (cell) {
    const isPONumberCell = this._getFieldFromCell(cell) === this.poNumberFieldName;

    return isPONumberCell ? this._getRecognizedWordValuesByCell(cell) : this._getGridMappingValuesByCellSimple(cell);
}

RecognizedValueMapper.prototype._getGridMappingValuesByCellSimple = function (cell) {
    const fieldRow = this._getFieldRowFromCell(cell);
    const mapping = this.gridMappingByFieldRow[fieldRow];

    return mapping ? mapping.recognizedValues : null;
}

RecognizedValueMapper.prototype._getRecognizedWordValuesByCell = function (cell) {
    const that = this;
    let poNumberFieldIndex = -1;
    cell.row.cells.some(function (c, i) {
        if (c && c.column && c.column.dataField === that.poNumberJsonFieldName) {
            poNumberFieldIndex = i;
            return true;
        }
    });
    if (poNumberFieldIndex === -1) {
        return [];
    }

    let poNumberJsonEncoded = cell.row.cells[poNumberFieldIndex].getValue();
    if (!poNumberJsonEncoded) {
        return [];
    }

    poNumberJsonEncoded = poNumberJsonEncoded.replace(/\+/g, '%20');
    const poNumberJson = decodeURIComponent(poNumberJsonEncoded);
    const pageWordInfo = JSON.parse(poNumberJson);
    if (!pageWordInfo || pageWordInfo.pageIndex === null || pageWordInfo.wordIndex === null) {
        return [];
    }

    const pageWord = this._getPageWord(pageWordInfo.Page, pageWordInfo.Word);

    return [this.recognizedWordValuesByPageWord[pageWord]];
}

RecognizedValueMapper.prototype._setGridMapping = function (cell, recognizedValue, appendValue, column) {
    const fieldRow = this._getFieldRowFromCell(cell);
    let mapping = this.gridMappingByFieldRow[fieldRow];
    const rvArray = recognizedValue === null ? [] : [recognizedValue];
    let prevRecognizedValues = null;

    if (!mapping) {
        mapping = {
            cell: cell,
            recognizedValues: rvArray
        };
    }
    else if (appendValue === true) {
        prevRecognizedValues = mapping.recognizedValues;

        if (recognizedValue !== null) {
            mapping.recognizedValues.push(recognizedValue);
        }
    }
    else {
        prevRecognizedValues = mapping.recognizedValues;
        mapping.recognizedValues = rvArray;
    }

    this.gridMappingByFieldRow[fieldRow] = mapping;
    this.collectGridFeedback(cell, mapping.recognizedValues, prevRecognizedValues);

    if (column !== null) {
        const cellIndex = cell.getIndex();
        column.setGridMapping(cellIndex, fieldRow, mapping);
    }
}

RecognizedValueMapper.prototype.collectGridFeedback = function (cell, recognizedValues, prevRecognizedValues) {
    const detailColumn = cell.column.dataField;
    const detailRow = cell.row.getIndex();
    let pageIndex = null;
    let tableIndex = null;
    let rowIndex = null;
    let columnIndexArray = [];

    if (recognizedValues.length === 0) {
        if (prevRecognizedValues === null || prevRecognizedValues.length === 0) {
            return;
        }

        const prevValue = prevRecognizedValues[0];
        if (prevValue === null || prevValue.cellInfo === null || prevValue.cellInfo.pageIndex === null || prevValue.cellInfo.tableIndex === null ||
            prevValue.cellInfo.rowIndex === null || prevValue.cellInfo.columnIndex === null) {
            return;
        }

        pageIndex = prevValue.cellInfo.pageIndex;
        tableIndex = prevValue.cellInfo.tableIndex;
        rowIndex = prevValue.cellInfo.rowIndex;
        columnIndexArray.push(-1);
    }
    else {
        const cellInfo = this._getCellInfo(recognizedValues);
        if (cellInfo === null) {
            return;
        }

        pageIndex = cellInfo.pageIndex;
        tableIndex = cellInfo.tableIndex;
        rowIndex = cellInfo.rowIndex;
        columnIndexArray = cellInfo.columnIndexArray;
    }

    const that = this;
    setTimeout(function () {
        const isUserInputPrev = that.isUserInput;

        try {
            that.isUserInput = false;
            that.feedbackCollector.collectGridFeedback(detailColumn, detailRow, pageIndex, tableIndex, columnIndexArray, rowIndex);
        }
        finally {
            that.isUserInput = isUserInputPrev;
        }
    }, 10);
}

RecognizedValueMapper.prototype._getCellInfo = function (recognizedValues) {
    let cellInfo = {
        pageIndex: null,
        tableIndex: null,
        rowIndex: null,
        columnIndexArray: []
    };

    for (let i = 0; i < recognizedValues.length; i++) {
        let rv = recognizedValues[i];

        if (rv.cellInfo === null || rv.cellInfo.pageIndex === null || rv.cellInfo.tableIndex === null || rv.cellInfo.rowIndex === null ||
            rv.cellInfo.columnIndex === null) {
            return null;
        }

        if (cellInfo.pageIndex === null) {
            cellInfo.pageIndex = rv.cellInfo.pageIndex;
        }
        else if (cellInfo.pageIndex !== rv.cellInfo.pageIndex) {
            return null;
        }

        if (cellInfo.tableIndex === null) {
            cellInfo.tableIndex = rv.cellInfo.tableIndex;
        }
        else if (cellInfo.tableIndex !== rv.cellInfo.pageIndex) {
            return null;
        }

        if (cellInfo.rowIndex === null) {
            cellInfo.rowIndex = rv.cellInfo.rowIndex;
        }
        else if (cellInfo.rowIndex !== rv.cellInfo.rowIndex) {
            return null;
        }

        cellInfo.columnIndexArray.push(rv.cellInfo.columnIndex);
    }

    return cellInfo;
}

RecognizedValueMapper.prototype.trackRecognizedValue = function (recognizedValue) {
    if (recognizedValue.isPrimaryField === true) {
        this._setFormMappingByValue(recognizedValue);
    }
    else if (recognizedValue.isDetailField === true) {
        this._initGridMappingByValue(recognizedValue);
    }

    this.recognizedValues.push(recognizedValue);

    if (recognizedValue.wordInfo && recognizedValue.wordInfo.word &&
        recognizedValue.wordInfo.pageIndex !== null && recognizedValue.wordInfo.wordIndex !== null) {
        const pageWord = this._getPageWord(recognizedValue.wordInfo.pageIndex, recognizedValue.wordInfo.wordIndex);
        this.recognizedWordValuesByPageWord[pageWord] = recognizedValue;
    }

    const that = this;

    recognizedValue.subscribeOnMousedown(function (v, e) {
        that._mapRecognizedValue(v, e);
    });
}

RecognizedValueMapper.prototype._recognizedValueMappingInProgress = function () {
    return this.selectedControl !== null || this.selectedCell !== null;
}

RecognizedValueMapper.prototype._mapRecognizedValue = function (recognizedValue, event) {
    if (this._recognizedValueMappingInProgress() === false) {
        return;
    }

    // do not lose focus from the selected control
    event.preventDefault();

    const appendValue = event.ctrlKey === true || event.metaKey === true;
    const that = this;

    let control;
    let getMappingByControl;
    let performMapping;

    if (this.selectedControl !== null) {
        control = this.selectedControl;

        getMappingByControl = function () {
            return that._getFormMappingValuesByControl(that.selectedControl);
        };

        performMapping = function (recognizedValue, newValue, appendValue) {
            that._mapRecognizedValueToForm(recognizedValue, newValue, appendValue);
        };

    }
    else if (this.selectedCell !== null) {
        control = this.selectedCell;

        getMappingByControl = function () {
            return that._getGridMappingValuesByCell(that.selectedCell);
        }

        performMapping = function (recognizedValue, newValue, appendValue) {
            that._mapRecognizedValueToCell(recognizedValue, newValue, appendValue);
        }
    }

    this._mapRecognizedValueToControl(control, recognizedValue, appendValue, getMappingByControl, performMapping);
}

RecognizedValueMapper.prototype._mapRecognizedValueToControl = function (control, recognizedValue, appendValue, getMappingByControl, performMapping) {
    const mappedValues = getMappingByControl();

    if (mappedValues !== null) {
        let rvIndex = -1;
        mappedValues.some(function (rv, i) {
            if (rv === recognizedValue) {
                rvIndex = i;
                return true;
            }
        });
        const alreadyMapped = rvIndex !== -1;
        if (alreadyMapped === true) {
            return;
        }
    }

    const value = recognizedValue.value != null ? recognizedValue.value : recognizedValue.text;
    if (value === null) {
        return;
    }

    const newControlValue = this._getValueToMap(control, value, appendValue);
    const validationResult = this._validValueAndParams(control, newControlValue, appendValue);

    if (validationResult.isValid === true) {
        performMapping(recognizedValue, newControlValue, validationResult.append);
    }
}

RecognizedValueMapper.prototype._isDateControl = function (control) {
    const controlValue = control.getValue();
    const isDateControlValue = (controlValue && controlValue.constructor.name === 'Date') ||
        (control.__className && control.__className.indexOf('Date') > 0);

    return isDateControlValue;
}

RecognizedValueMapper.prototype._isStringValue = function (value) {
    const isStringValue = value && value.constructor.name === 'String';

    return isStringValue;
}

RecognizedValueMapper.prototype._validValueAndParams = function (control, value, appendValue) {
    if (!value) {
        return {
            isValid: false,
            append: false
        };
    }

    if (control.getReadOnly && control.getReadOnly() === true) {
        return {
            isValid: false,
            append: false
        };
    }

    if (control.parseValue) {
        if (isNaN(control.parseValue(value)) === true) {
            return {
                isValid: false,
                append: false
            };
        }
        else {
            return {
                isValid: true,
                append: false
            };
        }
    }

    const append = appendValue === true && this._isDateControl(control) !== true && this._isStringValue(value) === true;

    return {
        isValid: true,
        append: append
    };
}

RecognizedValueMapper.prototype._getValueToMap = function (control, newValue, appendValue) {
    const isDateControlValue = this._isDateControl(control);
    const isStringNewValue = this._isStringValue(newValue);

    if (isDateControlValue === true && isStringNewValue === true) {
        try {
            const dateValue = new Date(newValue);

            if (dateValue < control.getMinValue() || dateValue > control.getMaxValue()) {
                return null;
            }

            // Convert date to mm/dd/yyyy format
            return Intl.DateTimeFormat('en-US').format(dateValue);
        }
        catch (e) {
            return null;
        }
    }

    if (appendValue === false || isStringNewValue === false) {
        return newValue;
    }

    const controlValue = control.getValue();
    const appendedValue = controlValue ? controlValue + ' ' + newValue : newValue;

    return appendedValue;
}

RecognizedValueMapper.prototype._mapRecognizedValueToForm = function (recognizedValue, value, appendValue) {
    let correctMapping = true;
    this.isUserInput = false;

    try {
        if (this.selectedControl.updateValue !== null) {
            this.selectedControl.updateValue(value)
        }
        else {
            this.selectedControl.setValue(value);
        }

        if (this.selectedControl.getValue() == null) {
            correctMapping = false;
        }
    }
    catch (e) {
        correctMapping = false;
    }
    finally {
        this.isUserInput = true;
    }

    if (correctMapping === true) {
        this._correctFormMapping(this.selectedControl, recognizedValue, appendValue);
    }
}

RecognizedValueMapper.prototype._mapRecognizedValueToCell = function (recognizedValue, value, appendValue) {
    if (this._isPreRowMode()) {
        this._switchToBoxMode(false, false, false);
    }

    let correctMapping = true;

    try {
        this.isUserInput = false;

        if (!this.gridControl.editMode) {
            this.gridControl.beginEdit();
        }

        if (this.selectedCell.updateValue !== null) {
            try {
                const oldValue = this.selectedCell.getValue();
                this.selectedCell.updateValue(value);

                const valueBeforeSave = this.selectedCell.getValue();
                this.gridControl.executeCommand('Save');
                const valueAfterSave = this.selectedCell.getValue();

                const isValueRestored = oldValue === valueAfterSave && valueBeforeSave === 0;
                if (isValueRestored) {
                    correctMapping = false;
                }
            }
            catch (e) {
                correctMapping = false;
            }
        }
        else {
            try {
                this.selectedCell.setValue(value);
            }
            catch (e) {
                correctMapping = false;
            }
        }
    }
    finally {
        this.gridControl.endEdit();
        this.isUserInput = true;
    }

    if (correctMapping === true) {
        this._correctGridMapping(this.selectedCell, recognizedValue, appendValue, true, null);
    }
}

RecognizedValueMapper.prototype.trackRecognizedTable = function (recognizedTable) {
    this.recognizedTables.push(recognizedTable);

    const that = this;
    recognizedTable.selectedRowsChangedCallback = function (table) { that._handleTableSelectedRowsChanged(table); };
    recognizedTable.columnSelectedCallback = function (column, event) { return that._handleColumnSelected(recognizedTable, column, event); };
    recognizedTable.columnUnselectedCallback = function (column) { that._handleColumnUnselected(column); };
    recognizedTable.maxRowsToSelect = function () { return that.linesHint.linesToSelect(); };
}

RecognizedValueMapper.prototype._setFormEnabled = function (isEnabled) {
    if (this.formControl === null) {
        return;
    }

    if (isEnabled === true) {
        this.formControl.refresh();
        return;
    }

    for (let controlKey in this.formControl.controls) {
        let control = this.formControl.controls[controlKey];
        control.setEnabled(false);
    }
}

RecognizedValueMapper.prototype._setGridEnabled = function (isEnabled) {
    if (this.gridControl === null) {
        return;
    }

    if (isEnabled === true) {
        this.gridControl.refresh();
        return;
    }

    if (this.gridControl.rows && this.gridControl.rows.items) {
        this.gridControl.rows.items.forEach(function (r) {
            for (let i = 0; i < r.cells.length; i++) {
                let cell = r.cells[i];
                if (!cell) {
                    continue;
                }

                cell.setReadOnly(true);
            }
        });
    }

    this.gridControl.levels[0].columns.forEach(function (c) {
        c.viewLink = false;
    });

    this._setGridActionsEnabled(isEnabled);
}

RecognizedValueMapper.prototype._setGridActionsEnabled = function (isEnabled) {
    if (this.gridControl.actionsTop && this.gridControl.actionsTop.toolBar && this.gridControl.actionsTop.toolBar.items) {
        const topItems = this.gridControl.actionsTop.toolBar.items;

        for (let i = 0; i < topItems.length; i++) {
            if (!topItems[i].setEnabled) {
                continue;
            }

            topItems[i].setEnabled(isEnabled);
        }
    }

    if (this.gridControl.actionsBottom && this.gridControl.actionsBottom.toolBar && this.gridControl.actionsBottom.toolBar.items) {
        const bottomItems = this.gridControl.actionsBottom.toolBar.items;

        for (let i = 0; i < bottomItems.length; i++) {
            if (!bottomItems[i].setEnabled) {
                continue;
            }

            bottomItems[i].setEnabled(isEnabled);
        }
    }
}

RecognizedValueMapper.prototype._handleTableSelectedRowsChanged = function (table) {
    const isTableSelected = table.selectedRows > 0;

    if (isTableSelected === true) {
        if (this._isRowMode() === false) {
            this.selectedTable = table;
            this._switchToRowMode();
        }
    }

    this.linesHint.setLinesCount(table.selectedRows);
}

RecognizedValueMapper.prototype._handleColumnUnselected = function (column) {
    const mappingByFieldRow = column.getMappingByFieldRow();
    const that = this;

    for (const key in mappingByFieldRow) {
        const item = mappingByFieldRow[key];

        const rowIndex = item.cell.row.getIndex();
        const row = this.gridControl.rows.getRow(rowIndex);
        const cellIndex = item.cell.getIndex();
        const cell = row.getCell(cellIndex);

        that.gridControl.editMode = false;

        if (cell.getReadOnly && cell.getReadOnly() === true) {
            cell.setReadOnly(false);
        }

        const value = null;
        let newValue = null;
        const editor = cell.getEditor();
        if (editor && editor.control && editor.control.setValue && editor.control.getValue) {
            editor.control.setValue(value);
            newValue = editor.control.getValue();
        }

        cell.setValue(newValue);
        cell.dataChanged = true;

        item.recognizedValues.forEach(function (rv) {
            rv.markAsNotMapped();
        });
        item.recognizedValues = [];

        that.gridControl.editMode = false;
        row.dataChanged = true;
        row.commitChanges();
        row.dataChanged = false;

        this._setGridActionsStateInColumnMode();
    }
}

RecognizedValueMapper.prototype._handleColumnSelected = function (table, column, event) {
    if (this.selectedRowIndex === null) {
        return false;
    }

    if (!this.gridControl.activeCell) {
        return false;
    }

    const appendColumn = event.ctrlKey === true || event.metaKey === true;
    const activeRow = this.gridControl.activeRow;

    const activeCellIndex = this.gridControl.activeCell.getIndex();

    if (appendColumn === false) {
        const mappedColumns = table.columns.filter(function (c) { return c.gridColumnIndex === activeCellIndex; });

        mappedColumns.forEach(function (c) {
            c.onUndoMousedown(false);
        });
    }

    const selectedCellInfos = table.getSelectedCellInfos(column);
    const selectedCellIndices = selectedCellInfos.map(function (info) { return info.index; });
    let row = null;
    let gridRowIndicesToUpdate = [];

    for (let i = 0; i < selectedCellIndices.length; i++) {
        const gridRowIndex = this.selectedRowIndex + i;

        let recognizedValues = [];
        this.recognizedValues.forEach(function (rv) {
            rv.fillCellInfo();

            if (rv.cellInfo.pageIndex === table.pageIndex &&
                rv.cellInfo.tableIndex === table.tableIndex &&
                rv.cellInfo.cellIndex === selectedCellIndices[i]) {
                recognizedValues.push(rv);
            }
        });

        // remove details duplicates
        const recognizedNonDetailValues = recognizedValues.filter(function (rv) { return rv.isDetailField !== true; });
        if (recognizedNonDetailValues.length > 0 && recognizedNonDetailValues.length < recognizedValues.length) {
            recognizedValues = recognizedNonDetailValues;
        }

        // remove word duplicates if contains table cell value
        const recognizedTableValues = recognizedValues.filter(function (rv) { return rv.wordInfo === null; });
        if (recognizedTableValues.length > 0) {
            recognizedValues = recognizedTableValues;
        }

        this.gridControl.editMode = false;
        gridRowIndicesToUpdate.push(gridRowIndex);

        const prevRow = row;
        row = this.gridControl.rows.getRow(gridRowIndex);
        if (!row) {
            if (prevRow) {
                prevRow.dataChanged = true;
            }

            row = this.gridControl.addNewRow();

            this._setGridActionsStateInColumnMode();

            if (prevRow) {
                prevRow.dataChanged = false;
            }

            if (this.gridControl.activeCell) {
                this.gridControl.activeCell.editor.hide();
            }

            if (!row) {
                continue;
            }

            row.cells.forEach(function (c) {
                c.setReadOnly(true);
            });
        }

        const cell = row.getCell(activeCellIndex);
        if (!cell) {
            continue;
        }
        if (cell.getReadOnly && cell.getReadOnly() === true) {
            cell.setReadOnly(false);
        }

        const that = this;
        const getMappingByControl = function () {
            return that._getGridMappingValuesByCell(cell);
        };
        const performMapping = function (rv, newValue, appendValue) {
            that._mapTableCellToGridCell(gridRowIndex, activeCellIndex, newValue, rv, appendValue, column);
        };

        for (let i = 0; i < recognizedValues.length; i++) {
            const rvToMap = recognizedValues[i];
            const appendValue = i > 0 || appendColumn;

            this._mapRecognizedValueToControl(cell, rvToMap, appendValue, getMappingByControl, performMapping);
        }
    }

    if (row) {
        this.gridControl.editMode = false;
        row.dataChanged = true;
        row.commitChanges();
        row.dataChanged = false;

        this._setGridActionsStateInColumnMode();
    }

    const that = this;
    gridRowIndicesToUpdate.forEach(function (i) {
        const row = that.gridControl.rows.getRow(i);

        if (row) {
            const status = row.getStatus();

            if (status === 0) { //PXRowStatus.NotSet
                row.setStatus(1); //PXRowStatus.Modified
            }
        }
    });

    if (activeRow) {
        activeRow.activate();
        activeRow.activateCell(activeCellIndex, true);
    }

    return true;
}

RecognizedValueMapper.prototype._setGridActionsStateInColumnMode = function () {
    if (this._isColumnMode() !== true) {
        return;
    }

    this._setGridActionsEnabled(false);
    this.exitTableDefiningButton.setEnabled(true);
    this._setMappingOptionsEnabled();
}

RecognizedValueMapper.prototype._clearColumnCellsMapping = function () {

}

RecognizedValueMapper.prototype._mapTableCellToGridCell = function (rowIndex, cellIndex, value, recognizedValue, appendValue, column) {
    let row = this.gridControl.rows.getRow(rowIndex);
    if (!row) {
        return;
    }

    const cell = row.getCell(cellIndex);
    if (!cell) {
        return;
    }

    let newValue = value;

    if (appendValue === true && cell.column.dataType === 18) { // string type
        newValue = cell.getValue() + ' ' + newValue;
    }

    const editor = cell.getEditor();
    // Parse value using editor's control
    if (editor && editor.control && editor.control.setValue && editor.control.getValue) {
        editor.control.setValue(value);
        newValue = editor.control.getValue();
    }

    cell.setValue(newValue);
    cell.dataChanged = true;

    this._correctGridMapping(cell, recognizedValue, appendValue, true, column);
}

RecognizedValueMapper.prototype.enrichValue = function (recognizedValue) {
    return this.recognizedValues.some(function (value) {
        if (recognizedValue.equals(value)) {
            if (value.rowIndex === null && recognizedValue.rowIndex !== null) {
                value.rowIndex = recognizedValue.rowIndex;
            }

            return true;
        }

        return false;
    });
}
