declare var $ : any;
declare var when : any;
declare var Q : any;

module agxGL
{
  export class Kernel
  {
    program;
    context;
    shaders;
  
  
    /**
    Do not use construtor directly, since shaders are fetched async.
    The load method returns a promise containing the loaded kernel. 
    */
    static load(context, path) {
      var kernel = new Kernel(context);
      return kernel.loadShaders(path);
    }
  
  
  
  
  
  
    constructor(context)
    {
      this.context = context;
      this.program = context.createProgram();
      this.shaders = {};
    }
  
    loadShaders(path)
    {
      var vertShaderPath = path + '.vert';
      var fragShaderPath = path + '.frag';
    
      var vertexShader = this.loadShader(vertShaderPath);
      var fragShader = this.loadShader(fragShaderPath);
    
      var done = Q.all([vertexShader, fragShader]).then(
        () =>
        {
          this.context.linkProgram(this.program);

          if (!this.context.getProgramParameter(this.program, this.context.LINK_STATUS))
            throw 'Could not link shader program \'' + path + '\'';
        
          return this;
        }
      );
    
      return done;
    }
  
  
    loadShader(path)
    {
      return $.ajax({
        dataType: 'text',
        mimeType: "textPlain",
        url: '/agxComponent/Read',
        data: {path: path}
      }).then(
          // Success
          (response, textStatus, xhr) => {
            console.log('Received shader: ' + path);
            return this.initShader(path, response);
          },
          // Error
          (xhr, textStatus, errorThrown) => {
            console.log('Error: ' + errorThrown);
            throw errorThrown;
          }
      );
    }
  
  
    initShader(path, data)
    {
      var shader;
    
      if (path.endsWith('.frag')) {
        shader = this.context.createShader(this.context.FRAGMENT_SHADER);
        this.shaders.fragment = shader;
        this.shaders.fragment.path = path;
      } else if (path.endsWith('.vert')) {
        shader = this.context.createShader(this.context.VERTEX_SHADER);
        this.shaders.vertex = shader;
        this.shaders.vertex.path = path;
      } else {
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
    }
  
  } 
  
}


