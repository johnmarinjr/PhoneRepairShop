'use strict';

function RecognizedTable(unit, pageWidth, pageHeight, pageIndex, tableIndex, table, containerWidth, containerHeight, svg) {
    this.pageIndex = pageIndex;
    this.tableIndex = tableIndex;
    this.rows = [];
    this.columns = [];
    this.cells = table.cells;
    this.currentRow = null;
    this.maxRowsToSelect = null;
    this.selectedRows = 0;
    this.selectedRowsChangedCallback = null;
    this.columnSelectedCallback = null;
    this.columnUnselectedCallback = null;

    this._initRows(unit, pageWidth, pageHeight, table, containerWidth, containerHeight, svg);
    this._initColumns(unit, pageWidth, pageHeight, table, containerWidth, containerHeight, svg);
}

RecognizedTable.prototype.removeEventListeners = function () {
    this.columns.forEach(function (c) {
        c.removeEventListeners();
    });
}

RecognizedTable.prototype._initColumns = function (unit, pageWidth, pageHeight, table, containerWidth, containerHeight, svg) {
    const that = this;

    for (let i = 0; i < table.columnNumber; i++) {
        const undoMousedownCallback = function (column) { that._handleColumnUndoMousedown(column); };
        const mouseEnterCallback = function (column) { that._handleColumnMouseenter(column); };
        const mouseLeaveCallback = function (column) { that._handleColumnMouseleave(column); };
        const column = new RecognizedColumn(unit, pageWidth, pageHeight, table, i, containerWidth, containerHeight, svg);
        column.onUndoMousedownCallback = undoMousedownCallback;
        column.onMouseenterCallback = mouseEnterCallback;
        column.onMouseleaveCallback = mouseLeaveCallback;
        const mousedownCallback = function (column, event) { that._handleColumnSelected(column, event); };
        column.subscribeOnMousedown(mousedownCallback);

        this.columns.push(column);
    }
}

RecognizedTable.prototype.showSelectedColumnCells = function () {
    const that = this;

    this.columns.forEach(function (c) {
        const selectedCellInfos = that.getSelectedCellInfos(c);
        const selectedCells = selectedCellInfos.map(function (info) { return info.cell; });

        c.hideNotSelectedCells(selectedCells);
    });
}

RecognizedTable.prototype._handleColumnSelected = function (column, event) {
    if (this.columnSelectedCallback !== null) {
        if (column.getSelected() === true) {
            return;
        }

        if (this.columnSelectedCallback(column, event) === true) {
            column.setSelected(true);
        }
    }
}

RecognizedTable.prototype._handleColumnMouseleave = function (column) {
    this.columns.forEach(function (c) {
        if (column.gridColumnIndex !== null && c.gridColumnIndex === column.gridColumnIndex && c !== column) {
            c.onMouseleave(true);
        }
    });
}

RecognizedTable.prototype._handleColumnMouseenter = function (column) {
    this.columns.forEach(function (c) {
        if (column.gridColumnIndex !== null && c.gridColumnIndex === column.gridColumnIndex && c !== column) {
            c.onMouseenter(true);
        }
    });
}

RecognizedTable.prototype._handleColumnUndoMousedown = function (column) {
    if (this.columnUnselectedCallback !== null) {
        this.columnUnselectedCallback(column);
    }

    this.columns.forEach(function (c) {
        if (column.gridColumnIndex !== null && c.gridColumnIndex === column.gridColumnIndex && c !== column) {
            c.onUndoMousedown(true);
        }
    });
}

RecognizedTable.prototype._initRows = function (unit, pageWidth, pageHeight, table, containerWidth, containerHeight, svg) {
    const that = this;
    const selectedCallback = function (row, e) { that._handleUpdateSelected(row, e); };

    for (let i = 0; i < table.rowNumber; i++) {
        const row = new RecognizedRow(unit, pageWidth, pageHeight, table, i, containerWidth, containerHeight, selectedCallback, svg);

        this.rows.push(row);
    }
}

RecognizedTable.prototype._handleUpdateSelected = function (row, e) {
    const multiSelect = e.shiftKey === true && this.currentRow !== null;

    if (multiSelect === true) {
        this._multipleRowsSelected(row);
    }
    else {
        this._singleRowSelected(row);
    }

    this._setCurentRow(row);

    if (this.selectedRowsChangedCallback !== null) {
        this.selectedRowsChangedCallback(this);
    }
}

RecognizedTable.prototype._setCurentRow = function (row) {
    if (this.currentRow !== null) {
        this.currentRow.setActive(false);
    }

    this.currentRow = row;
    this.currentRow.setActive(true);
}

