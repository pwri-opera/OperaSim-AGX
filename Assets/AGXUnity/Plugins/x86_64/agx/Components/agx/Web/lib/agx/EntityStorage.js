/// <reference path="Util.ts"/>
/// <reference path="Component.ts"/>
/// <reference path="Buffer.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
var agx;
(function (agx) {
    var EntityStorage = (function (_super) {
        __extends(EntityStorage, _super);
        // buffers : Buffer[];
        function EntityStorage(entity, name) {
            if (name === void 0) { name = ""; }
            _super.call(this, name);
            this.entity = entity;
            this.numElements = 0;
            this.capacity = 0;
            this.idToIndexBuffer = null;
            this.indexTdIdBuffer = null;
            // this.buffers = [];
        }
        // Override Component.addObject
        EntityStorage.prototype.addObject = function (object) {
            _super.prototype.addObject.call(this, object);
            agx.Assert(object instanceof agx.Buffer);
            var buffer = object;
            // buffers.push(buffer);
            if (buffer.name == EntityStorage.IdToIndexName) {
                agx.Assert(!this.idToIndexBuffer);
                this.idToIndexBuffer = buffer;
            }
            else if (buffer.name == EntityStorage.IndexToIdName) {
                agx.Assert(!this.indexTdIdBuffer);
                this.indexTdIdBuffer = buffer;
            }
        };
        // Override Component.removeObject
        EntityStorage.prototype.removeObject = function (object) {
            _super.prototype.removeObject.call(this, object);
            if (object == this.idToIndexBuffer)
                this.idToIndexBuffer = null;
            if (object == this.indexTdIdBuffer)
                this.indexTdIdBuffer = null;
        };
        /**
        \return The index for a specified instance id
        */
        EntityStorage.prototype.idToIndex = function (id) {
            agx.Assert(this.idToIndexBuffer);
            agx.Assert(id < this.idToIndexBuffer.numElements);
            var index = this.idToIndexBuffer.data[id];
            agx.Assert(index < this.numElements, "The given id does not denote a valid entity instance.");
            return index;
        };
        EntityStorage.IdToIndexName = "_idToIndex";
        EntityStorage.IndexToIdName = "_indexToId";
        return EntityStorage;
    })(agx.Component);
    agx.EntityStorage = EntityStorage;
})(agx || (agx = {}));
