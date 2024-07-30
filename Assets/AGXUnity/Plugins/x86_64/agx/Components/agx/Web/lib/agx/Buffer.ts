/// <reference path="Util.ts"/>
/// <reference path="Type.ts"/>
/// <reference path="Object.ts"/>

module agx
{
  export class Buffer extends Object
  {
    format : Format;
    data : any; // Normally a ArrayBufferView, eg Float32Array, but for name format it is string[]
    numElements : number;
    displayName : string;

    constructor(format : Format, name : string = "")
    {
      super(name);
      agx.Assert(format);
      this.format = format;
      this.data = null;
      this.numElements = 0;
      this.displayName = name;
    }
  }
}
