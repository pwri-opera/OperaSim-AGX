// Array Remove - By John Resig (MIT Licensed)
// Array.prototype.remove = function(from, to) {
//   var rest = this.slice((to || from) + 1 || this.length);
//   this.length = from < 0 ? this.length + from : from;
//   return this.push.apply(this, rest);
// };

function isString(s) {
    return typeof(s) === 'string' || s instanceof String;
}

String.prototype.endsWith = function (s) {
  return this.length >= s.length && this.substr(this.length - s.length) == s;
}


// Align a value upwards to the next multiple of alignment.
// 'value' must be non-negative and 'alignment' must be positive.
function align_ceil( value, alignment )
{
  if ( value != 0 )
    return (value + (alignment-1) - ((value-1) % alignment));
  else
    return 0;
}

function uiSetEnable(component, flag) {
  component.prop('disabled', !flag)
  
  if (flag)
    component.removeClass('ui-state-disabled');
  else
    component.addClass('ui-state-disabled');
}

// http://paulirish.com/2011/requestanimationframe-for-smart-animating/
window.requestAnimFrame = (function(){
  return  window.requestAnimationFrame       ||
          window.webkitRequestAnimationFrame ||
          window.mozRequestAnimationFrame    ||
          window.oRequestAnimationFrame      ||
          window.msRequestAnimationFrame     ||
          function( callback ){
            window.setTimeout(callback, 1000 / 60);
          };
})();



var TypeMap = {
  'Real:32bit' : { elementSize: 1, byteSize: 4, arrayType: Float32Array },
  'Real:64bit' : { elementSize: 1, byteSize: 8, arrayType: Float64Array },
  'Int:8bit' : { elementSize: 1, byteSize: 1, arrayType: Int8Array },
  'Int:16bit' : { elementSize: 1, byteSize: 2, arrayType: Int16Array },
  'Int:32bit' : { elementSize: 1, byteSize: 4, arrayType: Int32Array },
  'Int:64bit' : { elementSize: 2, byteSize: 8, arrayType: Int32Array }, // TODO: [1] Fixme, how do we handle 64bit integer arrays in javascript? :(
  'UInt:8bit' : { elementSize: 1, byteSize: 1, arrayType: Uint8Array },
  'UInt:16bit' : { elementSize: 1, byteSize: 2, arrayType: Uint16Array },
  'UInt:32bit' : { elementSize: 1, byteSize: 4, arrayType: Uint32Array },
  'UInt:64bit' : { elementSize: 2, byteSize: 8, arrayType: Uint32Array },  // TODO: [1] Fixme, how do we handle 64bit integer arrays in javascript? :(
  'Vec3:32bit' : { elementSize: 4, byteSize: 16, arrayType: Float32Array },
  'Vec3:64bit' : { elementSize: 4, byteSize: 32, arrayType: Float64Array },
  'Vec4:32bit' : { elementSize: 4, byteSize: 16, arrayType: Float32Array },
  'Vec4:64bit' : { elementSize: 4, byteSize: 32, arrayType: Float64Array },
  'AffineMatrix4x4:32bit' : { elementSize: 16, byteSize: 64, arrayType: Float32Array },
  'AffineMatrix4x4:64bit' : { elementSize: 16, byteSize: 128, arrayType: Float64Array },
  'IndexRange:32bit' : {elementSize: 2, byteSize: 8, arrayType: Uint32Array },
  'IndexRange:64bit' : {elementSize: 2, byteSize: 16, arrayType: Uint32Array }, // TODO: [1] Fixme, how do we handle 64bit integer arrays in javascript? :(
  'Name' : { elementSize: 0, byteSize: 1, arrayType: Uint8Array, customParser: parseNameBuffer}
}

/*
[1] :

JavaScript does not natively support any 64bit integer data type. All numbers
are stored in the IEEE 754-1985 double-precision floating-point format and
reads from any other array type results in, I assume, a type conversion. Since
a double-precision float cannot accurately describe all 64bit integers, there
is no practical way to implement the Int64Array or the Uint64Array.

See 
 - http://www.yaldex.com/javascript_tutorial_2/LiB0022.html
 - http://stackoverflow.com/questions/6041124/javascript-typed-arrays-64-bit-integers

*/


