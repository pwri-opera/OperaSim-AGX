agxGL.Camera = function(fov, width, height, near, far) {
  
  // Methods
  this.synchronize = function() {
    mat4f.perspective(this.fov, this.aspectRatio, this.near, this.far, this.projectionMatrix);
    mat4f.multiply(this.projectionMatrix, this.viewMatrix, this.modelViewProjectionMatrix);
    // console.log('fov: ' + this.fov + ', aspectRatio: ' + this.aspectRatio + ', near: ' + this.near + ', far: ' + this.far);
    // console.log('projection: ' + this.projectionMatrix);
    // console.log('view: ' + this.viewMatrix);
    // console.log('mvp: ' + this.modelViewProjectionMatrix);
  }

  this.synchronizeMVP = function() {
    mat4f.multiply(this.projectionMatrix, this.viewMatrix, this.modelViewProjectionMatrix);
  }
  
  this.synchronizePointScale = function() {
    this.pointScale = height / Math.tan(this.fov * 0.5 * Math.PI/180);
    // console.log('pointScale: ' + this.pointScale);
  }

  this.setViewPort = function(width, height) {
    this.width = width;
    this.height = height;
    this.aspectRatio = width/height;

    this.synchronizePointScale();
    this.synchronize();
  }

  this.setFov = function(fov) {
    this.fov = fov;

    this.synchronizePointScale();
    this.synchronize();
  }
  
  
  this.getViewMatrixAsLookAt = function(lookDistance)
  {
    if (lookDistance == undefined)
      lookDistance = 1.0;
      
    var result = {};
    result.eye = vec3f.create();
    result.center = vec3f.create();
    result.up = vec3f.create();
    
    var inv = mat4f.create();
    mat4f.inverse(this.viewMatrix, inv);
    
    mat4f.multiplyVec3(inv, [0, 0, 0], result.eye);
    
    var inv3x3 = mat3f.create();
    mat4f.toInverseMat3(this.viewMatrix, inv3x3);
    mat3f.multiplyVec3(inv3x3, [0, 1, 0], result.up);
    
    var lookVector = vec3f.create();
    mat3f.multiplyVec3(inv3x3, [0, 0, -1], lookVector);
    
    vec3f.normalize(lookVector, lookVector);
    
    vec3f.scale(lookVector, lookDistance, lookVector);
    vec3f.add(result.eye, lookVector, result.center);
    
    return result;
  }
  
  this.setViewMatrixAsLookAt = function(eye, center, up) {
    if (up == undefined)
      up = [0, 1, 0];
      
    mat4f.lookAt(eye, center, up, this.viewMatrix);
    this.synchronizeMVP();
  }
  
  this.move = function(offset) {
    // mat4f.translate(this.viewMatrix, offset);
    
    var lookAt = this.getViewMatrixAsLookAt();
    // console.log('lookAt.eye: ' + lookAt.eye + ', center: ' + lookAt.center + ', up: ' + lookAt.up);
    
    vec3f.add(lookAt.eye, offset, lookAt.eye);
    vec3f.add(lookAt.center, offset, lookAt.center);
    
    this.setViewMatrixAsLookAt(lookAt.eye, lookAt.center, lookAt.up);
  }
  
  
  this.moveLocal = function(localOffset) {
    var inv3x3 = mat3f.create();
    mat4f.toInverseMat3(this.viewMatrix, inv3x3);
    var globalOffset = vec3f.create();
    mat3f.multiplyVec3(inv3x3, localOffset, globalOffset);
    this.move(globalOffset);
  }
  
  
  this.setViewMatrix = function(matrix) {
    this.viewMatrix = matrix;
    this.synchronizeMVP();
  }

  this.setProjectionMatrix = function(matrix) {
    this.projectionMatrix = matrix;
    this.synchronizeMVP();
  }
  
  this.setProjectionAndViewMatrix = function(projectionMatrix, viewMatrix) {
    this.projectionMatrix = projectionMatrix;
    this.viewMatrix = viewMatrix;
    this.synchronizeMVP();
  }
  
  this.alignUpVector = function(targetUp) {
    if (targetUp == undefined)
      targetUp = [0, 1, 0];
      
    var lookAt = this.getViewMatrixAsLookAt();
    
    var forward = vec3f.create();
    vec3f.subtract(lookAt.center, lookAt.eye, forward);

    var side = vec3f.create();
    vec3f.cross(forward, targetUp, side);
    
    var alignedUp = vec3f.create();
    vec3f.cross(side, forward, alignedUp);
    vec3f.normalize(alignedUp, alignedUp);
    
    this.setViewMatrixAsLookAt(lookAt.eye, lookAt.center, alignedUp);
  }
  

  // this.setPosition = function(position) {
  //   mat4f.identity(this.viewMatrix);
  //   mat4f.translate(this.viewMatrix, position);
  //   this.synchronizeMVP();
  // }
  // 
  // this.setTranslate = function(translate) {
  //   mat4f.set(translate, this.viewMatrix);
  //   this.synchronizeMVP();
  // }
  
  /////////////////////////////////////////////
  
  
  // Constructor
  // this.transform = mat4f.identity();
  this.viewMatrix = mat4f.identity()
  this.projectionMatrix = mat4f.identity();
  this.modelViewProjectionMatrix = mat4f.identity();

  this.fov = fov;
  this.near = near != undefined ? near : 1.0;
  this.far = far != undefined ? far : 100.0;
  this.setViewPort(width, height);


  this.synchronize();
}



