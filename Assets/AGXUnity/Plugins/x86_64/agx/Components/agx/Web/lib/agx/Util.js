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