// Custom parser for name buffer
function parseNameBuffer(buffer, data, binarySegmentOffset) {
  
  if (buffer.children == undefined)
    throw 'Expected name buffer to contain sub-buffers';
    
  
  var subBuffers = {};
  for (var i = 0 ; i < buffer.children.length ; ++i) {
    child = buffer.children[i];
    if (child.nodeType != "Buffer")
      throw "Invalid child node";
    subBuffers[child.name] = parseFrameBuffer(child, data, binarySegmentOffset);
  }
    
  if (subBuffers.range == undefined)
    throw 'Expected sub-buffer \'range\'';

  // if (subBuffers.characters == undefined)
    // throw 'Expected sub-buffer \'characters\'';
    
  var rangeBuffer = subBuffers.range;
  var charBuffer = subBuffers.characters;
  
  var array = {};
  array.data = [];
  array.typeName = buffer.type;
  array.numElements = buffer.numElements;
  
  
  for (var i=0; i < buffer.numElements; i++) {
    var range = {begin: rangeBuffer.data[i*2], end:rangeBuffer.data[i*2+1]};
    
    var name = '';
    for (var j=range.begin; j < range.end; j++)
      name += String.fromCharCode( charBuffer.data[j] );
    
    array.data.push(name);
  };
  
  return array;
}

// Parser for generic buffer
function parseFrameBuffer(buffer, data, binarySegmentOffset) {
  // console.log( "Got a " + buffer.type + " buffer with " + buffer.numElements + " elements named " + buffer.name + "." );
  
  if (! (buffer.type in TypeMap))
    throw 'Buffer \'' + buffer.name + '\' has unknown type \'' + buffer.type + '\'';
  
  if (buffer.isPartial)
    throw 'Buffer \'' + buffer.name + '\' is partial!';
  
  var type = TypeMap[buffer.type];

  if (buffer.customSerialization != undefined)
    return type.customParser(buffer, data, binarySegmentOffset);
  
  var byteOffset = binarySegmentOffset + buffer.byteOffset;
  var length = buffer.numElements * type.elementSize;
  
  
  // The length of the Name type is dynamic.
  if (length == 0)
    length = buffer.numBytes;
    
  var array = {};
  array.data = new type.arrayType(data, byteOffset, length);
  array.typeName = buffer.type;
  array.numElements = buffer.numElements;
  return array;
}



function parseFrameNode(node, data, binarySegmentOffset)
{
  var result = {};
  
  if (node.children != undefined) {
    for (var i = 0 ; i < node.children.length ; ++i) {
      child = node.children[i];
      
      if (child.nodeType == "Buffer")
        result[child.name] = parseFrameBuffer(child, data, binarySegmentOffset);
      else if (child.nodeType == "Value")
        // throw 'TODO: parseFrameValue';
        console.log("Ignoring frame value");
      else if (child.nodeType == "Storage")
        result[child.name] = parseFrameNode(child, data, binarySegmentOffset); // TODO Storage parsing
      else if (child.nodeType == "Component")
        result[child.name] = parseFrameNode(child, data, binarySegmentOffset);
      else
        throw 'Unknown node type: ' + child.nodeType;
    }
  }
  
  
  return result;
}

function extractFrameHeader(arrayView) {

  // Ugh, JavaScript come on, must be more efficient way to build string from Uint8Array!
  var headerString = '';
  for (var i = 0 ; i < arrayView.length ; ++i )
    headerString = headerString + String.fromCharCode( arrayView[i] );
  return headerString;

  // See http://wiki.whatwg.org/wiki/StringEncoding
  // and http://updates.html5rocks.com/2012/06/How-to-convert-ArrayBuffer-to-and-from-String
  //
  // Doesn't work on Chrome 16.0.912.77 under Linux:
  //       Uncaught TypeError: Function.prototype.apply: Arguments list has wrong type
  // May be that fromCharCode requires UInt16Array and arrayView is UInt8Array. Not sure.
  // return String.fromCharCode.apply(null, arrayView);
}


function writeFrameHeader(arrayView, header) {
  var headerLength = header.length;
  for ( var i = 0 ; i < headerLength; i++ ) {
    arrayView[i] = header.charCodeAt(i);
  }
}



