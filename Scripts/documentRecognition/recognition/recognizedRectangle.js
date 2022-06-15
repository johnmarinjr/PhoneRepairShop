'use strict';

const rectClass = 'recognition-rect';
const mappedRectClass = 'mapped';

function RecognizedRectangle(unit, pageWidth, pageHeight, coordinates, containerWidth, containerHeight, svg) {
    this.x0 = null;
    this.x1 = null;
    this.y0 = null;
    this.y3 = null;
    this.left = null;
    this.top = null;
    this.height = null;
    this.width = null;
    this.polygon = null;

    this._initElement(unit, pageWidth, pageHeight, coordinates, containerWidth, containerHeight, svg);
    this.markAsNotMapped();
}

RecognizedRectangle.prototype.equals = function (otherRect) {
    return this.polygon.getAttribute('points') === otherRect.polygon.getAttribute('points');
}

RecognizedRectangle.prototype._initElement = function (unit, pageWidth, pageHeight, coordinates, containerWidth, containerHeight, svg) {
    let localCoordinates = JSON.parse(JSON.stringify(coordinates));

    if (unit && unit !== 'scale' && pageWidth && pageHeight) {
        localCoordinates.forEach(function (coord) {
            coord.x /= pageWidth;
            coord.y /= pageHeight;
        });
    }

    this.x0 = localCoordinates[0].x;
    this.x1 = localCoordinates[1].x;
    this.y0 = localCoordinates[0].y;
    this.y3 = localCoordinates[3].y;

    this.polygon = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
    this.polygon.classList.add(rectClass);

    const that = this;
    localCoordinates.forEach(function (coord) {
        const point = svg.createSVGPoint();

        point.x = that.convertRelativeToPixel(coord.x, containerWidth);
        point.y = that.convertRelativeToPixel(coord.y, containerHeight);

        that.polygon.points.appendItem(point);
    });

    this.rescale(containerWidth, containerHeight);
}

RecognizedRectangle.prototype.rescale = function (containerWidth, containerHeight, scale) {
    this.left = this.convertRelativeToPixel(this.x0, containerWidth);
    this.top = this.convertRelativeToPixel(this.y0, containerHeight);
    this.height = this.convertRelativeToPixel(this.y3 - this.y0, containerHeight);
    this.width = this.convertRelativeToPixel(this.x1 - this.x0, containerWidth);

    if (scale) {
        this.polygon.setAttribute('transform', 'scale(' + scale + ', ' + scale + ')');
    }
}

RecognizedRectangle.prototype.convertRelativeToPixel = function (relativeValue, valueOf100Percents) {
    return valueOf100Percents * relativeValue;
}

RecognizedRectangle.prototype.subscribeOnMousedown = function (callback) {
    const that = this;

    this.polygon.addEventListener('mousedown', function (event) {
        callback(that, event);
    });
}

RecognizedRectangle.prototype.subscribeOnMouseenter = function (callback) {
    this.polygon.addEventListener('mouseenter', callback);
}

RecognizedRectangle.prototype.unsubscribeOnMouseenter = function (callback) {
    this.polygon.removeEventListener('mouseenter', callback);
}

RecognizedRectangle.prototype.subscribeOnMouseleave = function (callback) {
    this.polygon.addEventListener('mouseleave', callback);
}

RecognizedRectangle.prototype.unsubscribeOnMouseleave = function (callback) {
    this.polygon.removeEventListener('mouseleave', callback);
}

RecognizedRectangle.prototype.appendToParent = function (parent) {
    parent.appendChild(this.polygon)
}

RecognizedRectangle.prototype.isMapped = function () {
    return this.polygon.classList.contains(mappedRectClass) ? true : false;
}

RecognizedRectangle.prototype.markAsMapped = function () {
    this.polygon.classList.add(mappedRectClass);
}

RecognizedRectangle.prototype.markAsNotMapped = function () {
    this.polygon.classList.remove(mappedRectClass);
}

RecognizedRectangle.prototype.addClass = function (cssClass) {
    this.polygon.classList.add(cssClass);
}

RecognizedRectangle.prototype.removeClass = function (cssClass) {
    this.polygon.classList.remove(cssClass);
}

RecognizedRectangle.prototype.hasClass = function (cssClass) {
    return this.polygon.classList.contains(cssClass);
}