'use strict';

const columnClass = 'recognition-column';
const hiddenInColumnModeClass = 'hidden-in-column-mode';
const hoverClass = 'recognition-column-hover';
const hoverSelectedClass = 'selected-hover';
const undoClass = 'recognition-column-undoContainer';
const undoContainerSize = 11;
const undoContainerOffset = 5;

function RecognizedColumn(unit, pageWidth, pageHeight, table, columnIndex, containerWidth, containerHeight, svg) {
    const columnCells = table.cells.filter(function (cell) { return cell.columnIndex === columnIndex; });
    RecognizedRectangleLine.call(this, unit, pageWidth, pageHeight, columnCells, columnClass, containerWidth, containerHeight, svg);

    this.columnIndex = columnIndex;
    this.gridColumnIndex = null;
    this.firstSelectedCellIndex = null;
    this._gridMappingByFieldRow = [];

    this.hoverCounter = 0;
    this.undoContainer = null;

    this.onUndoMousedownCallback = null;
    this.onMouseenterCallback = null;
    this.onMouseleaveCallback = null;
    this._undoMouseenterCallback = null;
    this._undoMouseleaveCallback = null;

    const that = this;

    this.mouseenterCallback = function () { that.onMouseenter(); };
    this.subscribeOnMouseenter(this.mouseenterCallback);

    this.mouseleaveCallback = function () { that.onMouseleave(); };
    this.subscribeOnMouseleave(this.mouseleaveCallback);

    this._initUndoContainer();
}

RecognizedColumn.prototype = Object.create(RecognizedRectangleLine.prototype);
RecognizedColumn.prototype.constructor = RecognizedColumn;

RecognizedColumn.prototype.removeEventListeners = function () {
    this.unsubscribeOnMouseenter(this.mouseenterCallback);
    this.unsubscribeOnMouseleave(this.mouseleaveCallback);
    this.undoContainer.removeEventListener('mouseenter', this._undoMouseenterCallback);
    this.undoContainer.removeEventListener('mouseleave', this._undoMouseleaveCallback);
    this.undoContainer.removeEventListener('mousedown', this._undoMousedownCallback);
}

RecognizedColumn.prototype._initUndoContainer = function () {
    this.undoContainer = document.createElement('div');
    this.undoContainer.classList.add(undoClass);

    const undoElement = document.createElement('div');
    const undoText = document.createTextNode('╳');
    undoElement.appendChild(undoText);

    this.undoContainer.appendChild(undoElement);

    const that = this;

    this._undoMouseenterCallback = function () { that.onMouseenter(); };
    this.undoContainer.addEventListener('mouseenter', this._undoMouseenterCallback);

    this._undoMouseleaveCallback = function () { that.onMouseleave(); };
    this.undoContainer.addEventListener('mouseleave', this._undoMouseleaveCallback);

    this._undoMousedownCallback = function () { that.onUndoMousedown(); };
    this.undoContainer.addEventListener('mousedown', this._undoMousedownCallback);
}

RecognizedColumn.prototype._rescaleUndoContainer = function () {
    if (this.undoContainer === null || this.firstSelectedCellIndex === null) {
        return;
    }

    const firstRect = this.rectangles[this.firstSelectedCellIndex];
    const offset = RescaleUtils.isOffsetNeeded(firstRect) ? undoContainerOffset : 0;

    this.undoContainer.style.top = firstRect.top + 'px';
    this.undoContainer.style.left = (firstRect.left + firstRect.width - undoContainerSize - offset) + 'px';
    this.undoContainer.style.height = firstRect.height + 'px';
    this.undoContainer.style.fontSize = undoContainerSize + 'px';
}

RecognizedColumn.prototype._showUndoContainer = function (show) {
    this.undoContainer.style.display = show === true ? 'flex' : 'none';
}

RecognizedColumn.prototype.rescale = function (containerWidth, containerHeight, scale) {
    RecognizedRectangleLine.prototype.rescale.call(this, containerWidth, containerHeight, scale)
    this._rescaleUndoContainer();
}

RecognizedColumn.prototype.appendToParent = function (parentElement, parentSvg) {
    RecognizedRectangleLine.prototype.appendToParent.call(this, parentSvg);
    parentElement.appendChild(this.undoContainer);
}

RecognizedColumn.prototype.showInColumnMode = function () {
    this.removeClass(hiddenInColumnModeClass);
}

RecognizedColumn.prototype.hideInColumnMode = function () {
    this.addClass(hiddenInColumnModeClass);
}

RecognizedColumn.prototype.reset = function () {
    RecognizedRectangleLine.prototype.reset.call(this);
    this.setSelected(false);
    this.showInColumnMode();
    this.removeClass(hoverClass);
    this._showUndoContainer(false);
    this._clearGridMapping();
}

RecognizedColumn.prototype.onMouseenter = function (externalCall) {
    if (this.hasClass(hoverClass) || this.hasClass(hoverSelectedClass)) {
        return;
    }

    if (!externalCall && this.onMouseenterCallback !== null) {
        this.onMouseenterCallback(this);
    }

    if (this.getSelected() === true) {
        this.addClass(hoverSelectedClass);
        this._showUndoContainer(true);
    }
    else {
        this.addClass(hoverClass);
    }
}

RecognizedColumn.prototype.onMouseleave = function (externalCall) {
    if (!externalCall && this.onMouseleaveCallback !== null) {
        this.onMouseleaveCallback(this);
    }

    this.removeClass(hoverClass);
    this.removeClass(hoverSelectedClass);
    this._showUndoContainer(false);
}

RecognizedColumn.prototype.onUndoMousedown = function (externalCall) {
    if (!externalCall) {
        this.onUndoMousedownCallback(this);
    }

    this._showUndoContainer(false);
    this.setSelected(false);
    this._clearGridMapping();
}

RecognizedColumn.prototype.hideNotSelectedCells = function (selectedCells) {
    let first = true;

    for (let i = 0; i < this.cells.length; i++) {
        if (selectedCells.indexOf(this.cells[i]) !== -1) {
            if (first === true) {
                first = false;
                this.firstSelectedCellIndex = i;
            }
        }
        else {
            this.rectangles[i].addClass(hiddenInColumnModeClass);
        }
    }
}

RecognizedColumn.prototype.setSelected = function (isSelected) {
    RecognizedRectangleLine.prototype.setSelected.call(this, isSelected);
    this._rescaleUndoContainer();
}

RecognizedColumn.prototype.setGridMapping = function (gridColumnIndex, fieldRow, mapping) {
    this.gridColumnIndex = gridColumnIndex;
    this._gridMappingByFieldRow[fieldRow] = mapping;
}

RecognizedColumn.prototype.getMappingByFieldRow = function () {
    return this._gridMappingByFieldRow;
}

RecognizedColumn.prototype._clearGridMapping = function () {
    this._gridMappingByFieldRow = [];
    this.gridColumnIndex = null;
}