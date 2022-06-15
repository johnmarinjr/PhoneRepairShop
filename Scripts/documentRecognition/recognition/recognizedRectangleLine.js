'use strict';

function RecognizedRectangleLine(unit, pageWidth, pageHeight, cells, cssClass, containerWidth, containerHeight, svg) {
    this.isSelected = false;
    this.rectangles = [];
    this.cells = cells;

    const that = this;
    this.cells.forEach(function (c) {
        const rect = new RecognizedRectangle(unit, pageWidth, pageHeight, c.boundingBox, containerWidth, containerHeight, svg);

        rect.polygon.classList.add(cssClass);
        that.rectangles.push(rect);
    });
}

RecognizedRectangleLine.prototype.markAsMapped = function () {
    this.rectangles.forEach(function (rect) {
        rect.markAsMapped();
    });
}

RecognizedRectangleLine.prototype.markAsNotMapped = function () {
    this.rectangles.forEach(function (rect) {
        rect.markAsNotMapped();
    });
}

RecognizedRectangleLine.prototype.appendToParent = function (parent) {
    this.rectangles.forEach(function (rect) {
        rect.appendToParent(parent);
    });
}

RecognizedRectangleLine.prototype.rescale = function (containerWidth, containerHeight, scale) {
    this.rectangles.forEach(function (rect) {
        rect.rescale(containerWidth, containerHeight, scale);
    });
}

RecognizedRectangleLine.prototype.reset = function () {
    this.markAsNotMapped();
}

RecognizedRectangleLine.prototype.addClass = function (cssClass) {
    this.rectangles.forEach(function (rect) {
        rect.addClass(cssClass);
    })
}

RecognizedRectangleLine.prototype.removeClass = function (cssClass) {
    this.rectangles.forEach(function (rect) {
        rect.removeClass(cssClass);
    })
}

RecognizedRectangleLine.prototype.hasClass = function (cssClass) {
    for (let i = 0; i < this.rectangles.length; i++) {
        if (this.rectangles[i].hasClass(cssClass)) {
            return true;
        }
    }

    return false;
}

RecognizedRectangleLine.prototype.isMapped = function () {
    for (let i = 0; i < this.rectangles.length; i++) {
        if (this.rectangles[i].isMapped()) {
            return true;
        }
    }

    return false;
}

RecognizedRectangleLine.prototype.subscribeOnMousedown = function (callback) {
    const that = this;

    this.rectangles.forEach(function (rect) {
        rect.subscribeOnMousedown(function (r, event) {
            callback(that, event);
        });
    });
}

RecognizedRectangleLine.prototype.subscribeOnMouseenter = function (callback) {
    this.rectangles.forEach(function (rect) {
        rect.subscribeOnMouseenter(callback);
    });
}

RecognizedRectangleLine.prototype.unsubscribeOnMouseenter = function (callback) {
    this.rectangles.forEach(function (rect) {
        rect.unsubscribeOnMouseenter(callback);
    })
}

RecognizedRectangleLine.prototype.subscribeOnMouseleave = function (callback) {
    this.rectangles.forEach(function (rect) {
        rect.subscribeOnMouseleave(callback);
    });
}

RecognizedRectangleLine.prototype.unsubscribeOnMouseleave = function (callback) {
    this.rectangles.forEach(function (rect) {
        rect.unsubscribeOnMouseleave(callback);
    });
}

RecognizedRectangleLine.prototype.getSelected = function () {
    return this.isSelected;
}

RecognizedRectangleLine.prototype.setSelected = function (isSelected) {
    this.isSelected = isSelected;

    if (isSelected === true) {
        this.markAsMapped();
    }
    else {
        this.markAsNotMapped();
    }
}