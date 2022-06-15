'use strict';

const valueClass = 'recognition-value';

function RecognizedValue(fieldInfo, recognizedPages, pagesInfo, wordInfo) {
    this.fieldName = null;
    this.rowIndex = null;
    this.isPrimaryField = false;
    this.isDetailField = false;

    this.recognizedPages = recognizedPages;
    this.pagesInfo = pagesInfo;
    this.wordInfo = wordInfo;

    this.rectangles = [];
    this.boundingBoxes = [];
    this.rectangleScrollIndex = 0;

    this.text = null;
    this.value = null;

    this.cellInfo = {
        rectangleIndex: 0,
        pageIndex: null,
        tableIndex: null,
        cellIndex: null,
        rowIndex: null,
        columnIndex: null,
        columnNumber: null,
        isSet: null
    };

    this.scrollRowIndex = null;
    this.scrollColumnNumber = null;

    let recognizedField = null;
    let searchTerm = null;
    if (fieldInfo !== null) {
        this.fieldName = fieldInfo.fieldName;
        this.rowIndex = fieldInfo.rowIndex;
        this.isPrimaryField = fieldInfo.isPrimaryField;

        recognizedField = fieldInfo.recognizedField;
        searchTerm = fieldInfo.searchTerm;

        if (this.rowIndex !== null) {
            this.isDetailField = true;
        }
    }

    let ocr = null;
    if (recognizedField !== null) {
        this.value = recognizedField.value;
        ocr = recognizedField.ocr;
    }

    this._initText(ocr, wordInfo, searchTerm);
    this._initBoundingBoxes(ocr, wordInfo, searchTerm);
}

RecognizedValue.prototype.addSearchTerm = function (searchTerm) {
    if (!searchTerm) {
        return;
    }

    if (this.text === null && searchTerm.text) {
        this.text = searchTerm.text;
    }

    if (this.boundingBoxes.length === 0) {
        this._initBoundingBoxes(null, null, searchTerm);
        this.appendToPages();
    }
}

RecognizedValue.prototype.equals = function (otherValue) {
    if (otherValue.value !== this.value) {
        return false;
    }

    if (otherValue.text !== this.text) {
        return false;
    }

    if (otherValue.rectangles.length !== this.rectangles.length) {
        return false;
    }

    const that = this;

    return otherValue.rectangles.every(function (otherRect) {
        return that.rectangles.some(function (thisRect) {
            return thisRect.equals(otherRect);
        });
    });
}

RecognizedValue.prototype._initText = function (ocr, wordInfo, searchTerm) {
    if (ocr && ocr.text) {
        this.text = ocr.text;
    }
    else if (wordInfo && wordInfo.word && wordInfo.word.text) {
        this.text = wordInfo.word.text;
    }
    else if (searchTerm && searchTerm.text) {
        this.text = searchTerm.text;
    }
}

RecognizedValue.prototype._initBoundingBoxes = function (ocr, wordInfo, searchTerm) {
    if (ocr && ocr.boundingBoxes) {
        this._initOcrBoundingBoxes(ocr.boundingBoxes);
    }
    else if (wordInfo && wordInfo.word && wordInfo.wordIndex != null) {
        this._initWordBoundingBox(wordInfo.word, wordInfo.wordIndex, wordInfo.pageIndex);
    }
    else if (searchTerm) {
        this._initOcrBoundingBoxes(searchTerm.boundingBoxes);
    }
}

RecognizedValue.prototype._initOcrBoundingBoxes = function (boundingBoxes) {
    const that = this;

    boundingBoxes.forEach(function (box) {
        const page = that.recognizedPages[box.page];
        const container = that.pagesInfo[box.page].canvas;
        const svg = that.pagesInfo[box.page].svg;

        let coordinates = null;

        if (box.word != null) {
            let word = page.words[box.word];

            coordinates = word.boundingBox;
        }
        else if (box.keyValuePair != null) {
            let keyValuePair = page.keyValuePairs[box.keyValuePair];

            coordinates = keyValuePair.value.boundingBox;
        }
        else if (box.table != null && box.cell != null) {
            let table = page.tables[box.table];
            let cell = table.cells[box.cell];

            coordinates = cell.boundingBox;
        }

        if (coordinates !== null) {
            that._addRectangle(page.unit, page.width, page.height, coordinates, container.width, container.height, svg);
            that.boundingBoxes.push(box);
        }
    });
}

