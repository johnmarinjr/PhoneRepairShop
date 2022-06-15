'use strict';

function RescaleUtils() {
}

RescaleUtils.isOffsetNeeded = function(recognizedRect) {
    const p0 = recognizedRect.polygon.points.getItem(0);
    const p1 = recognizedRect.polygon.points.getItem(1);

    // Vertical orientation
    if (p0.x === p1.x) {
        return false;
    }

    try {
        const tgAngle = (p0.y - p1.y) / (p1.x - p0.x);
        const angleInRad = Math.atan(tgAngle);
        const angleInGrad = angleInRad * (180 / Math.PI);
        const offsetNeeded = angleInGrad < 45 && angleInGrad > -45;

        return offsetNeeded;
    }
    catch (e) {
        return false;
    }
}