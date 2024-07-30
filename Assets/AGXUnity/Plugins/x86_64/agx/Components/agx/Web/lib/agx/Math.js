var agx;
(function (agx) {
    // export var InvalidIndex = -1;
    function AlignCeil(value, alignment) {
        if (value > 0)
            return (value + (alignment - 1) - ((value - 1) % alignment));
        else
            return 0;
    }
    agx.AlignCeil = AlignCeil;
    function AlignFloor(value, alignment) {
        return value - (value % alignment);
    }
    agx.AlignFloor = AlignFloor;
    function IsAligned(value, alignment) {
        return value % alignment == 0;
    }
    agx.IsAligned = IsAligned;
    function Epsilon() {
        return 2.220460492503130808472633361816E-16;
    }
    agx.Epsilon = Epsilon;
    function toReadableNumber(value) {
        var absValue = Math.abs(value);
        var exponentialLimit = 1e6;
        if (absValue == 0)
            // Special case for initialized values.
            return String(Math.round(value));
        else if (absValue < exponentialLimit && absValue > 1 / exponentialLimit)
            // Remove extra digits.
            return String(Math.round(value * exponentialLimit) / exponentialLimit);
        else
            // Make more readable.
            return String(value.toExponential(3));
    }
    agx.toReadableNumber = toReadableNumber;
})(agx || (agx = {}));