RecognizedValue.prototype._initWordBoundingBox = function (word, wordIndex, pageIndex) {
    let coordinates = word.boundingBox;
    if (coordinates === null || !coordinates.length || coordinates.length !== 4) {
        return;
    }

    const container = this.pagesInfo[pageIndex].canvas;
    const svg = this.pagesInfo[pageIndex].svg;
    const page = this.recognizedPages[pageIndex];

    this._addRectangle(page.unit, page.width, page.height, coordinates, container.width, container.height, svg);

    let boundingBox = {
        page: pageIndex,
        word: wordIndex
    };

    this.boundingBoxes.push(boundingBox);
}

RecognizedValue.prototype._addRectangle = function (unit, pageWidth, pageHeight, coordinates, containerWidth, containerHeight, svg) {
    const rect = new RecognizedRectangle(unit, pageWidth, pageHeight, coordinates, containerWidth, containerHeight, svg);

    rect.polygon.classList.add(valueClass);
    this.rectangles.push(rect);
}

RecognizedValue.prototype.subscribeOnMousedown = function (callback) {
    const that = this;

    this.rectangles.forEach(function (rect) {
        rect.subscribeOnMousedown(function (r, event) {
            callback(that, event);
        });
    })
}

RecognizedValue.prototype.appendToPages = function () {
    for (let i = 0; i < this.rectangles.length; i++) {
        let rect = this.rectangles[i];
        let page = this.boundingBoxes[i].page;
        let parent = this.pagesInfo[page].svg;

        rect.appendToParent(parent);
    }
}

RecognizedValue.prototype.markAsMapped = function () {
    this.rectangles.forEach(function (rect) {
        rect.markAsMapped();
    });
}

RecognizedValue.prototype.markAsNotMapped = function () {
    this.rectangles.forEach(function (rect) {
        rect.markAsNotMapped();
    });
}

RecognizedValue.prototype.rescale = function (scale) {
    for (let i = 0; i < this.rectangles.length; i++) {
        let rect = this.rectangles[i];
        let page = this.boundingBoxes[i].page;
        let container = this.pagesInfo[page].canvas;

        rect.rescale(container.width, container.height, scale);
    }
}

RecognizedValue.prototype.getScrollTarget = function () {
    if (this.rectangles.length === 0) {
        return null;
    }

    const firstRectangle = this.rectangles[this.cellInfo.rectangleIndex];
    const rectangleElement = firstRectangle.polygon;

    return rectangleElement;
}

RecognizedValue.prototype.fillCellInfo = function () {
    if (this.cellInfo.isSet === true) {
        return;
    }

    for (let b = 0; b < this.boundingBoxes.length; b++) {
        const box = this.boundingBoxes[b];
        const page = this.recognizedPages[box.page];

        if (box.table != null && box.cell != null) {
            const table = page.tables[box.table];
            const cell = table.cells[box.cell];

            this.addCellInfo(box.page, box.table, box.cell, cell.rowIndex, cell.columnIndex, table.columnNumber);
            return;
        }

        if (box.word != null) {
            for (let t = 0; t < page.tables.length; t++) {
                const table = page.tables[t];

                for (let c = 0; c < table.cells.length; c++) {
                    const cell = table.cells[c];
                    if (!cell.ocr || !cell.ocr.boundingBoxes) {
                        continue;
                    }

                    for (let ob = 0; ob < cell.ocr.boundingBoxes.length; ob++) {
                        const ocrBox = cell.ocr.boundingBoxes[ob];

                        if (ocrBox.page === box.page && ocrBox.word === box.word) {
                            this.addCellInfo(box.page, t, c, cell.rowIndex, cell.columnIndex, table.columnNumber);
                            return;
                        }
                    }
                }
            }
        }
    }
}

RecognizedValue.prototype.addCellInfo = function (pageIndex, tableIndex, cellIndex, rowIndex, columnIndex, columnNumber) {
    this.cellInfo.pageIndex = pageIndex;
    this.cellInfo.tableIndex = tableIndex;
    this.cellInfo.cellIndex = cellIndex;
    this.cellInfo.rowIndex = rowIndex;
    this.cellInfo.columnIndex = columnIndex;
    this.cellInfo.columnNumber = columnNumber;
    this.cellInfo.isSet = true;
}