RecognizedTable.prototype._multipleRowsSelected = function (row) {
    const isSelected = row.getSelected() === true;
    row.setSelected(!isSelected);

    const currentRowIndex = this.rows.indexOf(this.currentRow);
    const rowIndex = this.rows.indexOf(row);

    const currentRowOffset = this.currentRow.getSelected() === isSelected ? 1 : 0;
    const firstRowIndex = currentRowIndex < rowIndex ? currentRowIndex + currentRowOffset : rowIndex;
    const lastRowIndex = currentRowIndex > rowIndex ? currentRowIndex - currentRowOffset : rowIndex;

    for (let i = firstRowIndex; i <= lastRowIndex; i++) {
        if (this.rows[i].getSelected() === isSelected && this.rows[i] !== row) {
            continue;
        }

        this.rows[i].setSelected(isSelected);
        this._singleRowSelected(this.rows[i], true);

        if (this.maxRowsToSelect !== null && this.maxRowsToSelect() === this.selectedRows) {
            break;
        }
    }
}

RecognizedTable.prototype._singleRowSelected = function (row) {
    if (row.getSelected() === true) {
        this.selectedRows++;
    }
    else {
        this.selectedRows--;
    }
}

RecognizedTable.prototype.appendToParent = function (parentElement, parentSvg) {
    this.rows.forEach(function (row) {
        row.appendToParent(parentElement, parentSvg);
    });

    this.columns.forEach(function (column) {
        column.appendToParent(parentElement, parentSvg);
    });
}

RecognizedTable.prototype.rescale = function (containerWidth, containerHeight, scale) {
    this.rows.forEach(function (row) {
        row.rescale(containerWidth, containerHeight, scale);
    });

    this.columns.forEach(function (column) {
        column.rescale(containerWidth, containerHeight, scale);
    });
}

RecognizedTable.prototype.getCheckboxMinX = function () {
    if (this.rows.length == 0) {
        return null;
    }

    let minLeft = this.rows[0].getCheckboxContainerLeft();

    for (let i = 1; i < this.rows.length; i++) {
        const left = this.rows[i].getCheckboxContainerLeft();

        if (left < minLeft) {
            minLeft = left;
        }
    }

    return minLeft;
}

RecognizedTable.prototype.hideRowsInRowMode = function () {
    this.rows.forEach(function (row) {
        row.hideInRowMode();
    });
}

RecognizedTable.prototype.hideColumnsInColumnMode = function () {
    this.columns.forEach(function (column) {
        column.hideInColumnMode();
    });
}

RecognizedTable.prototype.reset = function () {
    this.selectedRows = 0;

    this.rows.forEach(function (row) {
        row.reset();
    });

    this.columns.forEach(function (column) {
        column.reset();
    });
}

RecognizedTable.prototype.subscribeOnColumnSelected = function (callback) {
    const that = this;

    this.columns.forEach(function (column) {
        column.subscribeOnMousedown(function (c) {
            callback(that, c);
        });
    });
}

RecognizedTable.prototype.getSelectedCellInfos = function (column) {
    const selectedRows = this.rows.filter(function (row) { return row.getSelected() === true; });
    const selectedCellInfos = [];

    for (let i = 0; i < this.cells.length; i++) {
        const cell = this.cells[i];
        if (cell.columnIndex !== column.columnIndex) {
            continue;
        }

        const rowsContainCell = selectedRows.some(function (row) { return cell.rowIndex === row.rowIndex; });
        if (rowsContainCell === false) {
            continue;
        }

        const cellInfo = {
            cell: cell,
            index: i
        };
        selectedCellInfos.push(cellInfo);
    }

    return selectedCellInfos;
}

RecognizedTable.prototype.activateRows = function (rowIndices) {
    this.rows.forEach(function (r) {
        if (rowIndices.indexOf(r.rowIndex) != -1) {
            r.isSelected = true;
        }
    });
}

RecognizedTable.prototype.activateColumns = function (columnInfoSet) {
    this.columns.forEach(function (c) {
        if (columnInfoSet.has(c.columnIndex) && c.getSelected() === false) {
            const mappingInfo = columnInfoSet.get(c.columnIndex);

            mappingInfo.mappings.forEach(function (m) {
                c.setGridMapping(mappingInfo.gridColumnIndex, m.fieldRow, m.mapping);
            });

            c.setSelected(true);
        }
    });
}

RecognizedTable.prototype.allowSelectMoreRows = function (allow) {
    this.rows.forEach(function (r) {
        if (r.getSelected() === false) {
            r.allowSelect(allow);
        }
    });
}