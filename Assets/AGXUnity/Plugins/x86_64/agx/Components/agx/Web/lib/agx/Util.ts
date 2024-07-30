/// <reference path="Type.ts"/>

declare var $ : any;
declare var requestAnimFrame;
declare var escape;
declare var unescape;

module agx
{
  /**
  Test if an object is a string.
  */
  export function isString(s : any) : boolean
  {
    return typeof(s) === 'string' || s instanceof String;
  }


  export function startswith(str : string, prefix : string) : boolean
  {
    return str.length >= prefix.length && str.substring(0, prefix.length) == prefix;
  }

  export function endswith(str : string, suffix : string) : boolean
  {
    return str.length >= suffix.length && str.substr(str.length - suffix.length) == suffix;
  }

  export function capitalize(str : string) : string
  {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }
  
  export function encodeUTF8(s) {
    return unescape(encodeURIComponent(s));
  }

  export function decodeUTF8(s) {
    return decodeURIComponent(escape(s));
  }

  /**
  Extract a string from an array buffer

  See http://wiki.whatwg.org/wiki/StringEncoding
  and http://updates.html5rocks.com/2012/06/How-to-convert-ArrayBuffer-to-and-from-String
  */
  export function ExtractStringFromArray(array) : string
  {
    var headerString = '';
    for (var i = 0; i < array.length; ++i )
      headerString = headerString + String.fromCharCode( array[i] );
    return headerString;

    // return String.fromCharCode.apply(null, array);
    /**
    Doesn't work on Chrome 16.0.912.77 under Linux:
    Uncaught TypeError: Function.prototype.apply: Arguments list has wrong type
    May be that fromCharCode requires UInt16Array and arrayView is UInt8Array. Not sure.
    return String.fromCharCode.apply(null, arrayView);
    */
  }

  /**
  Write a string to an array.
  */
  export function WriteStringToArray(array, str : string)
  {
    var strLen = str.length;
    for ( var i = 0; i < strLen; i++ )
    array[i] = str.charCodeAt(i);
  }

  /**
  Create a WebSocket
  */
  export function CreateWebSocket(url : string, protocol : string = "agx_stream") : WebSocket
  {
    var socket = new WebSocket(url, protocol);
    socket.binaryType = "arraybuffer";
    return socket;
  }

  /**
  Verifies that a specified element is not undefined.
  */
  export function AssertDefined(element)
  {
    if ( IsUndefined(element) )
      throw "AssertDefined failed!";
  }

  export function IsDefined(element)
  {
    return typeof element !== 'undefined';
  }

  export function IsUndefined(element)
  {
    return typeof element === 'undefined';
  }

  /**
  Assert implementation. NOTE: message is always constructed even if assertion is valid (performance overhead)
  */
  export function Assert(expression : any, message? : string)
  {
    if (!expression)
      throw (message != undefined ? message : "Assert failed");
  }

  /**
  Abort implementation.
  */
  export function Abort(message? : string)
  {
     throw (message != undefined ? message : "Abort!");
  }

  /**
  Extract a serialized buffer.
  */
  export function ExtractBuffer(header : any, binarySegment : Uint8Array) : Buffer
  {
    agx.AssertDefined(header.type);

    var format = agx.GetFormat(header.type);

    if (agx.IsDefined(header.isPartial) && header.isPartial)
    {
      // buffer = this.extractPartialBuffer(bufferNode, binarySegment);
      agx.Abort('TODO');
    }

    return format.extractBuffer(header, binarySegment);
  }
  
  /**
  Extract a serialized value.
  */
  export function ExtractValue(header : any, binarySegment : Uint8Array) : Value
  {
    agx.AssertDefined(header.type);

    var format = agx.GetFormat(header.type);
    return format.extractValue(header, binarySegment);
  }
  
  
  export function getQueryString(name : string) : string
  {
    var result = (new RegExp('[?&]'+encodeURIComponent(name)+'=([^&]*)')).exec(location.search);
     if(result)
        return decodeURIComponent(result[1]);
  }
}

