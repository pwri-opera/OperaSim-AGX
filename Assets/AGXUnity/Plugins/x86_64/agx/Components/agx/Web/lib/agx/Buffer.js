/// <reference path="Util.ts"/>
/// <reference path="Type.ts"/>
/// <reference path="Object.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
var agx;
(function (agx) {
    var Buffer = (function (_super) {
        __extends(Buffer, _super);
        function Buffer(format, name) {
            if (name === void 0) { name = ""; }
            _super.call(this, name);
            agx.Assert(format);
            this.format = format;
            this.data = null;
            this.numElements = 0;
            this.displayName = name;
        }
        return Buffer;
    })(agx.Object);
    agx.Buffer = Buffer;
})(agx || (agx = {}));
