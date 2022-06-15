'use strict';

const pageClass = 'pdf-viewer-page';
const canvasClass = 'pdf-viewer-canvas'
const svgClass = 'pdf-viewer-svg';

function PdfRecognitionViewer(fileUrl, pdfjsLib, container, fieldBoundFeedbackControl, tableRelatedFeedbackControl, dumpTableFeedbackCallback,
    vendorFieldName, vendorControlFieldName, poNumberFieldName, poNumberJsonFieldName, linesHintSingleLine, linesHintMultipleLines,
    linesHintSelectTextPrefix, linesHintSelectTextSingleLine, linesHintSelectTextMultipleLines,
    linesHintButonText, columnsExcludedFromColumnMapping) {
    this.container = container;
    this.fileUrl = fileUrl;
    this.pdfjsLib = pdfjsLib;
    this.pageRendering = false;
    this.vendorTermData = null;
    this.vendorFieldName = vendorFieldName;
    this.vendorControlFieldName = vendorControlFieldName;
    this.recognizedData = null;
    this.recognizedPages = null;
    this.recognizedDocument = null;
    this.viewerContainer = null;
    this.pageInfo = [];
    this.scaleSpan = null;
    this.currentPageNum = 1;
    this.scale = 1.0;
    this.scaleStep = 0.1;
    this.minScale = this.scaleStep;
    this.maxScale = this.scaleStep * 100;
    this.scrolling = false;
    this.pageBoundingClientRectsOnScrolling = [];
    this.pageNumPending = [];
    this.currentPageSpan = null;
    this.pagesCountSpan = null;
    this._svgScale1 = null;

    this.createChildControls(container);

    this.recognizedValueMapper = new RecognizedValueMapper(this.viewerContainer, fieldBoundFeedbackControl, tableRelatedFeedbackControl,
        this.vendorFieldName, dumpTableFeedbackCallback, poNumberFieldName, poNumberJsonFieldName,
        linesHintSingleLine, linesHintMultipleLines,
        linesHintSelectTextPrefix, linesHintSelectTextSingleLine, linesHintSelectTextMultipleLines,
        linesHintButonText, columnsExcludedFromColumnMapping);
}

PdfRecognitionViewer.prototype.exitTableMode = function () {
    this.recognizedValueMapper.handleExitTableDefining(true);
}

PdfRecognitionViewer.prototype.removeEventListeners = function () {
    this.recognizedValueMapper.removeEventListeners();
}

PdfRecognitionViewer.prototype.clearDetailsMapping = function () {
    this.recognizedValueMapper.clearDetailsMapping();
}

PdfRecognitionViewer.prototype.addVendorSearchTerm = function (vendorTermIndex) {
    const vendorTerm = this._getVendorTerm(vendorTermIndex);
    this.recognizedValueMapper.addVendorSearchTerm(vendorTerm, this.vendorControlFieldName, this.vendorControlFieldName);
}

PdfRecognitionViewer.prototype._getVendorTerm = function (vendorTermIndex) {
    if (!vendorTermIndex) {
        return null;
    }

    const vendorField = this.recognizedDocument.fields[this.vendorFieldName];

    if (vendorField && vendorField.fullTextTerms && vendorTermIndex < vendorField.fullTextTerms.length) {
        return vendorField.fullTextTerms[vendorTermIndex];
    }

    return null;
}

PdfRecognitionViewer.prototype.processRecognizedData = function (recognizedData, vendorTermIndex, isReloading) {
    if (isReloading === false) {
        this._extractRecognizedData(recognizedData);
    }
    else {
        this.recognizedValueMapper.clear();

        this.pageInfo.forEach(function (pi) {
            const element = pi.container;
            const rectangles = element.getElementsByClassName(rectClass);

            for (let i = 0; i < rectangles.length; i++) {
                element.removeChild(rectangles[i]);
            }
        });
    }

    this.vendorTermData = this._getVendorTerm(vendorTermIndex);
    this._createRecognizedValues();
}

PdfRecognitionViewer.prototype._extractRecognizedData = function (recognizedData) {
    if (!recognizedData) {
        return;
    }

    this.recognizedData = recognizedData;

    if (recognizedData.documents && recognizedData.documents.length && recognizedData.documents.length > 0) {
        this.recognizedDocument = recognizedData.documents[0];
    }

    if (recognizedData.pages && recognizedData.pages.length && recognizedData.pages.length > 0) {
        this.recognizedPages = recognizedData.pages;
        this.recognizedValueMapper.feedbackCollector.fillPageKeyValuePairByWord(recognizedData.pages);
    }
}

