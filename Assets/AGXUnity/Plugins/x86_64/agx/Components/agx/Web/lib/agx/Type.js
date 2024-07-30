/// <reference path="Util.ts"/>
/// <reference path="Buffer.ts"/>
/// <reference path="Value.ts"/>
/// <reference path="Object.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
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
