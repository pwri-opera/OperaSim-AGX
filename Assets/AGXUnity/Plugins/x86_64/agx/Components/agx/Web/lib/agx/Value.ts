/// <reference path="Object.ts"/>
/// <reference path="Type.ts"/>

module agx
{
  export class Value extends Object
  {
    format : Format;
    data : any;

    constructor(format : Format, name : string = "")
    {
      super(name);
      this.format = format;
    }
  }
}