PdfRecognitionViewer.prototype.trackFormControls = function (form, controlsInfo) {
    this.recognizedValueMapper.trackFormControls(form, controlsInfo);
}

PdfRecognitionViewer.prototype.trackGridControls = function (grid, exitTableModeButtonKey, mappingOptionsButtonKey, updateColumnMappingMenuIndex,
    addColumnsMenuIndex) {
    this.recognizedValueMapper.trackGridControls(grid, exitTableModeButtonKey, mappingOptionsButtonKey, updateColumnMappingMenuIndex,
        addColumnsMenuIndex);
}

PdfRecognitionViewer.prototype.createChildControls = function (container) {
    this.createToolbarControls(container);
    this.createViewerControls(container);
}

PdfRecognitionViewer.prototype.createToolbarControls = function (container) {
    this.toolbarContainer = document.createElement('div');

    let that = this;
    const buttonClassName = 'pdf-viewer-button';

    let prevButton = document.createElement('button');
    prevButton.type = 'button';
    prevButton.className = buttonClassName;
    let prevButtonText = document.createTextNode('<');
    prevButton.appendChild(prevButtonText);
    prevButton.addEventListener("click", function () { that.onPrevPage(that) });
    this.toolbarContainer.appendChild(prevButton);

    let nextButton = document.createElement('button');
    nextButton.type = 'button';
    nextButton.className = buttonClassName;
    let nextButtonText = document.createTextNode('>');
    nextButton.appendChild(nextButtonText);

    nextButton.addEventListener("click", function () { that.onNextPage(that) });
    this.toolbarContainer.appendChild(nextButton);

    let pagesInfoSpan = document.createElement('span');
    this.currentPageSpan = document.createElement('span');
    let defaultPageNum = document.createTextNode('1');
    this.currentPageSpan.appendChild(defaultPageNum);
    this.pagesCountSpan = document.createElement('span');
    pagesInfoSpan.appendChild(this.currentPageSpan);
    let pagesInfoSeparator = document.createTextNode(' / ');
    pagesInfoSpan.appendChild(pagesInfoSeparator);
    pagesInfoSpan.appendChild(this.pagesCountSpan);
    this.toolbarContainer.appendChild(pagesInfoSpan);

    let zoomOutButton = document.createElement('button');
    zoomOutButton.type = 'button';
    zoomOutButton.className = buttonClassName;
    let zoomOutButtonText = document.createTextNode('-');
    zoomOutButton.appendChild(zoomOutButtonText);
    zoomOutButton.addEventListener("click", function () { that.onZoomOut(that) });
    this.toolbarContainer.appendChild(zoomOutButton);

    let zoomInButton = document.createElement('button');
    zoomInButton.type = 'button';
    zoomInButton.className = buttonClassName;
    let zoomInButtonText = document.createTextNode('+');
    zoomInButton.appendChild(zoomInButtonText);
    zoomInButton.addEventListener("click", function () { that.onZoomIn(that) });
    this.toolbarContainer.appendChild(zoomInButton);

    let scaleInPercentSpan = document.createElement('span');
    this.scaleSpan = document.createElement('span');
    scaleInPercentSpan.appendChild(this.scaleSpan);
    let percentChar = document.createTextNode('%');
    scaleInPercentSpan.appendChild(percentChar);
    this.toolbarContainer.appendChild(scaleInPercentSpan);

    container.appendChild(this.toolbarContainer);
}

PdfRecognitionViewer.prototype.createViewerControls = function (container) {
    this.viewerContainer = document.createElement('div');
    this.viewerContainer.className = 'pdf-viewer-container';

    let that = this;
    this.viewerContainer.addEventListener('scroll', function (e) { that.onPageScroll(e); })

    container.appendChild(this.viewerContainer);
}

PdfRecognitionViewer.prototype.createPageControls = function (page) {
    let pageContainer = document.createElement('div');
    pageContainer.id = 'pdfPage' + page.pageNumber;
    pageContainer.className = pageClass;

    let canvas = document.createElement('canvas');
    canvas.className = canvasClass;
    let canvasContext = canvas.getContext('2d');
    pageContainer.appendChild(canvas);

    let svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('preserveAspectRatio', 'none');
    svg.classList.add(svgClass);
    pageContainer.appendChild(svg);

    this.viewerContainer.appendChild(pageContainer);
    this.pageInfo[page.pageNumber - 1] = {
        pdfPage: page,
        container: pageContainer,
        canvas: canvas,
        canvasContext: canvasContext,
        svg: svg
    };
}

