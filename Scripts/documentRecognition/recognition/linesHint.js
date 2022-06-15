'use strict';

const mainContainerClass = 'linesHintMainContainer';
const textContainerWithButtonClass = 'linesHintTextContainerWithButton';
const linesSelectPrefixClass = 'linesHintSelectPrefixLabel';
const linesCounterClass = 'linesHintCounter';
const linesCounterLabelClass = 'linesHintCounterLabel';
const linesButtonClass = 'linesHintButton';
const hiddenClass = 'linesHintHidden';

function LinesHint(parentElement, buttonTextSingleLine, buttonTextMultipleLines, selectTextPrefix, selectTextSingleLine, selectTextMultipleLines,
    buttonText, onButtonClickCallback, onSelectAllLinesCallback, onSelectAllLinesPrevCallback) {
    this.containerElement = null;
    this.textContainerElement = null;
    this.linesCounterElement = null;
    this.linesCounterLabelElement = null;
    this.linesSelectPrefixLabelElement = null;
    this.buttonElement = null;

    this.buttonTextSingleLine = buttonTextSingleLine;
    this.buttonTextMultipleLines = buttonTextMultipleLines;
    this.selectTextSingleLine = selectTextSingleLine;
    this.selectTextMultipleLines = selectTextMultipleLines;

    this.selectMode = null;
    this.selectLinesCount = null;
    this.countToSelectPrev = null;

    this.onSelectAllLinesCallback = onSelectAllLinesCallback;
    this.onSelectAllLinesPrevCallback = onSelectAllLinesPrevCallback;

    this._createControls(parentElement, selectTextPrefix, buttonText, onButtonClickCallback);
    this.setSelectMode(false);
    this.setLinesCount(0);
}

LinesHint.prototype._createControls = function (parentElement, selectTextPrefix, buttonText, onButtonClickCallback) {
    const mainContainer = document.createElement('div');
    mainContainer.classList.add(mainContainerClass);
    this.containerElement = mainContainer;

    const textContainer = document.createElement('div');
    textContainer.classList.add(textContainerWithButtonClass);
    mainContainer.appendChild(textContainer);
    this.textContainerElement = textContainer;

    const linesSelectPrefixLabel = document.createElement('div');
    linesSelectPrefixLabel.classList.add(linesSelectPrefixClass);
    textContainer.appendChild(linesSelectPrefixLabel);
    this.linesSelectPrefixLabelElement = linesSelectPrefixLabel;

    const prefixTextNode = document.createTextNode(selectTextPrefix);
    linesSelectPrefixLabel.appendChild(prefixTextNode);

    const linesCounter = document.createElement('div');
    linesCounter.classList.add(linesCounterClass);
    textContainer.appendChild(linesCounter);
    this.linesCounterElement = linesCounter;

    const linesCounterLabel = document.createElement('div');
    linesCounterLabel.classList.add(linesCounterLabelClass);
    textContainer.appendChild(linesCounterLabel);
    this.linesCounterLabelElement = linesCounterLabel;

    const button = document.createElement('div');
    button.classList.add(linesButtonClass);
    mainContainer.appendChild(button);
    this.buttonElement = button;

    const buttonTextNode = document.createTextNode(buttonText);
    button.appendChild(buttonTextNode);

    if (onButtonClickCallback) {
        button.addEventListener('click', onButtonClickCallback);
    }

    parentElement.appendChild(mainContainer);
}

LinesHint.prototype.setSelectMode = function (selectMode, selectLinesCount) {
    if (selectMode === true) {
        this.selectMode = true;
        this.selectLinesCount = selectLinesCount;
        this.setLinesCount(0)
        this.linesSelectPrefixLabelElement.classList.remove(hiddenClass);
        this.buttonElement.classList.add(hiddenClass);
        this.textContainerElement.classList.remove(textContainerWithButtonClass);
    }
    else {
        this.selectMode = false;
        this.selectLinesCount = null;
        this.linesSelectPrefixLabelElement.classList.add(hiddenClass);
        this.buttonElement.classList.remove(hiddenClass);
        this.textContainerElement.classList.add(textContainerWithButtonClass);
    }
}

LinesHint.prototype.linesToSelect = function () {
    return this.selectLinesCount;
}

LinesHint.prototype.setLinesCount = function (count) {
    let hideTextContainer = false;

    if (this.selectMode === true) {
        const countToSelect = this.selectLinesCount - count;
        if (countToSelect === 0) {
            hideTextContainer = true;

            this.buttonElement.classList.remove(hiddenClass);
            this.textContainerElement.classList.add(textContainerWithButtonClass);

            this.onSelectAllLinesCallback();
        }
        else {
            this.buttonElement.classList.add(hiddenClass);
            this.textContainerElement.classList.remove(textContainerWithButtonClass);

            this.linesCounterElement.textContent = countToSelect;
            this.linesCounterLabelElement.textContent = countToSelect == 1 ? this.selectTextSingleLine : this.selectTextMultipleLines;

            if (this.countToSelectPrev === 0) {
                this.onSelectAllLinesPrevCallback();
            }
        }

        this.countToSelectPrev = countToSelect;
    }
    else {
        this.linesCounterElement.textContent = count;
        this.linesCounterLabelElement.textContent = count == 1 ? this.buttonTextSingleLine : this.buttonTextMultipleLines;
    }

    if (hideTextContainer === true) {
        this.textContainerElement.classList.add(hiddenClass);
    }
    else {
        this.textContainerElement.classList.remove(hiddenClass);
    }
}

LinesHint.prototype.setVisible = function (isVisible) {
    this.containerElement.style.display = isVisible ? '' : 'none';
}

LinesHint.prototype.reset = function () {
    this.setSelectMode(false);
    this.setLinesCount(0);
    this.countToSelectPrev = null;
}