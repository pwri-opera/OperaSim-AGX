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
/// <reference path="Util.ts"/>
/// <reference path="Buffer.ts"/>
/// <reference path="Value.ts"/>
/// <reference path="Object.ts"/>
var agx;
(function (agx) {
    var Format = (function (_super) {
        __extends(Format, _super);
        function Format(name, numElements, numBytes, arrayType, customParser) {
            if (customParser === void 0) { customParser = null; }
            _super.call(this, name);
            this.numElements = numElements;
            this.numBytes = numBytes;
            this.arrayType = arrayType;
            this.customParser = customParser;
            if (Format.FormatMap[name])
                throw "Format " + name + " is already registered";
            Format.FormatMap[name] = this;
        }
        // Initialized formats, done automatically on startup
        Format.Init = function () {
            console.log("agx.Format.Init");
            new Format("Real:32bit", 1, 4, Float32Array);
            new Format("Real:64bit", 1, 8, Float64Array);
            new Format("Int:8bit", 1, 1, Int8Array);
            new Format("Int:16bit", 1, 2, Int16Array);
            new Format("Int:32bit", 1, 4, Int32Array);
            new Format("Int:64bit", 2, 8, Int32Array); // JavaScript does not support 64bit integers
            new Format("UInt:8bit", 1, 1, Uint8Array);
            new Format("UInt:16bit", 1, 2, Uint16Array);
            new Format("UInt:32bit", 1, 4, Uint32Array);
            new Format("UInt:64bit", 2, 8, Uint32Array); // JavaScript does not support 64bit integers
            new Format("Bool:8bit", 1, 1, Uint8Array);
            new Format("Vec3:32bit", 4, 16, Float32Array);
            new Format("Vec3:64bit", 4, 32, Float64Array);
            new Format("Vec4:32bit", 4, 16, Float32Array);
            new Format("Vec4:64bit", 4, 32, Float64Array);
            new Format("Matrix4x4:32bit", 16, 64, Float32Array);
            new Format("Matrix4x4:64bit", 16, 128, Float64Array);
            new Format("AffineMatrix4x4:32bit", 16, 64, Float32Array);
            new Format("AffineMatrix4x4:64bit", 16, 128, Float64Array);
            new Format("IndexRange:32bit", 2, 8, Uint32Array);
            new Format("IndexRange:64bit", 4, 16, Uint32Array); // JavaScript does not support 64bit integers
            new Format("Name", 0, 1, Uint8Array, Format.extractNameBuffer);
            return true;
        };
        Format.prototype.extractBuffer = function (header, binarySegment) {
            agx.AssertDefined(header.name);
            agx.AssertDefined(header.numElements);
            agx.AssertDefined(header.type);
            agx.Assert(header.type == this.name);
            if (agx.IsDefined(header.customSerialization) && header.customSerialization) {
                agx.Assert(this.customParser);
                return this.customParser(header, binarySegment);
            }
            agx.AssertDefined(header.numBytes);
            agx.AssertDefined(header.byteOffset);
            agx.Assert(header.numBytes == header.numElements * this.numBytes);
            var buffer = new agx.Buffer(this, header.name);
            if (header.numElements > 0) {
                buffer.numElements = header.numElements;
                buffer.data = new this.arrayType(binarySegment.buffer, binarySegment.byteOffset + header.byteOffset, header.numElements * this.numElements);
                buffer.data.numElements = buffer.numElements;
                buffer.data.format = this;
            }
            return buffer;
        };
        Format.prototype.extractValue = function (header, binarySegment) {
            agx.AssertDefined(header.name);
            agx.AssertDefined(header.type);
            agx.AssertDefined(header.numBytes);
            agx.AssertDefined(header.byteOffset);
            agx.Assert(header.type == this.name);
            agx.Assert(header.numBytes == this.numBytes);
            var value = new agx.Value(this, header.name);
            var dataArray = new this.arrayType(binarySegment.buffer, binarySegment.byteOffset + header.byteOffset, this.numElements);
            if (this.numElements == 1)
                value.data = dataArray[0];
            else
                value.data = dataArray.subarray(0, this.numElements);
            return value;
        };
        //////////////////////////////////////////////////////////
        Format.extractNameBuffer = function (header, binarySegment) {
            agx.AssertDefined(header.name);
            agx.AssertDefined(header.children);
            var subBuffers = {};
            for (var i = 0; i < header.children.length; ++i) {
                var child = header.children[i];
                agx.AssertDefined(child.name);
                subBuffers[child.name] = agx.ExtractBuffer(child, binarySegment);
            }
            agx.AssertDefined(subBuffers.range);
            agx.AssertDefined(subBuffers.characters);
            var rangeBuffer = subBuffers.range;
            var charBuffer = subBuffers.characters;
            var buffer = new agx.Buffer(agx.GetFormat("Name"), header.name);
            buffer.data = [];
            buffer.data.format = buffer.format;
            buffer.data.numElements = buffer.numElements;
            for (var i = 0; i < buffer.numElements; i++) {
                var range = { begin: rangeBuffer.data[i * 2], end: rangeBuffer.data[i * 2 + 1] };
                var name = '';
                for (var j = range.begin; j < range.end; j++)
                    name += String.fromCharCode(charBuffer.data[j]);
                buffer.data.push(name);
            }
            ;
            return buffer;
        };
        Format.FormatMap = {}; // Do not access directly, use agx.GetFormat instead
        // Init formats on load
        Format.foo = Format.Init();
        return Format;
    })(agx.Object);
    agx.Format = Format;
    function IsFormat(name) {
        return agx.IsDefined(Format.FormatMap[name]);
    }
    agx.IsFormat = IsFormat;
    /**
    \return The format with the specified name.
    */
    function GetFormat(name) {
        var format = Format.FormatMap[name];
        if (format == undefined)
            throw "Unknown format " + name;
        return format != undefined ? format : null;
    }
    agx.GetFormat = GetFormat;
})(agx || (agx = {}));
/// <reference path="Type.ts"/>
var agx;
(function (agx) {
    /**
    Test if an object is a string.
    */
    function isString(s) {
        return typeof (s) === 'string' || s instanceof String;
    }
    agx.isString = isString;
    function startswith(str, prefix) {
        return str.length >= prefix.length && str.substring(0, prefix.length) == prefix;
    }
    agx.startswith = startswith;
    function endswith(str, suffix) {
        return str.length >= suffix.length && str.substr(str.length - suffix.length) == suffix;
    }
    agx.endswith = endswith;
    function capitalize(str) {
        return str.charAt(0).toUpperCase() + str.slice(1);
    }
    agx.capitalize = capitalize;
    function encodeUTF8(s) {
        return unescape(encodeURIComponent(s));
    }
    agx.encodeUTF8 = encodeUTF8;
    function decodeUTF8(s) {
        return decodeURIComponent(escape(s));
    }
    agx.decodeUTF8 = decodeUTF8;
    /**
    Extract a string from an array buffer
  
    See http://wiki.whatwg.org/wiki/StringEncoding
    and http://updates.html5rocks.com/2012/06/How-to-convert-ArrayBuffer-to-and-from-String
    */
    function ExtractStringFromArray(array) {
        var headerString = '';
        for (var i = 0; i < array.length; ++i)
            headerString = headerString + String.fromCharCode(array[i]);
        return headerString;
        // return String.fromCharCode.apply(null, array);
        /**
        Doesn't work on Chrome 16.0.912.77 under Linux:
        Uncaught TypeError: Function.prototype.apply: Arguments list has wrong type
        May be that fromCharCode requires UInt16Array and arrayView is UInt8Array. Not sure.
        return String.fromCharCode.apply(null, arrayView);
        */
    }
    agx.ExtractStringFromArray = ExtractStringFromArray;
    /**
    Write a string to an array.
    */
    function WriteStringToArray(array, str) {
        var strLen = str.length;
        for (var i = 0; i < strLen; i++)
            array[i] = str.charCodeAt(i);
    }
    agx.WriteStringToArray = WriteStringToArray;
    /**
    Create a WebSocket
    */
    function CreateWebSocket(url, protocol) {
        if (protocol === void 0) { protocol = "agx_stream"; }
        var socket = new WebSocket(url, protocol);
        socket.binaryType = "arraybuffer";
        return socket;
    }
    agx.CreateWebSocket = CreateWebSocket;
    /**
    Verifies that a specified element is not undefined.
    */
    function AssertDefined(element) {
        if (IsUndefined(element))
            throw "AssertDefined failed!";
    }
    agx.AssertDefined = AssertDefined;
    function IsDefined(element) {
        return typeof element !== 'undefined';
    }
    agx.IsDefined = IsDefined;
    function IsUndefined(element) {
        return typeof element === 'undefined';
    }
    agx.IsUndefined = IsUndefined;
    /**
    Assert implementation. NOTE: message is always constructed even if assertion is valid (performance overhead)
    */
    function Assert(expression, message) {
        if (!expression)
            throw (message != undefined ? message : "Assert failed");
    }
    agx.Assert = Assert;
    /**
    Abort implementation.
    */
    function Abort(message) {
        throw (message != undefined ? message : "Abort!");
    }
    agx.Abort = Abort;
    /**
    Extract a serialized buffer.
    */
    function ExtractBuffer(header, binarySegment) {
        agx.AssertDefined(header.type);
        var format = agx.GetFormat(header.type);
        if (agx.IsDefined(header.isPartial) && header.isPartial) {
            // buffer = this.extractPartialBuffer(bufferNode, binarySegment);
            agx.Abort('TODO');
        }
        return format.extractBuffer(header, binarySegment);
    }
    agx.ExtractBuffer = ExtractBuffer;
    /**
    Extract a serialized value.
    */
    function ExtractValue(header, binarySegment) {
        agx.AssertDefined(header.type);
        var format = agx.GetFormat(header.type);
        return format.extractValue(header, binarySegment);
    }
    agx.ExtractValue = ExtractValue;
    function getQueryString(name) {
        var result = (new RegExp('[?&]' + encodeURIComponent(name) + '=([^&]*)')).exec(location.search);
        if (result)
            return decodeURIComponent(result[1]);
    }
    agx.getQueryString = getQueryString;
})(agx || (agx = {}));
/// <reference path="Util.ts"/>
/// <reference path="Type.ts"/>
/// <reference path="Object.ts"/>
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
/// <reference path="Util.ts"/>
/// <reference path="Object.ts"/>
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
/// <reference path="Util.ts"/>
/// <reference path="Component.ts"/>
/// <reference path="Buffer.ts"/>
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
/// <reference path="Math.ts"/>
/// <reference path="Util.ts"/>
/// <reference path="Type.ts"/>
/// <reference path="Component.ts"/>
/// <reference path="Value.ts"/>
/// <reference path="Buffer.ts"/>
/// <reference path="EntityStorage.ts"/>
var agx;
(function (agx) {
    /**
    Data frame.
    */
    var Frame = (function (_super) {
        __extends(Frame, _super);
        function Frame(index, timeStamp, timeStep) {
            _super.call(this);
            this.index = index;
            this.timeStamp = timeStamp;
            this.timeStep = timeStep;
            this.isKeyFrame = false;
        }
        /**
        Construct a frame from a binary packet.
        */
        Frame.CreateFromPacket = function (packet) {
            var frameMessage = agx.StructuredMessage.ParseMessage(packet);
            return Frame.CreateFromMessage(frameMessage);
        };
        /**
        Construct a frame from a structured message.
        */
        Frame.CreateFromMessage = function (message) {
            var header = message.header;
            // console.log(JSON.stringify(message.header, undefined, 2));
            agx.AssertDefined(header.index);
            agx.AssertDefined(header.timeStamp);
            agx.AssertDefined(header.timeStep);
            agx.AssertDefined(header.keyFrame);
            var frame = new Frame(header.index, header.timeStamp, header.timeStep);
            frame.isKeyFrame = header.keyFrame;
            frame.addChildren(frame, header, message.binarySegment);
            return frame;
        };
        /**
        Merge another frame into this one. This will 'steal' buffers
        from the other frame which should be discarded after the merge.
        */
        Frame.prototype.merge = function (source) {
            agx.Assert(source.index > this.index);
            this.index = source.index;
            this.timeStep = source.timeStep;
            this.timeStamp = source.timeStamp;
            this.mergeChildren(this, source);
        };
        Frame.prototype.createRigidBodyBuffer = function (type, name) {
            var rbStorage = this.getObject("RigidBody");
            if (!rbStorage)
                return;
            var buffer = rbStorage.getObject(type);
            if (!buffer)
                return;
            var oldPositionBuffer = rbStorage.getObject(name);
            if (oldPositionBuffer)
                rbStorage.removeObject(oldPositionBuffer);
            var result;
            if (name == "position")
                result = Frame.extractPositionBuffer(buffer);
            else
                result = Frame.extractRotationBuffer(buffer);
            rbStorage.addObject(result);
        };
        Frame.extractPositionBuffer = function (transformBuffer) {
            var positionBuffer = new agx.Buffer(agx.GetFormat("Vec3:64bit"), "position");
            positionBuffer.numElements = transformBuffer.numElements;
            positionBuffer.data = new Float64Array(positionBuffer.numElements * positionBuffer.format.numElements);
            for (var i = 0; i < positionBuffer.numElements; ++i) {
                var translateIndex = i * transformBuffer.format.numElements + 12;
                var posIndex = i * positionBuffer.format.numElements;
                for (var j = 0; j < 3; j++)
                    positionBuffer.data[posIndex + j] = transformBuffer.data[translateIndex + j];
            }
            return positionBuffer;
        };
        Frame.extractRotationBuffer = function (transformBuffer) {
            var rotationBuffer = new agx.Buffer(agx.GetFormat("Vec3:64bit"), "rotation");
            rotationBuffer.numElements = transformBuffer.numElements;
            rotationBuffer.data = new Float64Array(rotationBuffer.numElements * rotationBuffer.format.numElements);
            var i = 0;
            var j = 1;
            var k = 2;
            for (var index = 0; index < rotationBuffer.numElements; ++index) {
                var translateIndex = index * transformBuffer.format.numElements;
                var posIndex = index * rotationBuffer.format.numElements;
                var getMatrixCell = function (buffer, index, x, y) {
                    return buffer.data[index + x * 4 + y];
                };
                var cy = Math.sqrt(Math.pow(getMatrixCell(transformBuffer, translateIndex, j, k), 2) + Math.pow(getMatrixCell(transformBuffer, translateIndex, k, k), 2));
                if (cy > 16 * agx.Epsilon()) {
                    rotationBuffer.data[posIndex + 0] = Math.atan2(getMatrixCell(transformBuffer, translateIndex, j, k), getMatrixCell(transformBuffer, translateIndex, k, k));
                    rotationBuffer.data[posIndex + 1] = Math.atan2(-getMatrixCell(transformBuffer, translateIndex, i, k), cy);
                    rotationBuffer.data[posIndex + 2] = Math.atan2(getMatrixCell(transformBuffer, translateIndex, i, j), getMatrixCell(transformBuffer, translateIndex, i, i));
                }
                else {
                    rotationBuffer.data[posIndex + 0] = Math.atan2(-getMatrixCell(transformBuffer, translateIndex, k, j), getMatrixCell(transformBuffer, translateIndex, j, j));
                    rotationBuffer.data[posIndex + 1] = Math.atan2(-getMatrixCell(transformBuffer, translateIndex, i, k), cy);
                    rotationBuffer.data[posIndex + 2] = 0;
                }
            }
            return rotationBuffer;
        };
        Frame.extractMagnitudeBuffer = function (buffer) {
            var subComponentBuffer = new agx.Buffer(agx.GetFormat("Real:64bit"), buffer.name);
            subComponentBuffer.numElements = buffer.numElements;
            subComponentBuffer.data = new Float64Array(buffer.numElements);
            for (var i = 0; i < buffer.numElements; ++i)
                subComponentBuffer.data[i] = Math.sqrt(Math.pow(buffer.data[i * buffer.format.numElements + 0], 2) +
                    Math.pow(buffer.data[i * buffer.format.numElements + 1], 2) +
                    Math.pow(buffer.data[i * buffer.format.numElements + 2], 2));
            return subComponentBuffer;
        };
        Frame.extractSubComponentBuffer = function (buffer, subComponentIndex) {
            var subComponentBuffer = new agx.Buffer(agx.GetFormat("Real:64bit"), buffer.name);
            subComponentBuffer.numElements = buffer.numElements;
            subComponentBuffer.data = new Float64Array(buffer.numElements);
            for (var i = 0; i < buffer.numElements; ++i)
                subComponentBuffer.data[i] = buffer.data[i * buffer.format.numElements + subComponentIndex];
            return subComponentBuffer;
        };
        ///////////////////////////////////////////////////////////////////
        Frame.prototype.addChildren = function (parent, frameNode, binarySegment) {
            if (!agx.IsDefined(frameNode.children))
                return;
            for (var i = 0; i < frameNode.children.length; ++i) {
                var child = frameNode.children[i];
                agx.AssertDefined(child.nodeType);
                agx.AssertDefined(child.name);
                if (child.nodeType == "Storage") {
                    this.addStorage(parent, child, binarySegment);
                }
                else if (child.nodeType == "Buffer") {
                    this.addBuffer(parent, child, binarySegment);
                }
                else if (child.nodeType == "Value") {
                    this.addValue(parent, child, binarySegment);
                }
                else if (child.nodeType == "Component") {
                    this.addComponent(parent, child, binarySegment);
                }
                else {
                    throw "Unknown node type " + child.nodeType;
                }
            }
        };
        Frame.prototype.addStorage = function (parent, storageNode, binarySegment) {
            agx.AssertDefined(storageNode.entity);
            agx.AssertDefined(storageNode.numElements);
            agx.AssertDefined(storageNode.capacity);
            var storage = new agx.EntityStorage(storageNode.entity);
            storage.name = storageNode.name;
            storage.numElements = storageNode.numElements;
            storage.capacity = storageNode.capacity;
            parent.addObject(storage);
            this.addChildren(storage, storageNode, binarySegment);
        };
        Frame.prototype.addBuffer = function (parent, bufferNode, binarySegment) {
            parent.addObject(agx.ExtractBuffer(bufferNode, binarySegment));
        };
        Frame.prototype.addValue = function (parent, valueNode, binarySegment) {
            parent.addObject(agx.ExtractValue(valueNode, binarySegment));
        };
        Frame.prototype.addComponent = function (parent, componentNode, binarySegment) {
            var component = new agx.Component(componentNode.name);
            parent.addObject(component);
            this.addChildren(component, componentNode, binarySegment);
        };
        Frame.prototype.mergeChildren = function (parent, otherParent) {
            for (var childName in otherParent.objects) {
                var child = otherParent.getObject(childName);
                var existingChild = parent.getObject(childName);
                otherParent.removeObject(child);
                if (!existingChild) {
                    parent.addObject(child);
                }
                else {
                    if (child instanceof agx.EntityStorage) {
                        agx.Assert(existingChild instanceof agx.EntityStorage);
                        this.mergeStorage(existingChild, child);
                    }
                    else if (child instanceof agx.Buffer) {
                        agx.Assert(existingChild instanceof agx.Buffer);
                        parent.removeObject(existingChild);
                        parent.addObject(child);
                    }
                    else if (child instanceof agx.Value) {
                        agx.Assert(existingChild instanceof agx.Value);
                        parent.removeObject(existingChild);
                        parent.addObject(child);
                    }
                    else if (child instanceof agx.Component) {
                        agx.Assert(existingChild instanceof agx.Component);
                        this.mergeChildren(existingChild, child);
                    }
                    else {
                        agx.Abort(child.getPath() + ": Unknown type");
                    }
                }
            }
        };
        Frame.prototype.mergeStorage = function (target, source) {
            agx.Assert(target.entity == source.entity);
            target.numElements = source.numElements;
            target.capacity = source.capacity;
            this.mergeChildren(target, source);
        };
        Frame.prototype.mergeBuffer = function (target, source) {
        };
        return Frame;
    })(agx.Component);
    agx.Frame = Frame;
    /**
    PreHeader is meta data for all messages sent over socket.
    */
    var PreHeader = (function () {
        function PreHeader(id, idResponse, uriSize, headerSize, binarySegmentOffset, binarySegmentSize) {
            if (id === void 0) { id = 0; }
            if (idResponse === void 0) { idResponse = 0; }
            if (uriSize === void 0) { uriSize = 0; }
            if (headerSize === void 0) { headerSize = 0; }
            if (binarySegmentOffset === void 0) { binarySegmentOffset = 0; }
            if (binarySegmentSize === void 0) { binarySegmentSize = 0; }
            this.id = id;
            this.idResponse = idResponse;
            this.uriSize = uriSize;
            this.headerSize = headerSize;
            this.binarySegmentOffset = binarySegmentOffset;
            this.binarySegmentSize = binarySegmentSize;
        }
        /**
        Construct a preheader from a binary packet.
        */
        PreHeader.CreateFromPacket = function (packet) {
            var preHeaderData = new Uint32Array(packet, 0, PreHeader.NumElements);
            return new PreHeader(preHeaderData[0], preHeaderData[1], preHeaderData[2], preHeaderData[3], preHeaderData[4], preHeaderData[5]);
        };
        /**
        \return The full size of the represented packet.
        */
        PreHeader.prototype.getPacketSize = function () {
            return this.binarySegmentOffset > 0 ? this.binarySegmentOffset + this.binarySegmentSize : PreHeader.NumBytes + this.uriSize + this.headerSize;
        };
        /**
        Write the preheader to a packet.
        */
        PreHeader.prototype.writeToPacket = function (packet) {
            if (packet.byteLength < PreHeader.NumBytes)
                throw "Invalid packet size: " + packet.byteLength;
            var data = new Uint32Array(packet, 0, PreHeader.NumElements);
            data[0] = this.id;
            data[1] = this.idResponse;
            data[2] = this.uriSize;
            data[3] = this.headerSize;
            data[4] = this.binarySegmentOffset;
            data[5] = this.binarySegmentSize;
        };
        PreHeader.NumElements = 6;
        PreHeader.NumBytes = PreHeader.NumElements * Uint32Array.BYTES_PER_ELEMENT;
        return PreHeader;
    })();
    agx.PreHeader = PreHeader;
    /**
    A serialized message for communication over socket.
    */
    var StructuredMessage = (function () {
        function StructuredMessage() {
        }
        StructuredMessage.prototype.setMessageId = function (id) {
            this.preHeader.id = id;
            this.preHeader.writeToPacket(this.packet);
        };
        /**
        Build a structued message from a header + (optional) binary segment
        */
        StructuredMessage.BuildMessage = function (uri, header, binarySegments) {
            var message = new StructuredMessage();
            message.uri = uri;
            message.header = agx.IsDefined(header) ? header : null;
            message.headerString = JSON.stringify(message.header);
            // Calculate pre header
            message.preHeader = new PreHeader();
            var preHeader = message.preHeader;
            preHeader.uriSize = message.uri.length;
            preHeader.headerSize = message.headerString.length; // Note, this will fail for non-ASCII characters.
            if (binarySegments != undefined) {
                for (var i = 0; i < binarySegments.length; ++i)
                    preHeader.binarySegmentSize += binarySegments[i].byteLength;
            }
            var headerEnd = PreHeader.NumBytes + preHeader.uriSize + preHeader.headerSize;
            preHeader.binarySegmentOffset = agx.AlignCeil(headerEnd, StructuredMessage.BinarySegmentAlignment);
            // Create packet
            message.packet = new ArrayBuffer(preHeader.getPacketSize());
            // Write pre header
            preHeader.writeToPacket(message.packet);
            // Write URI
            var uriView = new Uint8Array(message.packet, PreHeader.NumBytes, preHeader.uriSize);
            agx.WriteStringToArray(uriView, message.uri);
            // Write header
            var headerView = new Uint8Array(message.packet, PreHeader.NumBytes + preHeader.uriSize, preHeader.headerSize);
            agx.WriteStringToArray(headerView, message.headerString);
            // Write binary segment
            if (binarySegments != undefined) {
                message.binarySegment = new Uint8Array(message.packet, preHeader.binarySegmentOffset, preHeader.binarySegmentSize);
                var currentIndex = 0;
                for (var i = 0; i < binarySegments.length; ++i) {
                    var segment = binarySegments[i];
                    var byteArray = new Uint8Array(segment.buffer, segment.byteOffset, segment.byteLength);
                    for (var j = 0; j < byteArray.length; ++j)
                        message.binarySegment[currentIndex++] = byteArray[j];
                }
                agx.Assert(currentIndex == preHeader.binarySegmentSize);
            }
            return message;
        };
        /**
        Parse a structured message from a received binary packet
        */
        StructuredMessage.ParseMessage = function (packet) {
            var message = new StructuredMessage();
            message.packet = packet;
            message.preHeader = PreHeader.CreateFromPacket(packet);
            var preHeader = message.preHeader;
            if (preHeader.getPacketSize() != packet.byteLength)
                throw "Invalid packet: PreHeader says it should be " + preHeader.getPacketSize() + " bytes, but actual data is " + packet.byteLength + " bytes";
            var uriView = new Uint8Array(packet, PreHeader.NumBytes, preHeader.uriSize);
            message.uri = agx.ExtractStringFromArray(uriView);
            var headerView = new Uint8Array(packet, PreHeader.NumBytes + preHeader.uriSize, preHeader.headerSize);
            message.headerString = agx.ExtractStringFromArray(headerView);
            message.header = JSON.parse(message.headerString);
            message.binarySegment = new Uint8Array(packet, preHeader.binarySegmentOffset, preHeader.binarySegmentSize);
            // console.log("Parsed message " + message.uri + " with " + preHeader.headerSize + " character header and " + preHeader.binarySegmentSize + " bytes binary segment");
            return message;
        };
        /**
        Extract a buffer from the message.
        */
        StructuredMessage.prototype.extractBuffer = function (bufferHeader) {
            return agx.ExtractBuffer(bufferHeader, this.binarySegment);
            /**
            agx.AssertDefined(bufferHeader);
            agx.AssertDefined(bufferHeader.name);
            agx.AssertDefined(bufferHeader.type);
            agx.AssertDefined(bufferHeader.numElements);
            agx.AssertDefined(bufferHeader.numBytes);
            agx.AssertDefined(bufferHeader.byteOffset);
      
            var format = agx.GetFormat(bufferHeader.type);
      
            if (bufferHeader.isPartial != undefined && bufferHeader.isPartial)
              throw 'Buffer \'' + bufferHeader.name + '\' is partial!';
      
            if (bufferHeader.customSerialization != undefined && bufferHeader.customSerialization)
              throw 'Buffer \'' + bufferHeader.name + '\' is custom!';
              // return type.customParser(bufferHeader, data, binarySegmentOffset);
            
            var byteOffset = this.preHeader.binarySegmentOffset + bufferHeader.byteOffset;
            agx.Assert(bufferHeader.numElements * format.numBytes == bufferHeader.numBytes);
            
            
            var array = new format.arrayType(this.packet, byteOffset, bufferHeader.numElements * format.numElements);
            array.format = format;
            array.numElements = bufferHeader.numElements;
      
            return array;
            */
        };
        StructuredMessage.BinarySegmentAlignment = 32;
        return StructuredMessage;
    })();
    agx.StructuredMessage = StructuredMessage;
    var StructuredMessagePromise = (function () {
        function StructuredMessagePromise(message) {
            this.message = message;
        }
        StructuredMessagePromise.prototype.success = function (callback) {
            this.onSuccessCallback = callback;
        };
        StructuredMessagePromise.prototype.signalSuccess = function () {
            if (agx.IsDefined(this.onSuccessCallback))
                this.onSuccessCallback(this.message);
        };
        return StructuredMessagePromise;
    })();
    agx.StructuredMessagePromise = StructuredMessagePromise;
})(agx || (agx = {}));
var agx;
(function (agx) {
    /**
    Timer class for measuring elapsed time.
    */
    var Timer = (function () {
        /**
        Constructor
        */
        function Timer(startImmediately) {
            if (startImmediately === void 0) { startImmediately = false; }
            this.reset(startImmediately);
        }
        /**
        Start the timer.
        */
        Timer.prototype.start = function () {
            if (this.running)
                return;
            this.startTime = performance.now();
            this.running = true;
        };
        /**
        Stop the timer.
        \return The total measured time
        */
        Timer.prototype.stop = function () {
            if (!this.running)
                return;
            var currentTime = performance.now();
            this.totalTime += currentTime - this.startTime;
            this.running = false;
            return this.totalTime;
        };
        /**
        Reset the timer
        \param startAfterReset Restart the timer if true
        \return The total measured time
        */
        Timer.prototype.reset = function (startAfterReset) {
            if (startAfterReset === void 0) { startAfterReset = false; }
            var totalTime = this.getCurrentTime();
            this.running = false;
            this.startTime = 0;
            this.totalTime = 0;
            if (startAfterReset)
                this.start();
            return totalTime;
        };
        /**
        \return The measured time in milliseconds.
        */
        Timer.prototype.getTime = function () {
            return this.totalTime;
        };
        /**
        \return The elapsed time in milliseconds.
        */
        Timer.prototype.getCurrentTime = function () {
            return this.running ? (performance.now() - this.startTime) + this.totalTime : this.getTime();
        };
        return Timer;
    })();
    agx.Timer = Timer;
    ;
})(agx || (agx = {}));
/// <reference path="Frame.ts"/>
/// <reference path="Math.ts"/>
/// <reference path="Timer.ts"/>
var agx;
(function (agx) {
    var plot;
    (function (plot) {
        /**
        A data curve model. Does not know anything about rendering.
        */
        var DataCurve = (function () {
            function DataCurve(id, title) {
                if (title === void 0) { title = ""; }
                this.id = id;
                this.title = title;
                this.values = null;
                this.enabled = true;
                this.color = "#" + Math.random().toString(16).slice(2, 8);
                this.lineWidth = 1;
                this.lineType = 0;
                this.symbol = 0;
                this.xTitle = "";
                this.xUnit = "";
                this.xIsLogarithmic = false;
                this.yTitle = "";
                this.yUnit = "";
                this.yIsLogarithmic = false;
            }
            DataCurve.prototype.appendDataPoint = function (x, y) {
                if (!this.hasData()) {
                    this.xMin = x;
                    this.xMax = x;
                    this.yMin = y;
                    this.yMax = y;
                    this.values = [];
                }
                // console.log('appendDataPoint: ' + x + ', ' + y);
                if (this.xMin > x)
                    this.xMin = x;
                if (this.xMax < x)
                    this.xMax = x;
                if (this.yMin > y)
                    this.yMin = y;
                if (this.yMax < y)
                    this.yMax = y;
                this.values.push([x, y]);
            };
            DataCurve.prototype.hasData = function () {
                return this.values != null;
            };
            DataCurve.prototype.setColor = function (r, g, b, a) {
                var rStr = ("00" + Math.round(r * 255).toString(16)).substr(-2);
                var gStr = ("00" + Math.round(g * 255).toString(16)).substr(-2);
                var bStr = ("00" + Math.round(b * 255).toString(16)).substr(-2);
                this.color = "#" + rStr + gStr + bStr;
            };
            return DataCurve;
        })();
        plot.DataCurve = DataCurve;
        //////////////////////////////////////////////////////////////////////////////////////////////////
        /**
        Abstract plotting window.
        */
        var Window = (function () {
            function Window(name, id) {
                if (name === void 0) { name = ""; }
                this.maxRedrawFrequency = 30;
                this.hasRedrawRequest = false;
                this.isThrottled = false;
                if (agx.IsDefined(id)) {
                    // agx.Assert(id >= Window.idCounter);
                    this.id = id;
                    Window.idCounter = id + 1;
                }
                else {
                    this.id = Window.idCounter++;
                }
                this.name = name;
                this.savedName = null;
                this.curves = [];
                this.curveIdCounter = 0;
            }
            /**
            Add a curve to the plot window.
            */
            Window.prototype.addCurve = function (curve) {
                if (curve.id < 0)
                    curve.id = this.curveIdCounter++;
                var defaultColors = ["#edc240", "#afd8f8", "#cb4b4b", "#4da74d", "#9440ed"];
                if (curve.id < defaultColors.length)
                    curve.color = defaultColors[curve.id];
                this.curves.push(curve);
            };
            /**
            Remove the given curve from the window.
            \return True if the curve was found and removed. False otherwise.
            */
            Window.prototype.removeCurve = function (curve) {
                for (var i = 0; i < this.curves.length; ++i) {
                    if (this.curves[i] == curve) {
                        this.curves.splice(i, 1);
                        curve.id = -1;
                        return true;
                    }
                }
                return false;
            };
            /**
            \return The curve with the specified id.
            */
            Window.prototype.getCurve = function (id) {
                for (var i = 0; i < this.curves.length; ++i) {
                    if (this.curves[i].id == id)
                        return this.curves[i];
                }
                return null;
            };
            /**
            Request the plot to redraw itself. Throttled to specified max frequency.
            */
            Window.prototype.requestRedraw = function () {
                var _this = this;
                this.hasRedrawRequest = true;
                if (this.isThrottled)
                    return;
                this.redraw(); // Actual drawing
                this.hasRedrawRequest = false;
                this.isThrottled = true;
                window.setTimeout(function () {
                    _this.isThrottled = false;
                    if (_this.hasRedrawRequest)
                        _this.requestRedraw();
                }, 1000 / this.maxRedrawFrequency);
            };
            /////////////////////////////////////////////
            // Always use requestRedraw
            Window.prototype.redraw = function () {
                agx.Abort("Must be overridden in subclass");
            };
            Window.idCounter = 0;
            return Window;
        })();
        plot.Window = Window;
        var FlotCurve = (function () {
            function FlotCurve(curve, xaxis) {
                this.yRange = { min: undefined, max: undefined };
                if (agx.IsDefined(curve) && curve.values && curve.values.length > 0) {
                    var xMin = (agx.IsDefined(xaxis) && xaxis.min) ? xaxis.min : curve.values[0][0];
                    var xMax = (agx.IsDefined(xaxis) && xaxis.max) ? xaxis.max : curve.values[curve.values.length - 1][0];
                    this.label = curve.title;
                    this.color = curve.color;
                    this.data = curve.values;
                }
            }
            return FlotCurve;
        })();
        plot.FlotCurve = FlotCurve;
        /**
        A representation of a Flot instance. It is created inside a given HTML
        div. The window holds a number of curves that can be drawn in the plot
        view.
        */
        var FlotWindow = (function (_super) {
            __extends(FlotWindow, _super);
            /**
            Create a new, empty, plot window in the given HTML div.
            */
            function FlotWindow(targetDiv, name, id) {
                var _this = this;
                if (name === void 0) { name = ""; }
                _super.call(this, name, id);
                this.curves = [];
                this.div = targetDiv;
                this.rightButtonDown = false;
                this.xMarkerDiv = null;
                this.xMarker = null;
                this.plotOptions = {
                    legend: {
                        show: false
                    },
                    series: {
                        // shadowSize: 0, // drawing is faster without shadows
                        points: {
                            show: false,
                            radius: 0.5
                        },
                        lines: {
                            show: true
                        }
                    },
                    // crosshair: {
                    //   mode: "x"
                    // },
                    // zoom: {
                    //   interactive: true
                    // },
                    selection: { mode: "xy" },
                    grid: {
                        hoverable: true,
                        clickable: false,
                        autoHighlight: false
                    },
                    xaxis: {
                        zoomRange: [null, null],
                        panRange: [null, null],
                        min: null,
                        max: null,
                        tickFormatter: function (val, axis) { return agx.toReadableNumber(val) + " s"; }
                    },
                    yaxis: {
                        zoomRange: [null, null],
                        panRange: [null, null],
                        min: null,
                        max: null,
                        tickFormatter: function (val, axis) { return agx.toReadableNumber(val); }
                    }
                };
                this.detailDataRange = null;
                this.flot = $.plot(this.div, [], this.plotOptions);
                this.updateLegendTimeout = null;
                this.latestPosition = null;
                targetDiv.bind("plothover", function (event, pos, item) {
                    _this.latestPosition = pos;
                    if (!_this.updateLegendTimeout)
                        _this.updateLegendTimeout = setTimeout(function () { _this.updateLegend(); }, 50);
                });
                targetDiv.bind("mouseleave", function () {
                    $("#toolTip").hide();
                });
                targetDiv.bind("plotselected", function (event, ranges) {
                    // console.log(ranges);
                    if (_this.curves.length > 0) {
                        _this.plotOptions.xaxis.min = ranges.xaxis.from;
                        _this.plotOptions.xaxis.max = ranges.xaxis.to;
                        _this.plotOptions.yaxis.min = ranges.yaxis.from;
                        _this.plotOptions.yaxis.max = ranges.yaxis.to;
                        if (_this.zoomCallback) {
                            _this.resetPan(_this.plotOptions.xaxis);
                            _this.zoomCallback(_this.plotOptions.xaxis);
                        }
                    }
                    _this.requestRedraw();
                });
                // targetDiv.on("drag", (event) =>
                // {
                //   console.log("drag: " + event.pageX + ", " + event.pageY);
                // });
                targetDiv.on("mousemove", function (event) {
                    if (_this.rightButtonDown) {
                        // console.log("move: " + event.pageX + ", " + event.pageY);
                        _this.flot.pan({ left: _this.prevPageX - event.pageX,
                            top: _this.prevPageY - event.pageY });
                        _this.prevPageX = event.pageX;
                        _this.prevPageY = event.pageY;
                        // this.synchronizeAxis();
                        // 
                        // var dataRange = this.getCurveDataRange();
                        // 
                        // // Clamp panning to data range
                        // if (this.plotOptions.xaxis.min < dataRange.min)
                        // {
                        //   var violation = dataRange.min - this.plotOptions.xaxis.min;
                        //   this.plotOptions.xaxis.max += violation;
                        //   this.plotOptions.xaxis.min = dataRange.min;
                        //   this.requestRedraw();
                        // }
                        // 
                        // if (this.plotOptions.xaxis.max > dataRange.max)
                        // {
                        //   var violation = this.plotOptions.xaxis.max - dataRange.max;
                        //   this.plotOptions.xaxis.min -= violation;
                        //   this.plotOptions.xaxis.max = dataRange.max;
                        //   this.requestRedraw();
                        // }
                        if (_this.xMarker)
                            _this.drawMarker(_this.xMarker);
                    }
                });
                targetDiv.on("mousedown", function (event) {
                    // console.log("down which: " + event.which);
                    if (event.which == 3)
                        _this.rightButtonDown = true;
                    _this.prevPageX = event.pageX;
                    _this.prevPageY = event.pageY;
                    event.preventDefault();
                });
                targetDiv.on("mouseup", function (event) {
                    // console.log("up which: " + event.which);
                    if (event.which == 3) {
                        _this.rightButtonDown = false;
                        if (_this.zoomCallback) {
                            _this.synchronizeAxis();
                            var range = _this.updateDetailRange(_this.plotOptions.xaxis);
                            if (range)
                                _this.zoomCallback(range);
                        }
                    }
                    event.preventDefault();
                });
                targetDiv.on("mouseleave", function (event) {
                    _this.rightButtonDown = false;
                });
                targetDiv.on("contextmenu", function (event) {
                    event.preventDefault();
                });
            }
            FlotWindow.prototype.updateLegend = function () {
                return;
                this.updateLegendTimeout = null;
                var pos = this.latestPosition;
                var axes = this.flot.getAxes();
                if (pos.x < axes.xaxis.min || pos.x > axes.xaxis.max ||
                    pos.y < axes.yaxis.min || pos.y > axes.yaxis.max)
                    return;
                var i, j, dataset = this.flot.getData();
                var x, y, series, distance, found = false;
                for (i = 0; i < dataset.length; ++i) {
                    var locseries = dataset[i];
                    // Find the nearest points, x-wise.
                    for (j = 0; j < locseries.data.length; ++j)
                        if (locseries.data[j][0] > pos.x)
                            break;
                    // Interpolate.
                    var locx, locy, p1 = locseries.data[j - 1], p2 = locseries.data[j];
                    if (p1 == null) {
                        locx = p2[0];
                        locy = p2[1];
                    }
                    else if (p2 == null) {
                        locx = p1[0];
                        locy = p1[1];
                    }
                    else {
                        locx = pos.x;
                        locy = p1[1] + (p2[1] - p1[1]) * (pos.x - p1[0]) / (p2[0] - p1[0]);
                    }
                    var locDistance;
                    if (pos.y < locy)
                        locDistance = Math.abs(locy - pos.y);
                    else
                        locDistance = Math.abs(pos.y - locy);
                    if (!found) {
                        distance = locDistance;
                        x = locx;
                        y = locy;
                        series = locseries;
                        found = true;
                    }
                    else if (locDistance < distance) {
                        distance = locDistance;
                        x = locx;
                        y = locy;
                        series = locseries;
                    }
                }
                if (found) {
                    var o = this.flot.pointOffset({ x: x, y: y, xaxis: 1, yaxis: 1 });
                    o.top += this.div.offset().top - 50;
                    o.left += this.div.offset().left + 10;
                    $("#toolTip").html(series.label + "<br />x = " + agx.toReadableNumber(x) + " s<br />y = " + agx.toReadableNumber(y) + " " + series.unit)
                        .css(o)
                        .fadeIn(200);
                }
            };
            FlotWindow.prototype.synchronizeAxis = function () {
                var axis = this.flot.getAxes();
                this.plotOptions.xaxis.min = axis.xaxis.min;
                this.plotOptions.xaxis.max = axis.xaxis.max;
                this.plotOptions.yaxis.min = axis.yaxis.min;
                this.plotOptions.yaxis.max = axis.yaxis.max;
            };
            FlotWindow.prototype.printRange = function (range) {
                return range.min + ':' + range.max;
            };
            FlotWindow.prototype.updateDetailRange = function (range) {
                if (!this.detailDataRange) {
                    this.detailDataRange = { min: range.min, max: range.max };
                    return range;
                }
                // console.log('range: ' + this.printRange(range));
                // console.log('detailDataRange: ' + this.printRange(this.detailDataRange));
                // Check if inside current detail range
                if (range.min >= this.detailDataRange.min && range.max <= this.detailDataRange.max)
                    return null;
                var result = { min: range.min, max: range.max };
                // Clamp requested range and extend total detail range
                if (range.min < this.detailDataRange.min) {
                    agx.Assert(range.max > this.detailDataRange.min && range.max < this.detailDataRange.max);
                    result.max = this.detailDataRange.min;
                    this.detailDataRange.min = range.min;
                }
                else if (range.max > this.detailDataRange.max) {
                    agx.Assert(range.min < this.detailDataRange.max && range.min > this.detailDataRange.min);
                    result.min = this.detailDataRange.max;
                    this.detailDataRange.max = range.max;
                }
                else {
                    console.log('updateDetailRange failed!!');
                    console.log(range);
                    console.log(this.detailDataRange);
                    this.detailDataRange = { min: range.min, max: range.max };
                }
                // console.log('result: ' + this.printRange(result));
                // console.log('updated detailDataRange: ' + this.printRange(this.detailDataRange));
                return result;
            };
            FlotWindow.prototype.resetPan = function (range) {
                this.detailDataRange = range == undefined ? null : { min: range.min, max: range.max };
            };
            FlotWindow.prototype.drawMarker = function (xPos) {
                if (this.xMarkerDiv) {
                    this.xMarkerDiv.remove();
                    this.xMarkerDiv = null;
                }
                var plotOffset = this.flot.getPlotOffset();
                var height = this.div.height() - plotOffset.top - plotOffset.bottom;
                var canvasPoint = this.flot.p2c({ x1: xPos, y1: 0 });
                var divX = canvasPoint.left + plotOffset.left;
                // Make sure marker is within plot bounds
                if (divX >= plotOffset.left && divX <= this.div.width() - plotOffset.right) {
                    // var markerWidth = 1;
                    // var cssShadow = "rgb(100, 100, 100) -1px 0px 0px 0px";
                    // var cssGradient = "background: -moz-linear-gradient(left,  rgba(255,255,255,0) 0%, rgba(0,0,0,1) 100%);";
                    // cssGradient += "background: -webkit-gradient(linear, left top, right top, color-stop(0%,rgba(255,255,255,0)), color-stop(100%,rgba(0,0,0,1)));";
                    // cssGradient += "background: -webkit-linear-gradient(left,  rgba(255,255,255,0) 0%,rgba(0,0,0,1) 100%);";
                    // cssGradient += "background: -o-linear-gradient(left,  rgba(255,255,255,0) 0%,rgba(0,0,0,1) 100%);";
                    // cssGradient += "background: -ms-linear-gradient(left,  rgba(255,255,255,0) 0%,rgba(0,0,0,1) 100%);";
                    // cssGradient += "background: linear-gradient(to right,  rgba(255,255,255,0) 0%,rgba(0,0,0,1) 100%);";
                    // this.xMarkerDiv = $("<div style=\"width:" + markerWidth + "px; height:" + height + "px; " + cssGradient + "position:absolute; left:" + divX + "px;top:" + plotOffset.top + "px;\"><div style=\"width:2px; height:" + height + "px; background:rgb(100,100,100); position:absolute; left:" + (markerWidth-1) + "px;top:0px;\"></div></div>");
                    this.xMarkerDiv = $("<div style=\"width:1px; height:" + height + "px; background:red; position:absolute; left:" + divX + "px;top:" + plotOffset.top + "px;\"></div>");
                    this.div.append(this.xMarkerDiv);
                }
                this.xMarker = xPos;
            };
            ////////////////////////////////////////////////////////////////////////////
            // Private: use requestRedraw
            FlotWindow.prototype.redraw = function () {
                var totalTime = 0;
                this.activeCurves = new Array();
                var yRange = {};
                var xAxisTitle = "";
                var diffXAxisTitle = false;
                var xAxisUnit = "";
                var diffXAxisUnit = false;
                var xAxisLog = false;
                var diffXAxisLog = false;
                var yAxisLog = false;
                var diffYAxisLog = false;
                var yAxisTitle = "";
                var diffYAxisTitle = false;
                var yAxisUnit = "";
                var diffYAxisUnit = false;
                var first = true;
                var diffWidths = false;
                var width = 1;
                var diffLineType = false;
                var lineType = 0;
                var diffSymbols = false;
                var symbol = 0;
                var xMin = 0;
                var xMax = 0;
                var yMin = 0;
                var yMax = 0;
                // Setup plot options
                for (var i = 0; i < this.curves.length; ++i) {
                    var curve = this.curves[i];
                    if (curve.enabled) {
                        if (first) {
                            width = curve.lineWidth;
                            lineType = curve.lineType;
                            symbol = curve.symbol;
                            xAxisTitle = curve.xTitle;
                            xAxisUnit = curve.xUnit;
                            xAxisLog = curve.xIsLogarithmic;
                            yAxisTitle = curve.yTitle;
                            yAxisUnit = curve.yUnit;
                            yAxisLog = curve.yIsLogarithmic;
                            xMin = curve.xMin;
                            xMax = curve.xMax;
                            yMin = curve.yMin;
                            yMax = curve.yMax;
                            first = false;
                        }
                        else {
                            if (!diffWidths && Math.abs(width - curve.lineWidth) > 0.001) {
                                width = 1;
                                diffWidths = true;
                            }
                            if (!diffLineType && lineType != curve.lineType) {
                                lineType = 0;
                            }
                            if (!diffSymbols && symbol != curve.symbol) {
                                symbol = 3;
                                diffSymbols = true;
                            }
                            if (!diffXAxisTitle && xAxisTitle != curve.xTitle) {
                                xAxisTitle = "";
                                diffXAxisTitle = true;
                            }
                            if (!diffXAxisUnit && xAxisUnit != curve.xUnit) {
                                xAxisUnit = "";
                                diffXAxisUnit = true;
                            }
                            if (!diffXAxisLog && xAxisLog != curve.xIsLogarithmic) {
                                xAxisLog = false;
                                diffXAxisLog = true;
                            }
                            if (!diffYAxisTitle && yAxisTitle != curve.yTitle) {
                                yAxisTitle = "";
                                diffYAxisTitle = true;
                            }
                            if (!diffYAxisUnit && yAxisUnit != curve.yUnit) {
                                yAxisUnit = "";
                                diffYAxisUnit = true;
                            }
                            if (!diffYAxisLog && yAxisLog != curve.yIsLogarithmic) {
                                yAxisLog = false;
                                diffYAxisLog = true;
                            }
                            if (curve.xMin < xMin) {
                                xMin = curve.xMin;
                            }
                            if (curve.xMax > xMax) {
                                xMax = curve.xMax;
                            }
                            if (curve.yMin < yMin) {
                                yMin = curve.yMin;
                            }
                            if (curve.yMax > yMax) {
                                yMax = curve.yMax;
                            }
                        }
                    }
                }
                var funX = null;
                var iFunX = null;
                var funY = null;
                var iFunY = null;
                var xTicks = 6;
                var yTicks = 6;
                if (xAxisLog) {
                    funX = function (v) { if (v == 0)
                        return null; return Math.log(v); };
                    iFunX = function (v) { return Math.exp(v); };
                    var diff = xMax - xMin;
                    var lnDiff = Math.log(diff);
                    var lnStep = lnDiff / 5.0;
                    xTicks = [xMin, xMin + Math.exp(lnStep * 1), xMin + Math.exp(lnStep * 2), xMin + Math.exp(lnStep * 3), xMin + Math.exp(lnStep * 4), xMax];
                }
                if (yAxisLog) {
                    funY = function (v) { if (v == 0)
                        return null; return Math.log(v); };
                    iFunY = function (v) { return Math.exp(v); };
                    var diff = yMax - yMin;
                    var lnDiff = Math.log(diff);
                    var lnStep = lnDiff / 5.0;
                    yTicks = [yMin, yMin + Math.exp(lnStep * 1), yMin + Math.exp(lnStep * 2), yMin + Math.exp(lnStep * 3), yMin + Math.exp(lnStep * 4), yMax];
                }
                var labelX;
                if (xAxisTitle == "") {
                    labelX = xAxisUnit;
                }
                else {
                    if (xAxisUnit == "") {
                        labelX = xAxisTitle;
                    }
                    else {
                        labelX = xAxisTitle + "(" + xAxisUnit + ")";
                    }
                }
                var labelY;
                if (yAxisTitle == "") {
                    labelY = yAxisUnit;
                }
                else {
                    if (yAxisUnit == "") {
                        labelY = yAxisTitle;
                    }
                    else {
                        labelY = yAxisTitle + "(" + yAxisUnit + ")";
                    }
                }
                this.plotOptions = {
                    legend: {
                        show: true,
                        position: "ne"
                    },
                    series: {
                        // shadowSize: 0, // drawing is faster without shadows
                        points: {
                            show: symbol != 3,
                            radius: width
                        },
                        lines: {
                            show: lineType != 2,
                            lineWidth: width
                        }
                    },
                    // crosshair: {
                    //   mode: "x"
                    // },
                    // zoom: {
                    //   interactive: true
                    // },
                    selection: { mode: "xy" },
                    grid: {
                        hoverable: true,
                        clickable: false,
                        autoHighlight: false
                    },
                    xaxis: {
                        zoomRange: [null, null],
                        panRange: [null, null],
                        min: null,
                        max: null,
                        tickFormatter: function (val, axis) {
                            return agx.toReadableNumber(val) + " " + xAxisUnit;
                        },
                        ticks: xTicks,
                        transform: funX,
                        inverseTransform: iFunX,
                        axisLabel: labelX,
                        axisLabelUseCanvas: true
                    },
                    yaxis: {
                        zoomRange: [null, null],
                        panRange: [null, null],
                        min: null,
                        max: null,
                        tickFormatter: function (val, axis) {
                            return agx.toReadableNumber(val) + " " + yAxisUnit;
                        },
                        ticks: yTicks,
                        transform: funY,
                        inverseTransform: iFunY,
                        axisLabel: labelY,
                        axisLabelUseCanvas: true
                    }
                };
                for (var i = 0; i < this.curves.length; ++i) {
                    var curve = this.curves[i];
                    if (curve.enabled) {
                        var flotCurve = new FlotCurve(curve, this.plotOptions.xaxis);
                        this.activeCurves.push(flotCurve);
                    }
                }
                // Explicity handle yRange (for xMarker to work properly)
                // var yAxis = this.plotOptions.yaxis;
                // var explicitAxis = false;
                // if (!yAxis.min && !yAxis.max)
                // {
                //   explicitAxis = true;
                //   var rangeLength = yRange.max - yRange.min;
                //   var margin = rangeLength * 0.1;
                //   yRange.min -= margin;
                //   yRange.max += margin;
                //   yAxis.min = yRange.min;
                //   yAxis.max = yRange.max;
                // }
                this.flot = $.plot(this.div, this.activeCurves, this.plotOptions);
                if (this.xMarker)
                    this.drawMarker(this.xMarker);
                // if (explicitAxis)
                // {
                //   yAxis.min = null;
                //   yAxis.max = null;
                // }
            };
            return FlotWindow;
        })(agx.plot.Window);
        plot.FlotWindow = FlotWindow;
    })(plot = agx.plot || (agx.plot = {}));
})(agx || (agx = {}));
var agx;
(function (agx) {
    var treeview;
    (function (treeview) {
        /**
        A factory for tree views. Pass a TreeSpecificationNode root and a HTML div to
        the 'Instantiate' method and it will produce a tree view.
        */
        var Builder = (function () {
            function Builder() {
            }
            Builder.Instantiate = function (treeRoot, htmlDiv) {
                var domTree = $('<ul>');
                domTree.addClass('filetree');
                domTree.treeview({
                    collapsed: true,
                    animated: "fast"
                });
                Builder.TraverseTreeSpecification(treeRoot, domTree, "");
                domTree.treeview({
                    collapsed: true,
                    animated: "fast"
                });
                // Fix the inverted signs.
                $(".open", domTree).removeClass("expandable").addClass("collapsable").removeClass("lastExpandable").addClass("lastCollapsable");
                $(".open-hitarea", domTree).removeClass("expandable-hitarea").addClass("collapsable-hitarea").removeClass("lastExpandable-hitarea").addClass("lastCollapsable-hitarea");
                $(htmlDiv).html(domTree);
                return domTree;
            };
            /**
          
            Recursive function that walks down the given TreeSpecificationNode and
            produces a 'ul'/'li' HTML tree augmented according to the
            TreeSpecificationNode and the rules of the JavaScript TreeView library.
          
            */
            Builder.TraverseTreeSpecification = function (inputNode, outputNode, parentPath) {
                if (parentPath === void 0) { parentPath = ""; }
                var numChildren = inputNode.getNumChildren();
                var haveChildren = numChildren > 0;
                // Construct the current path.
                if (parentPath == '') {
                    var path = inputNode.getName();
                }
                else {
                    var path = parentPath + '.' + inputNode.getName();
                }
                // Create the 'li' element representing the current tree specification node.
                var listElement = $('<li>');
                outputNode.append(listElement);
                listElement.attr('name', inputNode.getName());
                inputNode.setTreeViewNode(listElement);
                // Set the id of the 'li' node.
                if (inputNode.getId()) {
                    listElement.attr('id', inputNode.getId());
                }
                // Add any custom classes to the 'li' node, which is the root of the entire subtree.
                for (var i = 0; i < inputNode.getNumSubtreeClasses(); ++i) {
                    listElement.addClass(inputNode.getSubtreeClass(i));
                }
                // Add any custom attributes to the 'li' node, which is the root of the entire subtree.
                for (var i = 0; i < inputNode.getNumSubtreeAttributes(); ++i) {
                    var attribute = inputNode.getSubtreeAttributeByIndex(i);
                    listElement.attr(attribute.key, attribute.value);
                }
                // Add the name of the node. Leaf nodes need some extra stuff, which is added here as well.
                var titleHolder = $('<span>');
                if (inputNode.getCallback()) {
                    var clickable = $('<a>');
                    listElement.append(clickable);
                    clickable.append(titleHolder);
                }
                else {
                    listElement.append(titleHolder);
                }
                if (inputNode.getTitle()) {
                    titleHolder.append(inputNode.getTitle());
                }
                else {
                    titleHolder.append(inputNode.getName());
                }
                // Add any custom attributes to the 'span' node.
                for (var i = 0; i < inputNode.getNumAttributes(); ++i) {
                    var attribute = inputNode.getAttributeByIndex(i);
                    titleHolder.attr(attribute.key, attribute.value);
                }
                // Add any custom classes to the 'span' node, which is not shared by the subtree.
                for (var i = 0; i < inputNode.getNumClasses(); ++i) {
                    titleHolder.addClass(inputNode.getClass(i));
                }
                // Set the appropriate icon, and traverse into any children.
                if (haveChildren) {
                    // The node has children, so let's make a subtree.
                    var subtreeRoot = $('<ul>');
                    listElement.append(subtreeRoot);
                    // Add the children to the subtree.
                    for (var i = 0; i < numChildren; ++i) {
                        Builder.TraverseTreeSpecification(inputNode.getChild(i), subtreeRoot, path);
                    }
                    // Add icon.
                    titleHolder.addClass('folder');
                }
                else if (inputNode.getUseIcon()) {
                    // No children, add icon.
                    titleHolder.addClass('file');
                }
                // Add callback.
                if (inputNode.getCallback()) {
                    (function (callback, path, sourceNode) {
                        $(clickable).click(function () {
                            callback(path, sourceNode);
                            return true;
                        });
                    })(inputNode.getCallback(), path, inputNode);
                }
            };
            return Builder;
        })();
        treeview.Builder = Builder;
        /**
        Tree description class. The user describe the tree that is to be created using
        a connected set of these nodes.
        */
        var TreeSpecificationNode = (function () {
            function TreeSpecificationNode(name, title, id) {
                if (title === void 0) { title = name; }
                if (id === void 0) { id = null; }
                this.name = name;
                this.title = title;
                this.id = id;
                this.attributes = new Array();
                this.subtreeAttributes = new Array();
                this.classes = new Array();
                this.subtreeClasses = new Array();
                this.callback = null;
                this.parent = null;
                this.children = new Array();
                this.treeViewNode = null;
                this.customData = null;
                this.useIcon = true;
            }
            TreeSpecificationNode.prototype.getName = function () {
                return this.name;
            };
            TreeSpecificationNode.prototype.getTitle = function () {
                return this.title;
            };
            TreeSpecificationNode.prototype.setTitle = function (title) {
                this.title = title;
            };
            TreeSpecificationNode.prototype.getId = function () {
                return this.id;
            };
            /// Add an HTML attribute that is seen by the current node only. Will not be
            /// part of the sub tree rooted at the current node.
            TreeSpecificationNode.prototype.addAttribute = function (key, value) {
                this.attributes.push(new TreeNodeAttribute(key, value));
            };
            /// /return The number of node-specific attributes.
            TreeSpecificationNode.prototype.getNumAttributes = function () {
                return this.attributes.length;
            };
            /// \return The node-specific attribute on the given index, or null if index is out of range.
            TreeSpecificationNode.prototype.getAttributeByIndex = function (index) {
                if (index >= this.attributes.length || index < 0)
                    return null;
                return this.attributes[index];
            };
            /// \return The node-specific attribute with the given key, or null if there is no such attribute.
            TreeSpecificationNode.prototype.getAttributeByKey = function (key) {
                for (var i = 0; i < this.attributes.length; ++i) {
                    if (this.attributes[i].key == key)
                        return this.attributes[i].value;
                }
                return null;
            };
            /// Add an HTML attribute that is seen by the entire subtree rooted at the current node.
            TreeSpecificationNode.prototype.addSubtreeAttribute = function (key, value) {
                this.subtreeAttributes.push(new TreeNodeAttribute(key, value));
            };
            /// \return The number of attributes assigned to the subtree rooted at the current node.
            TreeSpecificationNode.prototype.getNumSubtreeAttributes = function () {
                return this.subtreeAttributes.length;
            };
            /// \return The subtree attribute at the given index, or null if the given index is out of range.
            TreeSpecificationNode.prototype.getSubtreeAttributeByIndex = function (index) {
                if (index >= this.subtreeAttributes.length || index < 0)
                    return null;
                return this.subtreeAttributes[index];
            };
            TreeSpecificationNode.prototype.getSubtreeAttributeByKey = function (key) {
                for (var i = 0; i < this.subtreeAttributes.length; ++i) {
                    if (this.subtreeAttributes[i].key == key)
                        return this.subtreeAttributes[i].value;
                }
                return null;
            };
            /// Add an HTML class that is seen by the current node only. Will not be
            /// part of the subtree rooted at the current node.
            TreeSpecificationNode.prototype.addClass = function (newClass) {
                this.classes.push(newClass);
            };
            /// \return The number of classes this node will produce.
            TreeSpecificationNode.prototype.getNumClasses = function () {
                return this.classes.length;
            };
            /// \return The class with the given index, or null if the given index is out of range.
            TreeSpecificationNode.prototype.getClass = function (index) {
                if (index >= this.classes.length || index < 0)
                    return null;
                return this.classes[index];
            };
            /// Add an HTML class that is seen by the entire subtree rooted at the current node.
            TreeSpecificationNode.prototype.addSubtreeClass = function (newClass) {
                this.subtreeClasses.push(newClass);
            };
            TreeSpecificationNode.prototype.getNumSubtreeClasses = function () {
                return this.subtreeClasses.length;
            };
            TreeSpecificationNode.prototype.getSubtreeClass = function (index) {
                if (index >= this.subtreeClasses.length || index < 0)
                    return null;
                return this.subtreeClasses[index];
            };
            /// Make the given node a child of the current node.
            TreeSpecificationNode.prototype.addChild = function (child) {
                if (child.parent == null) {
                    this.children.push(child);
                    child.parent = this;
                }
                else {
                    return false;
                }
            };
            /// \return The child at the given index, or null if the index is out of range.k
            TreeSpecificationNode.prototype.getChild = function (index) {
                if (index >= this.children.length || index < 0)
                    return null;
                return this.children[index];
            };
            /// \return The child with the given name, or null if no such child exists.
            TreeSpecificationNode.prototype.getChildByName = function (name) {
                var numChildren = this.children.length;
                for (var i = 0; i < numChildren; ++i) {
                    if (this.children[i].name == name)
                        return this.children[i];
                }
                return null;
            };
            TreeSpecificationNode.prototype.getParent = function () {
                return this.parent;
            };
            /// \return The number of children this node has.
            TreeSpecificationNode.prototype.getNumChildren = function () {
                return this.children.length;
            };
            /// Set the callback that will be called when the current node is clicked.
            TreeSpecificationNode.prototype.setCallback = function (callback) {
                this.callback = callback;
            };
            TreeSpecificationNode.prototype.getCallback = function () {
                return this.callback;
            };
            TreeSpecificationNode.prototype.setCustomData = function (data) {
                this.customData = data;
            };
            TreeSpecificationNode.prototype.getCustomData = function () {
                return this.customData;
            };
            TreeSpecificationNode.prototype.setUseIcon = function (value) {
                this.useIcon = value;
            };
            TreeSpecificationNode.prototype.getUseIcon = function () {
                return this.useIcon;
            };
            /// Called by the tree view builder to create a mapping from the tree
            /// specification node to the HTML 'li' node created for that specification
            /// node.
            TreeSpecificationNode.prototype.setTreeViewNode = function (treeViewNode) {
                this.treeViewNode = treeViewNode;
            };
            return TreeSpecificationNode;
        })();
        treeview.TreeSpecificationNode = TreeSpecificationNode;
        var TreeNodeAttribute = (function () {
            function TreeNodeAttribute(key, value) {
                this.key = key;
                this.value = value;
            }
            return TreeNodeAttribute;
        })();
        treeview.TreeNodeAttribute = TreeNodeAttribute;
        // Main function. Run when not used as a library.
        var main = function () {
            console.log("Running through Node.js");
        };
        if (typeof require != "undefined") {
            if (require.main === module) {
                main();
            }
        }
    })(treeview = agx.treeview || (agx.treeview = {})); // Module Widget.
})(agx || (agx = {})); // Module Agx.
var agxGL;
(function (agxGL) {
    var Kernel = (function () {
        function Kernel(context) {
            this.context = context;
            this.program = context.createProgram();
            this.shaders = {};
        }
        /**
        Do not use construtor directly, since shaders are fetched async.
        The load method returns a promise containing the loaded kernel.
        */
        Kernel.load = function (context, path) {
            var kernel = new Kernel(context);
            return kernel.loadShaders(path);
        };
        Kernel.prototype.loadShaders = function (path) {
            var _this = this;
            var vertShaderPath = path + '.vert';
            var fragShaderPath = path + '.frag';
            var vertexShader = this.loadShader(vertShaderPath);
            var fragShader = this.loadShader(fragShaderPath);
            var done = Q.all([vertexShader, fragShader]).then(function () {
                _this.context.linkProgram(_this.program);
                if (!_this.context.getProgramParameter(_this.program, _this.context.LINK_STATUS))
                    throw 'Could not link shader program \'' + path + '\'';
                return _this;
            });
            return done;
        };
        Kernel.prototype.loadShader = function (path) {
            var _this = this;
            return $.ajax({
                dataType: 'text',
                mimeType: "textPlain",
                url: '/agxComponent/Read',
                data: { path: path }
            }).then(
            // Success
            function (response, textStatus, xhr) {
                console.log('Received shader: ' + path);
                return _this.initShader(path, response);
            }, 
            // Error
            function (xhr, textStatus, errorThrown) {
                console.log('Error: ' + errorThrown);
                throw errorThrown;
            });
        };
        Kernel.prototype.initShader = function (path, data) {
            var shader;
            if (path.endsWith('.frag')) {
                shader = this.context.createShader(this.context.FRAGMENT_SHADER);
                this.shaders.fragment = shader;
                this.shaders.fragment.path = path;
            }
            else if (path.endsWith('.vert')) {
                shader = this.context.createShader(this.context.VERTEX_SHADER);
                this.shaders.vertex = shader;
                this.shaders.vertex.path = path;
            }
            else {
                throw "Unknown shader type \'" + path + "\'";
            }
            this.context.shaderSource(shader, data);
            this.context.compileShader(shader);
            if (!this.context.getShaderParameter(shader, this.context.COMPILE_STATUS)) {
                console.log('Failed to compile shader ' + path);
                console.log('==== Error:');
                console.log(this.context.getShaderInfoLog(shader));
                console.log('');
                console.log('==== Source:');
                console.log(data);
                throw "Failed to compile shader " + path;
            }
            this.context.attachShader(this.program, shader);
            return shader;
        };
        return Kernel;
    })();
    agxGL.Kernel = Kernel;
})(agxGL || (agxGL = {}));