PdfRecognitionViewer.prototype.onPageScroll = function (e) {
    for (let i = 0; i < this.pageInfo.length; i++) {
        this.pageBoundingClientRectsOnScrolling[i] = this.pageInfo[i].container.getBoundingClientRect();
    }

    if (!this.scrolling) {
        let that = this;

        window.requestAnimationFrame(function () {
            that.trackCurrentPageNum();
            that.scrolling = false;
        });

        this.scrolling = true;
    }
}

PdfRecognitionViewer.prototype.trackCurrentPageNum = function () {
    let newCurrentPageNum = this.currentPageNum;
    let viewerRect = this.viewerContainer.getBoundingClientRect();
    let maxVisiblePageHeight = 0;

    for (let i = 0; i < this.pageBoundingClientRectsOnScrolling.length; i++) {
        let pageRect = this.pageBoundingClientRectsOnScrolling[i];

        if (pageRect.top > viewerRect.bottom || pageRect.bottom < viewerRect.top) {
            continue;
        }

        let pageTop = pageRect.top <= viewerRect.top ? viewerRect.top : pageRect.top;
        let pageBottom = pageRect.bottom >= viewerRect.bottom ? viewerRect.bottom : pageRect.bottom;
        let visibleHeight = pageBottom - pageTop;

        if (visibleHeight > maxVisiblePageHeight) {
            maxVisiblePageHeight = visibleHeight;
            newCurrentPageNum = i + 1;
        }
    }

    if (newCurrentPageNum !== this.currentPageNum) {
        this.currentPageNum = newCurrentPageNum;
        this.currentPageSpan.textContent = this.currentPageNum;
    }

    // For IE
    if (this.currentPageSpan.textContent != this.currentPageNum) {
        this.currentPageSpan.textContent = this.currentPageNum;
    }
}

PdfRecognitionViewer.prototype.renderPdf = function (callback) {
    if (!this.fileUrl) {
        return;
    }

    let that = this;

    this.pdfjsLib.getDocument(this.fileUrl).promise.then(function (pdfDoc) {
        that.pagesCountSpan.textContent = pdfDoc.numPages;

        let initialScale = null;

        for (let i = 1; i <= pdfDoc.numPages; i++) {
            let iCaptured = i;

            pdfDoc.getPage(i).then(function (page) {
                if (initialScale === null) {
                    initialScale = that._computeScale(page);
                    that.scale = initialScale;
                    that.updateScaleText();
                }

                that.createPageControls(page);
                that.renderPage(iCaptured);

                if (iCaptured === pdfDoc.numPages) {
                    if (callback) {
                        callback();
                    }
                    //that.recognizedValueMapper._updateFeedback(true);
                }
            });
        }
    });

    window.addEventListener('resize', function () {
        that._resize();
    });
};

PdfRecognitionViewer.prototype._computeScale = function (page) {
    let view = page.view;
    let width = view[2];
    let height = view[3];

    let scrollbarWidth = 20; // Use constant as there is no scrollbar at this point
    let containerWidth = this.viewerContainer.clientWidth - scrollbarWidth;

    let scrollbarHeight = 20; // Use constant as there is no scrollbar at this point
    let containerHeight = this.viewerContainer.clientHeight - scrollbarHeight;

    let widthScale = containerWidth / width;
    let heightScale = containerHeight / height;

    return Math.max(widthScale, heightScale);
}

PdfRecognitionViewer.prototype._resize = function () {
    var firstPage = this.pageInfo[0];
    if (!firstPage) {
        return;
    }

    this.scale = this._computeScale(firstPage.pdfPage);
    this.rescalePages();
}

PdfRecognitionViewer.prototype._createRecognizedValues = function () {
    const pagesInfo = this.pageInfo.map(function (pi) {
        return {
            container: pi.container,
            canvas: pi.canvas,
            svg: pi.svg
        };
    });

    this._createPrimaryMappedRecognizedValues(pagesInfo);
    this._createDetailMappedRecognizedValues(pagesInfo);
    this._createNotMappedRecognizedValues(pagesInfo);
    this._createTables(pagesInfo);
    this.recognizedValueMapper.scroller.createCellScrollingIndex(this.recognizedPages, this.recognizedValueMapper.recognizedValues);
}

PdfRecognitionViewer.prototype._createPrimaryMappedRecognizedValues = function (pagesInfo) {
    if (!this.recognizedDocument || !this.recognizedDocument.fields) {
        return;
    }

    for (let fieldName in this.recognizedDocument.fields) {
        let field = this.recognizedDocument.fields[fieldName];

        if (field === null) {
            continue;
        }

        this._createFieldRecognizedValue(field, fieldName, pagesInfo, null, false);
    }
}