/*
  Given an ArrayBuffer containing an AGX Frame, this method constructs a
  hierarhical JavaScript object that mirrors the tree structure of the
  original AGX Frame object with the buffers replaced by ArrayViews looking
  into the appropriate ranges of the given ArrayBuffer's buffer segment.

  \param data ArrayBuffer containing an AGX Frame.
 */
function parseDataFrame(data) {
  
  if (!(data instanceof ArrayBuffer))
    throw 'Expected \'ArrayBuffer\', got data of type ' + typeof(frame);
  
  if (data.byteLength < 8)
    throw 'Invalid frame header';

  // Get some information about the buffer.
  var preHeader = new Uint32Array(data, 0, 6);
  var uriLength = preHeader[2];
  var headerLength = preHeader[3];
  var binarySegmentOffset = preHeader[4];
  var binarySegmentSize = preHeader[5];

  // Extract the header from the packet.
  var headerView = new Uint8Array(data, 6*4 + uriLength, headerLength);
  var headerString = extractFrameHeader(headerView);
  


  // Create a JSON object from the header.
  var headerJson = JSON.parse(headerString);

  // var prettyHeaderString = JSON.stringify(headerJson, undefined, 2);
  // console.log( "Pretty header:\n" + prettyHeaderString);
  var frame = parseFrameNode(headerJson, data, binarySegmentOffset);
  
  Object.defineProperty(frame, "timeStamp", {value : headerJson.timeStamp,  
                                             writable : false,  
                                             enumerable : false,  
                                             configurable : false});

  return frame;
}




/*
  Given a solve result object, 'buildFramePacket' creates an ArrayBuffer containing a
  complete AGX Frame packet that can be sent over the network.
  */
function buildFramePacket(solveResult) {
/*
 header size  | offset to    | size of      | pad      | header            | pad | Binary data segment (buffers/values)                  
 in bytes     | data segment | data segment |          |                   |     |                                                
 ---------------------------------------------------------------------------------------------------------------------------------
|  UInt32     |  UInt32      |  UInt32      |  UInt32  | JSON / XML string ||||||| Buffer 0 |   | Buffer 1 |   | .....| | Buffer n |
 ---------------------------------------------------------------------------------------------------------------------------------
*/

  // These constants must match those with the same name defined in agxNet/FramePacket.h.
  var PACKET_PRE_HEADER_SIZE = 4*4; // Four UInt32s.
  var BINARY_SEGMENT_ALIGNMENT = 32;  // The highest memory alignment requirement we know about.


  // Build the JSON header string
  var header = buildHeader( solveResult );
  var headerString = JSON.stringify( header );
  var headerSize = headerString.length; // Note, this will fail for non-ASCII characters.
  var binarySegmentSize = header["binarySegmentSize"];

  // Compute some packet size related sizes and allocate memory.
  var headerEnd = PACKET_PRE_HEADER_SIZE + headerSize;
  var binarySegmentStart = align_ceil( headerEnd, BINARY_SEGMENT_ALIGNMENT );
  var packetSize = binarySegmentStart + binarySegmentSize;
  var packetBuffer = new ArrayBuffer( packetSize );

  // Fill in all header data.
  var preHeaderArray = new Uint32Array( packetBuffer, 0, 4 );
  preHeaderArray[0] = headerSize;
  preHeaderArray[1] = binarySegmentStart;
  preHeaderArray[2] = binarySegmentSize;
  preHeaderArray[3] = 0; // Pad
  var headerArray = new Uint8Array( packetBuffer, 4*4, headerSize );
  writeFrameHeader(headerArray, headerString);

  // Fill in the buffer data.
  writeBuffers( packetBuffer, binarySegmentStart, solveResult, header );

  return packetBuffer;
}


/*
  Helper method that creates an AgxFrame header object from the solveResult object.
*/
function buildHeader(solveResult) {
  var headerObject = {};
  addToHeader(headerObject, solveResult);

  var binarySegmentSize = findBinarySegmentSizeAndOffsets( headerObject );
  headerObject["binarySegmentSize"] = binarySegmentSize;

  /*
    These are read by the server-side parser, but currently not used anywhere
    so they are safe to zero out for now. They can be used for order checking
    if ever needed, but everything is synchronous at the moment, so no need
    for that. Also, some of these are currently unknown browser-side.
  */
  headerObject["index"] = 0;
  headerObject["timeStamp"] = 0.0;
  headerObject["timeStep"] = 0.0;

  return headerObject;
}


