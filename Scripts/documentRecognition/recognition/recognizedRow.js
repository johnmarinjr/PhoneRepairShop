'use strict';

const rowClass = 'recognition-row';
const activeRowClass = 'active';
const rowCheckBoxContainerClass = 'recognition-row-checkboxContainer';
const rowCheckBoxClass = 'recognition-row-checkbox';
const hiddenInRowModesClass = 'hidden-in-row-mode';
const checkboxContainerOffset = 20;

function RecognizedRow(unit, pageWidth, pageHeight, table, rowIndex, containerWidth, containerHeight, selectedCallback, svg) {
    const rowCells = table.cells.filter(function (cell) { return cell.rowIndex === rowIndex; });
    RecognizedRectangleLine.call(this, unit, pageWidth, pageHeight, rowCells, rowClass, containerWidth, containerHeight, svg);

    this.rowIndex = rowIndex;
    this.checkbox = null;
    this.checkboxContainer = null;
    this.selectedCallback = selectedCallback;

    this._initCheckboxContainer();
}

RecognizedRow.prototype = Object.create(RecognizedRectangleLine.prototype);
RecognizedRow.prototype.constructor = RecognizedRow;

RecognizedRow.prototype._initCheckboxContainer = function () {
    this.checkboxContainer = document.createElement('div');
    this.checkboxContainer.classList.add(rowCheckBoxContainerClass);

    this.checkbox = document.createElement('input');
    this.checkbox.type = 'checkbox';
    this.checkbox.classList.add(rowCheckBoxClass);

    const that = this;
    this.checkbox.addEventListener('click', function (e) {
        that._handleCheckboxChange(e)
    });

    this.checkboxContainer.appendChild(this.checkbox);

    this.rescaleCheckboxContainer();
}

RecognizedRow.prototype._handleCheckboxChange = function (e) {
    if (this.checkbox.checked === true) {
        this.isSelected = true;
        this.markAsMapped();
    }
    else {
        this.isSelected = false;
        this.markAsNotMapped();
    }

    if (this.selectedCallback) {
        this.selectedCallback(this, e);
    }
}

RecognizedRow.prototype.setSelected = function (isSelected) {
    this.checkbox.checked = isSelected;
    RecognizedRectangleLine.prototype.setSelected.call(this, isSelected);
}

RecognizedRow.prototype.reset = function () {
    RecognizedRectangleLine.prototype.reset.call(this);
    this.setSelected(false);
    this.showInRowMode();
    this.setActive(false);
    this.allowSelect(true);
}

RecognizedRow.prototype.setActive = function (isActive) {
    if (isActive === true) {
        this.checkbox.classList.add(activeRowClass);
    }
    else {
        this.checkbox.classList.remove(activeRowClass);
    }
}

RecognizedRow.prototype.showInRowMode = function () {
    this.checkboxContainer.classList.remove(hiddenInRowModesClass);
}

RecognizedRow.prototype.hideInRowMode = function () {
    this.checkboxContainer.classList.add(hiddenInRowModesClass);
}

RecognizedRow.prototype.appendToParent = function (parentElement, parentSvg) {
    RecognizedRectangleLine.prototype.appendToParent.call(this, parentSvg);
    parentElement.appendChild(this.checkboxContainer);
}

RecognizedRow.prototype.rescale = function (containerWidth, containerHeight, scale) {
    RecognizedRectangleLine.prototype.rescale.call(this, containerWidth, containerHeight, scale)
    this.rescaleCheckboxContainer();
}

RecognizedRow.prototype.getCheckboxContainerLeft = function () {
    return parseInt(this.checkboxContainer.style.left);
}

RecognizedRow.prototype.rescaleCheckboxContainer = function () {
    if (this.checkboxContainer === null) {
        return;
    }

    const firstRect = this.rectangles[0];
    const offset = RescaleUtils.isOffsetNeeded(firstRect) ? checkboxContainerOffset : 0;

    this.checkboxContainer.style.top = firstRect.top + 'px';
    this.checkboxContainer.style.left = (parseInt(firstRect.left) - offset) + 'px';
    this.checkboxContainer.style.height = firstRect.height + 'px';
}

RecognizedRow.prototype.allowSelect = function (allow) {
    this.checkbox.disabled = !allow;
}