PdfRecognitionViewer.prototype._createTables = function (pagesInfo) {
    if (!this.recognizedPages) {
        return;
    }
    
    for (let pageIndex = 0; pageIndex < this.recognizedPages.length; pageIndex++) {
        const page = this.recognizedPages[pageIndex];
        const canvas = pagesInfo[pageIndex].canvas;
        const container = pagesInfo[pageIndex].container;
        const svg = pagesInfo[pageIndex].svg;

        if (page.tables === null || !page.tables.length) {
            continue;
        }

        for (let i = 0; i < page.tables.length; i++) {
            const table = page.tables[i];
            const recognizedTable = new RecognizedTable(page.unit, page.width, page.height, pageIndex, i, table, canvas.width, canvas.height, svg);

            recognizedTable.appendToParent(container, svg);
            this.recognizedValueMapper.trackRecognizedTable(recognizedTable);
        }

        this._setPageMarginLeft(container);
    }
}

PdfRecognitionViewer.prototype._createDetailMappedRecognizedValues = function (pagesInfo) {
    if (!this.recognizedDocument || !this.recognizedDocument.details || !this.recognizedDocument.details.Transactions ||
        !this.recognizedDocument.details.Transactions.value || !this.recognizedDocument.details.Transactions.value.length) {
        return;
    }

    const details = this.recognizedDocument.details.Transactions.value;
    const that = this;
    let rowIndex = 0;

    for (let i = 0; i < details.length; i++) {
        let d = details[i];

        if (!d.fields) {
            continue;
        }

        let isFieldCreated = false;

        for (let fieldName in d.fields) {
            let field = d.fields[fieldName];

            that._createFieldRecognizedValue(field, fieldName, pagesInfo, rowIndex, false);

            isFieldCreated = true;
        }

        if (isFieldCreated === true) {
            rowIndex++;
        }
    }
}

PdfRecognitionViewer.prototype._addRecognizedValue = function (recognizedValue) {
    if (this.recognizedValueMapper.enrichValue(recognizedValue) === true) {
        return;
    }

    recognizedValue.appendToPages();
    this.recognizedValueMapper.trackRecognizedValue(recognizedValue);
}

PdfRecognitionViewer.prototype._createNotMappedRecognizedValues = function (pagesInfo) {
    if (!this.recognizedPages) {
        return;
    }
    
    for (let pageIndex = 0; pageIndex < this.recognizedPages.length; pageIndex++) {
        let page = this.recognizedPages[pageIndex];

        if (page.tables && page.tables.length) {
            this._createCellNotMappedRecognizedValues(pagesInfo, page, pageIndex);
        }

        if (page.words && page.words.length) {
            this._createWordNotMappedRecognizedValues(pagesInfo, page, pageIndex);
        }
    }
}

PdfRecognitionViewer.prototype._createCellNotMappedRecognizedValues = function (pagesInfo, page, pageIndex) {
    for (let tableIndex = 0; tableIndex < page.tables.length; tableIndex++) {
        let table = page.tables[tableIndex];
        if (!table.cells || !table.cells.length) {
            continue;
        }

        for (let cellIndex = 0; cellIndex < table.cells.length; cellIndex++) {
            let cell = table.cells[cellIndex];
            if (!cell || !cell.ocr) {
                continue;
            }

            const ocrCopy = JSON.parse(JSON.stringify(cell.ocr));
            let field = {
                ocr: ocrCopy,
                value: ocrCopy.text
            };

            field.ocr.boundingBoxes = [
                {
                    page: pageIndex,
                    word: null,
                    keyValuePair: null,
                    table: tableIndex,
                    cell: cellIndex
                }
            ]

            this._createFieldRecognizedValue(field, null, pagesInfo, null, true);
        }
    }
}

PdfRecognitionViewer.prototype._createWordNotMappedRecognizedValues = function (pagesInfo, page, pageIndex) {
    for (let wordIndex = 0; wordIndex < page.words.length; wordIndex++) {
        let word = page.words[wordIndex];
        let wordInfo = {
            word: word,
            wordIndex: wordIndex,
            pageIndex: pageIndex
        };
        let recognizedValue = new RecognizedValue(null, this.recognizedPages, pagesInfo, wordInfo);

        this._addRecognizedValue(recognizedValue);
    }
}