/*
  Recursive helper function that traverses the 'solveResult' and creates the
  corresponding AGX Frame packet JSON header structure.
 */
function addToHeader(headerObject, solveResultNode) {
  
  // Process all child objects of the current solve result node. Note that
  // this loop may mix branch (Component) and leaf (Buffer) nodes.
  $.each(solveResultNode, function(childName, childData) {
    if ( childData.hasOwnProperty("isBuffer") ) {
      throw "Buffers -> children";
      
      if ( ! headerObject.hasOwnProperty("Buffers") ) {
        headerObject.Buffers = [];
      }
      headerObject.Buffers[ headerObject.Buffers.length ] = {
        compressed: 0,
        name: childName,
        numElements: childData.array.numElements,
        byteOffset: -1, // Offsets are left for later, when we can do the tree traversal in a known order.
        numBytes: childData.array.data.length * childData.array.data.BYTES_PER_ELEMENT,
        type: childData.array.typeName
      };
    }
    else if ( childData.hasOwnProperty("isComponent") ) {
      if ( ! headerObject.hasOwnProperty("Components") ) {
        headerObject.Components = [];
      }
      headerObject.Components[ headerObject.Components.length ] = {
        name: childName
      };
      addToHeader( headerObject.Components[headerObject.Components.length-1], childData );
    }
  });
  return headerObject;
}

/*
  Recursive helper function that traverses the header structure and
  accumulates an buffer segment offset for each buffer it encounters.
*/
function findBinarySegmentSizeAndOffsets( headerNode, bufferSizeSoFar ) {
  // We let the first found buffer start at zero.
  bufferSizeSoFar = typeof bufferSizeSoFar !== 'undefined' ? bufferSizeSoFar : 0;

  // Loop over the buffers first.
  if ( headerNode.hasOwnProperty("Buffers") ) {
    for ( var bufferIndex in headerNode.Buffers ) {
      var buffer = headerNode.Buffers[ bufferIndex ];
      buffer.byteOffset = align_ceil(bufferSizeSoFar, TypeMap[buffer.type].byteSize);
      bufferSizeSoFar += buffer.numBytes;
    }
  }

  // Then recurse into children.
  if ( headerNode.hasOwnProperty("Components") ) {
    for ( var componentIndex in headerNode.Components ) {
      var component = headerNode.Components[ componentIndex ];
      bufferSizeSoFar += findBinarySegmentSizeAndOffsets( component, bufferSizeSoFar );
    }
  }
  
  return bufferSizeSoFar;
}


/*
  Recursive function that copies buffer contents from the solve result
  structure into the buffer segment of the packet we are about to ship acroess
  the web socket according to the specifications found in the header.

*/
function writeBuffers(packetBuffer, binarySegmentStart, solveResultNode, headerNode) {

  // We write all the buffers of the current header node before descending into the children.
  if ( headerNode.hasOwnProperty("Buffers") ) {
    for ( var bufferIndex in headerNode.Buffers ) {
      // Get some data from the header object.
      var bufferHeader = headerNode.Buffers[ bufferIndex ];
      var bufferName = bufferHeader.name;
      var numElements = bufferHeader.numElements;
      var byteOffset = bufferHeader.byteOffset;
      var typeName = bufferHeader.type;

      // Get some data from the actual data object.
      var bufferArrayObject = solveResultNode[bufferName];
      var sourceArray = bufferArrayObject.array.data;
      if ( bufferArrayObject.array.numElements != numElements ) { throw "numElements doesn't match."; }
      if ( bufferArrayObject.array.typeName !=  typeName ) { throw "typeName doesn't match."; }

      // Do the copying. We use the TypeMap in order to know how to copy elements from one buffer to the other.
      var type = TypeMap[typeName];
      var packetArray = new type.arrayType( packetBuffer, binarySegmentStart+byteOffset, numElements*type.elementSize );
      packetArray.set( sourceArray );
    }
  }

  // Loop that descends into the component nodes.
  if ( headerNode.hasOwnProperty("Components") )
  {
    for ( var componentIndex in headerNode.Components )
    {
      // Get some data from the header object.
      var headerComponent = headerNode.Components[ componentIndex ];
      var componentName = headerComponent.name;

      if ( ! solveResultNode.hasOwnProperty(componentName) ) {
        throw "Found a missmatch between the solver result and the header constructed for the reply packet.\n "+
              "This means that there is a bug in the header creation code. The offending name is \'"+componentName+"\'.";
      }

      // Get some data from the actual data object.
      var solveResultComponent = solveResultNode[ componentName ];
      
      if ( ! solveResultComponent.hasOwnProperty( "isComponent" ) ) {
        throw "The component '" + componentName + "' isn't a component.";
      }

      // Descend deeper.
      writeBuffers( packetBuffer, binarySegmentStart, solveResultComponent, headerComponent );
    }
  }
}


