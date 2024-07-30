/// <reference path="Util.ts"/>
/// <reference path="Object.ts"/>

module agx
{
  export class Component extends Object
  {
    objects : any;

    constructor(name : string = "")
    {
      super(name);
      this.objects = {};
    }

    /**
    Add a child object.
    */
    addObject(object : Object)
    {
      agx.Assert(object);
      // console.log(this.getPath() + ": Adding " + object.name);
      agx.Assert(!object.context);
      agx.Assert(!agx.IsDefined(this.objects[object.name]));
      this.objects[object.name] = object;
      object.context = this;
    }

    /**
    Remove a child object.
    */
    removeObject(object : Object)
    {
      agx.Assert(object);
      agx.Assert(object.context == this);
      agx.Assert(agx.IsDefined(this.objects[object.name]));

      object.context = null;
      delete this.objects[object.name];
    }

    /**
    \return The child object with the specified name.
    */
    getObject(name : string)
    {
      var object = this.objects[name];
      return agx.IsDefined(object) ? object : null;
    }

    getNumObjects() : number
    {
      // This is the proper way, but the agx.Object class shadows the JavaScript Object object,
      // I don't know how to escape the the agx module scope.
      // return Object.keys(this.objects).length

      var numChildren = 0;
      for ( var key in this.objects ) {
        if ( this.objects.hasOwnProperty(key) )
          numChildren++;
      }
      return numChildren;
    }

    /**
    \return The child resource with the specified path.
    */
    getResource(path : string) : Object
    {
      var pathComponents = path.split(".");
      var parent = this;
      var object = null;

      for (var i = 0; i < pathComponents.length; ++i)
      {
        object = parent.getObject(pathComponents[i]);
        if (!object)
          break;

        parent = object;
      }

      return object;
    }
  }
}
