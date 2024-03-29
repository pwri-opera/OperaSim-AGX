/// <reference path="Util.ts"/>
/// <reference path="Object.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
var agx;
(function (agx) {
    var Component = (function (_super) {
        __extends(Component, _super);
        function Component(name) {
            if (name === void 0) { name = ""; }
            _super.call(this, name);
            this.objects = {};
        }
        /**
        Add a child object.
        */
        Component.prototype.addObject = function (object) {
            agx.Assert(object);
            // console.log(this.getPath() + ": Adding " + object.name);
            agx.Assert(!object.context);
            agx.Assert(!agx.IsDefined(this.objects[object.name]));
            this.objects[object.name] = object;
            object.context = this;
        };
        /**
        Remove a child object.
        */
        Component.prototype.removeObject = function (object) {
            agx.Assert(object);
            agx.Assert(object.context == this);
            agx.Assert(agx.IsDefined(this.objects[object.name]));
            object.context = null;
            delete this.objects[object.name];
        };
        /**
        \return The child object with the specified name.
        */
        Component.prototype.getObject = function (name) {
            var object = this.objects[name];
            return agx.IsDefined(object) ? object : null;
        };
        Component.prototype.getNumObjects = function () {
            // This is the proper way, but the agx.Object class shadows the JavaScript Object object,
            // I don't know how to escape the the agx module scope.
            // return Object.keys(this.objects).length
            var numChildren = 0;
            for (var key in this.objects) {
                if (this.objects.hasOwnProperty(key))
                    numChildren++;
            }
            return numChildren;
        };
        /**
        \return The child resource with the specified path.
        */
        Component.prototype.getResource = function (path) {
            var pathComponents = path.split(".");
            var parent = this;
            var object = null;
            for (var i = 0; i < pathComponents.length; ++i) {
                object = parent.getObject(pathComponents[i]);
                if (!object)
                    break;
                parent = object;
            }
            return object;
        };
        return Component;
    })(agx.Object);
    agx.Component = Component;
})(agx || (agx = {}));