// Build a structured message packet from a header object and a binary segment (optional)
function buildStructuredMessage(header, binarySegment) {

  // These constants must match those with the same name defined in agxNet/FramePacket.h.
  var PACKET_PRE_HEADER_SIZE = 4*4; // Four UInt32s.
  var BINARY_SEGMENT_ALIGNMENT = 32;  // The highest memory alignment requirement we know about.


  var headerString = JSON.stringify(header);
  var headerSize = headerString.length; // Note, this will fail for non-ASCII characters.
  
  binarySegmentSize = 0;
  if (binarySegment != undefined)
    binarySegmentSize = binarySegment.byteLength;

  // Compute some packet size related sizes and allocate memory.
  var headerEnd = PACKET_PRE_HEADER_SIZE + headerSize;
  var binarySegmentStart = align_ceil( headerEnd, BINARY_SEGMENT_ALIGNMENT );
  var packetSize = binarySegmentStart + binarySegmentSize;
  var packet = new ArrayBuffer( packetSize );

  // Fill in all header data.
  var preHeaderArray = new Uint32Array( packet, 0, 4 );
  preHeaderArray[0] = headerSize;
  preHeaderArray[1] = binarySegmentStart;
  preHeaderArray[2] = binarySegmentSize;
  preHeaderArray[3] = 0; // Pad
  var headerArray = new Uint8Array( packet, 4*4, headerSize );
  writeFrameHeader(headerArray, headerString);
  
  // Copy binary segment
  // TODO
  if (binarySegment != undefined)
    throw "TODO: Handle binary segment";
  

  return packet;
}

// Parse a structured message into a header and a binary segment
function parseStructuredMessage(packet) {
  if (!(packet instanceof ArrayBuffer))
    throw 'Expected \'ArrayBuffer\', got data of type ' + typeof(frame);
  
  if (packet.byteLength < 8)
    throw 'Invalid frame header';

  // Get some information about the buffer.
  var preHeader = new Uint32Array(packet, 0, 4);
  var headerLength = preHeader[0];
  var binarySegmentOffset = preHeader[1];
  var binarySegmentSize = preHeader[2];
  // var pad = preHeader[3];

  // Extract the header from the packet.
  var headerView = new Uint8Array(packet, 4*4, headerLength);
  var headerString = extractFrameHeader(headerView);
  
  // Create a JSON object from the header.
  var headerJson = JSON.parse(headerString);
  
  // var binarySegment = new ArrayBufferView(packet, binarySegmentOffset, binarySegmentSize);
  var binarySegment = new DataView(packet, binarySegmentOffset, binarySegmentSize);
  
  return {header: headerJson, binarySegment:binarySegment};
}

function structuredMessageExtractValue(node, binarySegment) {
  
  if (node.type == undefined)
    throw 'No type information';
  
  var type = TypeMap[node.type];
  
  if (type == undefined)
    throw 'Unknown type ' + node.type;
    
  if (type.byteSize != node.numBytes)
    throw 'Invalid byte count, ' + node.type + ' should have ' + type.byteSize + ' bytes but message contained ' + node.numBytes + ' bytes!';

  var array = new type.arrayType(binarySegment.buffer, binarySegment.byteOffset + node.byteOffset, 1 * type.elementSize);
  
  if (type.elementSize == 1)
    return array[0];
  else
    return array.subarray(0, type.elementSize);
}
