/// <reference path="Util.ts"/>
/// <reference path="Component.ts"/>
/// <reference path="Buffer.ts"/>

module agx
{
  export class EntityStorage extends Component
  {
    entity : string;
    numElements : number;
    capacity : number;

    idToIndexBuffer : Buffer;
    indexTdIdBuffer : Buffer;

    static IdToIndexName = "_idToIndex";
    static IndexToIdName = "_indexToId";

    // buffers : Buffer[];

    constructor(entity : string, name : string = "")
    {
      super(name);
      this.entity = entity;
      this.numElements = 0;
      this.capacity = 0;
      this.idToIndexBuffer = null;
      this.indexTdIdBuffer = null;
      // this.buffers = [];
    }

    // Override Component.addObject
    addObject(object : Object)
    {
      super.addObject(object);
      
      agx.Assert(object instanceof Buffer);
      var buffer = <Buffer> object;
      // buffers.push(buffer);

      if (buffer.name == EntityStorage.IdToIndexName)
      {
        agx.Assert(!this.idToIndexBuffer);
        this.idToIndexBuffer = buffer;
      }
      else if (buffer.name == EntityStorage.IndexToIdName)
      {
        agx.Assert(!this.indexTdIdBuffer);
        this.indexTdIdBuffer = buffer;
      }
    }

    
    // Override Component.removeObject
    removeObject(object : Object)
    {
      super.removeObject(object);

      if (object == this.idToIndexBuffer)
        this.idToIndexBuffer = null;

      if (object == this.indexTdIdBuffer)
        this.indexTdIdBuffer = null;
    }

    /**
    \return The index for a specified instance id
    */
    idToIndex(id : number) : number
    {
      agx.Assert(this.idToIndexBuffer);
      agx.Assert(id < this.idToIndexBuffer.numElements);
      var index = this.idToIndexBuffer.data[id];
      agx.Assert(index < this.numElements, "The given id does not denote a valid entity instance.");

      return index;
    }

  }
}
