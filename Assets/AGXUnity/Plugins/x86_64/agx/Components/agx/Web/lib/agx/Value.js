/// <reference path="Object.ts"/>
/// <reference path="Type.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
var agx;
(function (agx) {
    var Value = (function (_super) {
        __extends(Value, _super);
        function Value(format, name) {
            if (name === void 0) { name = ""; }
            _super.call(this, name);
            this.format = format;
        }
        return Value;
    })(agx.Object);
    agx.Value = Value;
})(agx || (agx = {}));
