var agx;
(function (agx) {
    var Object = (function () {
        function Object(name) {
            if (name === void 0) { name = ""; }
            this.name = name;
            this.id = Object.idCounter++;
            this.context = null;
        }
        Object.prototype.getPath = function () {
            return (this.context && this.context.context) ? this.context.getPath() + "." + this.name : this.name;
        };
        Object.idCounter = 0;
        return Object;
    })();
    agx.Object = Object;
})(agx || (agx = {}));