PdfRecognitionViewer.prototype._createFieldRecognizedValue = function (field, fieldName, pagesInfo, rowIndex, isCell) {
    const searchTerm = fieldName === this.vendorFieldName ? this.vendorTermData : null;
    const fieldInfo = {
        recognizedField: field,
        fieldName: fieldName,
        rowIndex: rowIndex,
        isPrimaryField: rowIndex === null && isCell === false,
        searchTerm: searchTerm
    };
    const recognizedValue = new RecognizedValue(fieldInfo, this.recognizedPages, pagesInfo, null);

    this._addRecognizedValue(recognizedValue);
}

PdfRecognitionViewer.prototype._setPageMarginLeft = function (pageContainer) {
    let marginLeft = 0;

    this.recognizedValueMapper.recognizedTables.forEach(function (rt) {
        const minX = rt.getCheckboxMinX();

        if (minX !== null && minX < marginLeft) {
            marginLeft = minX;
        }
    });

    if (marginLeft < 0) {
        pageContainer.style.marginLeft = (-1 * marginLeft) + 'px';
    }
    else {
        pageContainer.style.marginLeft = '';
    }
}

PdfRecognitionViewer.prototype.renderPage = function (num) {
    this.pageRendering = true;

    let pageInfo = this.pageInfo[num - 1];
    let page = pageInfo.pdfPage;
    let viewport = page.getViewport({ scale: this.scale });
    let fixedHeight = viewport.height.toFixed(0);
    let fixedWidth = viewport.width.toFixed(0);

    pageInfo.canvas.height = fixedHeight;
    pageInfo.canvas.width = fixedWidth;

    pageInfo.svg.setAttribute('height', fixedHeight);
    pageInfo.svg.setAttribute('width', fixedWidth);

    if (this._svgScale1 === null) {
        this._svgScale1 = this.scale;
    }
    else {
        const newSvgScale = this.scale / this._svgScale1;

        this.recognizedValueMapper.recognizedValues.forEach(function (rv) {
            rv.rescale(newSvgScale);
        });

        this.recognizedValueMapper.recognizedTables.forEach(function (rt) {
            rt.rescale(fixedWidth, fixedHeight, newSvgScale);
        });

        this._setPageMarginLeft(pageInfo.container);
    }

    var renderContext = {
        canvasContext: pageInfo.canvasContext,
        viewport: viewport
    };
    var renderTask = page.render(renderContext);

    let that = this;
    renderTask.promise.then(function () {
        that.pageRendering = false;

        if (that.pageNumPending.length > 0) {
            const pageNumToRender = that.pageNumPending.shift();

            that.renderPage(pageNumToRender);
        }
    });
}

PdfRecognitionViewer.prototype.queueRenderPage = function (num) {
    if (this.pageRendering === true) {
        this.pageNumPending.push(num);
    }
    else {
        this.renderPage(num);
    }
}

PdfRecognitionViewer.prototype.onZoomOut = function (pdfViewer) {
    if (pdfViewer.scale <= pdfViewer.minScale) {
        return;
    }

    pdfViewer.scale -= pdfViewer.scaleStep;
    pdfViewer.rescalePages();
}

PdfRecognitionViewer.prototype.onZoomIn = function (pdfViewer) {
    if (pdfViewer.scale >= pdfViewer.maxScale) {
        return;
    }

    pdfViewer.scale += pdfViewer.scaleStep;
    pdfViewer.rescalePages();
}

PdfRecognitionViewer.prototype.updateScaleText = function () {
    let percent = (this.scale * 100).toFixed(0);

    this.scaleSpan.textContent = percent;
}

PdfRecognitionViewer.prototype.rescalePages = function () {
    this.updateScaleText();

    for (let i = 1; i <= this.pageInfo.length; i++) {
        this.queueRenderPage(i);
    }
}

PdfRecognitionViewer.prototype.onPrevPage = function (pdfViewer) {
    if (pdfViewer.currentPageNum === 1) {
        return;
    }

    pdfViewer.currentPageNum--;
    pdfViewer.switchPage();
}

PdfRecognitionViewer.prototype.onNextPage = function (pdfViewer) {
    if (pdfViewer.currentPageNum === pdfViewer.pageInfo.length) {
        return;
    }

    pdfViewer.currentPageNum++;
    pdfViewer.switchPage();
}

PdfRecognitionViewer.prototype.switchPage = function () {
    let pageContainer = this.pageInfo[this.currentPageNum - 1].container;
    var topTable = this.container.closest("table");

    topTable.style.position = 'fixed';
    pageContainer.scrollIntoView({ behavior: 'smooth' });

    setTimeout(function () {
        topTable.style.position = '';
    }, 300);
}
