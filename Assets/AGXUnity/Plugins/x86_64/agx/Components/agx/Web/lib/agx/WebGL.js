var agxGL;
(function (agxGL) {
    var Kernel = (function () {
        function Kernel(context) {
            this.context = context;
            this.program = context.createProgram();
            this.shaders = {};
        }
        /**
        Do not use construtor directly, since shaders are fetched async.
        The load method returns a promise containing the loaded kernel.
        */
        Kernel.load = function (context, path) {
            var kernel = new Kernel(context);
            return kernel.loadShaders(path);
        };
        Kernel.prototype.loadShaders = function (path) {
            var _this = this;
            var vertShaderPath = path + '.vert';
            var fragShaderPath = path + '.frag';
            var vertexShader = this.loadShader(vertShaderPath);
            var fragShader = this.loadShader(fragShaderPath);
            var done = Q.all([vertexShader, fragShader]).then(function () {
                _this.context.linkProgram(_this.program);
                if (!_this.context.getProgramParameter(_this.program, _this.context.LINK_STATUS))
                    throw 'Could not link shader program \'' + path + '\'';
                return _this;
            });
            return done;
        };
        Kernel.prototype.loadShader = function (path) {
            var _this = this;
            return $.ajax({
                dataType: 'text',
                mimeType: "textPlain",
                url: '/agxComponent/Read',
                data: { path: path }
            }).then(
            // Success
            function (response, textStatus, xhr) {
                console.log('Received shader: ' + path);
                return _this.initShader(path, response);
            }, 
            // Error
            function (xhr, textStatus, errorThrown) {
                console.log('Error: ' + errorThrown);
                throw errorThrown;
            });
        };
        Kernel.prototype.initShader = function (path, data) {
            var shader;
            if (path.endsWith('.frag')) {
                shader = this.context.createShader(this.context.FRAGMENT_SHADER);
                this.shaders.fragment = shader;
                this.shaders.fragment.path = path;
            }
            else if (path.endsWith('.vert')) {
                shader = this.context.createShader(this.context.VERTEX_SHADER);
                this.shaders.vertex = shader;
                this.shaders.vertex.path = path;
            }
            else {
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
        };
        return Kernel;
    })();
    agxGL.Kernel = Kernel;
})(agxGL || (agxGL = {}));
