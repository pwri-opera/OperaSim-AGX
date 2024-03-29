
module agx
{
  export class Object
  {
    name : string;
    id : number;
    context : any; // Should be Component, but problem with include-loop

    private static idCounter : number = 0;

    constructor(name : string = "")
    {
      this.name = name;
      this.id = Object.idCounter++;
      this.context = null;
    }

    getPath() : string
    {
      return (this.context && this.context.context) ? this.context.getPath() + "." + this.name : this.name;
    }
  }
}
