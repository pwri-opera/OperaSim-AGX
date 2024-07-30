declare var $ : any;


module agx
{

  export module treeview
  {


/**
A factory for tree views. Pass a TreeSpecificationNode root and a HTML div to
the 'Instantiate' method and it will produce a tree view.
*/
export class Builder {
  static Instantiate( treeRoot : TreeSpecificationNode , htmlDiv : any ) : any
  {
    var domTree : any = $('<ul>');
    domTree.addClass('filetree');

    domTree.treeview( {
      collapsed: true,
      animated: "fast"
    } );

    Builder.TraverseTreeSpecification( treeRoot, domTree, "" );

    domTree.treeview( {
      collapsed: true,
      animated: "fast"
    } );

    // Fix the inverted signs.
    $(".open", domTree).removeClass("expandable").addClass("collapsable").removeClass("lastExpandable").addClass("lastCollapsable");
    $(".open-hitarea", domTree).removeClass("expandable-hitarea").addClass("collapsable-hitarea").removeClass("lastExpandable-hitarea").addClass("lastCollapsable-hitarea");

    $(htmlDiv).html( domTree );
    return domTree;
  }

  /**

  Recursive function that walks down the given TreeSpecificationNode and
  produces a 'ul'/'li' HTML tree augmented according to the
  TreeSpecificationNode and the rules of the JavaScript TreeView library.

  */
  private static TraverseTreeSpecification( inputNode : TreeSpecificationNode, outputNode : any, parentPath = "" ) : void
  {
    
    var numChildren : number = inputNode.getNumChildren();
    var haveChildren : boolean = numChildren > 0;

    // Construct the current path.
    if ( parentPath == '' ) {
      var path = inputNode.getName();
    } else {
      var path = parentPath + '.' + inputNode.getName();
    }

    // Create the 'li' element representing the current tree specification node.
    var listElement = $( '<li>' );
    outputNode.append( listElement );

    listElement.attr( 'name', inputNode.getName() );
    inputNode.setTreeViewNode( listElement );

    // Set the id of the 'li' node.
    if ( inputNode.getId() ) {
      listElement.attr( 'id', inputNode.getId() );
    }

    // Add any custom classes to the 'li' node, which is the root of the entire subtree.
    for ( var i = 0 ; i < inputNode.getNumSubtreeClasses() ; ++i ) {
      listElement.addClass( inputNode.getSubtreeClass(i) );
    }

    // Add any custom attributes to the 'li' node, which is the root of the entire subtree.
    for ( var i = 0 ; i < inputNode.getNumSubtreeAttributes() ; ++i ) {
      var attribute : TreeNodeAttribute = inputNode.getSubtreeAttributeByIndex(i);
      listElement.attr( attribute.key, attribute.value );
    }


    // Add the name of the node. Leaf nodes need some extra stuff, which is added here as well.
    var titleHolder : any = $( '<span>' );
    if ( inputNode.getCallback() ) {
      var clickable : any = $( '<a>' );
      listElement.append( clickable );
      clickable.append( titleHolder );
    }
    else {
      listElement.append( titleHolder );
    }
    if ( inputNode.getTitle() ) {
      titleHolder.append( inputNode.getTitle() );
    } else {
      titleHolder.append( inputNode.getName() );
    }


    // Add any custom attributes to the 'span' node.
    for ( var i = 0 ; i < inputNode.getNumAttributes() ; ++i ) {
      var attribute : TreeNodeAttribute = inputNode.getAttributeByIndex(i);
      titleHolder.attr( attribute.key, attribute.value );
    }

    // Add any custom classes to the 'span' node, which is not shared by the subtree.
    for ( var i = 0 ; i < inputNode.getNumClasses() ; ++i ) {
      titleHolder.addClass( inputNode.getClass(i) );
    }




    // Set the appropriate icon, and traverse into any children.
    if ( haveChildren ) {

      // The node has children, so let's make a subtree.
      var subtreeRoot : any = $( '<ul>' );
      listElement.append( subtreeRoot );

      // Add the children to the subtree.
      for ( var i = 0 ; i < numChildren ; ++i ) {
        Builder.TraverseTreeSpecification( inputNode.getChild(i), subtreeRoot, path );
      }

      // Add icon.
      titleHolder.addClass( 'folder' );
    }
    else if (inputNode.getUseIcon()) {
      // No children, add icon.
      titleHolder.addClass( 'file' );
    }


    // Add callback.
    if ( inputNode.getCallback() ) {
      (function ( callback, path, sourceNode ) {
        $(clickable).click( function() {
          callback( path, sourceNode );
          return true;
        });
      })( inputNode.getCallback(), path, inputNode );
    }
  }
}


/**
Tree description class. The user describe the tree that is to be created using
a connected set of these nodes.
*/
export class TreeSpecificationNode
{

  constructor( name : string, title : string = name, id : string = null)
  {
    this.name = name;
    this.title = title;
    this.id = id;
    this.attributes = new Array<TreeNodeAttribute>();
    this.subtreeAttributes = new Array<TreeNodeAttribute>();
    this.classes = new Array<string>();
    this.subtreeClasses = new Array<string>();
    this.callback = null;
    this.parent = null;
    this.children = new Array<TreeSpecificationNode>();
    this.treeViewNode = null;
    this.customData = null;
    this.useIcon = true;
  }

  getName() : string
  {
    return this.name;
  }

  getTitle() : string
  {
    return this.title;
  }

  setTitle(title: string)
  {
    this.title = title;
  }

  getId() : string
  {
    return this.id;
  }

  /// Add an HTML attribute that is seen by the current node only. Will not be
  /// part of the sub tree rooted at the current node.
  addAttribute( key : string, value : string ) : void
  {
    this.attributes.push( new TreeNodeAttribute(key, value) );
  }

  /// /return The number of node-specific attributes.
  getNumAttributes() : number
  {
    return this.attributes.length;
  }

  /// \return The node-specific attribute on the given index, or null if index is out of range.
  getAttributeByIndex( index : number ) : TreeNodeAttribute
  {
    if ( index >= this.attributes.length || index < 0  )
      return null;

    return this.attributes[ index ];
  }

  /// \return The node-specific attribute with the given key, or null if there is no such attribute.
  getAttributeByKey( key : string ) : string
  {
    for ( var i = 0 ; i < this.attributes.length ; ++i ) {
      if ( this.attributes[i].key == key )
        return this.attributes[i].value;
    }
    return null;
  }

  /// Add an HTML attribute that is seen by the entire subtree rooted at the current node.
  addSubtreeAttribute( key: string, value : string ) : void
  {
    this.subtreeAttributes.push( new TreeNodeAttribute(key, value) );
  }

  /// \return The number of attributes assigned to the subtree rooted at the current node.
  getNumSubtreeAttributes() : number
  {
    return this.subtreeAttributes.length;
  }

  /// \return The subtree attribute at the given index, or null if the given index is out of range.
  getSubtreeAttributeByIndex( index : number) : TreeNodeAttribute
  {
    if ( index >= this.subtreeAttributes.length || index < 0 )
      return null;

    return this.subtreeAttributes[ index ];
  }


  getSubtreeAttributeByKey( key : string ) : string
  {
    for ( var i = 0 ; i < this.subtreeAttributes.length ; ++i ) {
      if ( this.subtreeAttributes[i].key == key )
        return this.subtreeAttributes[i].value;
    }

    return null;

  }

  /// Add an HTML class that is seen by the current node only. Will not be
  /// part of the subtree rooted at the current node.
  addClass( newClass : string ) : void
  {
    this.classes.push( newClass );
  }

  /// \return The number of classes this node will produce.
  getNumClasses() : number
  {
    return this.classes.length;
  }

  /// \return The class with the given index, or null if the given index is out of range.
  getClass( index : number ) : string
  {
    if ( index >= this.classes.length || index < 0 )
      return null;

    return this.classes[ index ];
  }

  /// Add an HTML class that is seen by the entire subtree rooted at the current node.
  addSubtreeClass( newClass : string ) : void
  {
    this.subtreeClasses.push( newClass );
  }

  getNumSubtreeClasses() : number
  {
    return this.subtreeClasses.length;
  }

  getSubtreeClass( index : number )
  {
    if ( index >= this.subtreeClasses.length || index < 0 )
      return null;

    return this.subtreeClasses[ index ];
  }

  /// Make the given node a child of the current node.
  addChild( child : TreeSpecificationNode ) : boolean
  {
    if ( child.parent == null ) {
      this.children.push( child );
      child.parent = this;
    }
    else {
      return false;
    }
  }

  /// \return The child at the given index, or null if the index is out of range.k
  getChild( index : number ) : TreeSpecificationNode
  {
    if ( index >= this.children.length || index < 0 )
      return null;

    return this.children[ index ];
  }

  /// \return The child with the given name, or null if no such child exists.
  getChildByName( name : string ) : TreeSpecificationNode
  {
    var numChildren : number = this.children.length;
    for ( var i = 0 ; i < numChildren ; ++i ) {
      if ( this.children[i].name == name )
        return this.children[i];
    }
    return null;
  }


  getParent() : TreeSpecificationNode
  {
    return this.parent;
  }

  /// \return The number of children this node has.
  getNumChildren() : number
  {
    return this.children.length;
  }

  /// Set the callback that will be called when the current node is clicked.
  setCallback( callback : ( path : string, node : TreeSpecificationNode ) => any ) : void
  {
    this.callback = callback;
  }

  getCallback() : ( path : string, node : TreeSpecificationNode ) => any
  {
    return this.callback;
  }

  setCustomData( data : any )
  {
    this.customData = data;
  }

  getCustomData() : any
  {
    return this.customData;
  }

  setUseIcon( value : boolean )
  {
    this.useIcon = value;
  }

  getUseIcon() : boolean
  {
    return this.useIcon;
  }

  /// Called by the tree view builder to create a mapping from the tree
  /// specification node to the HTML 'li' node created for that specification
  /// node.
  setTreeViewNode( treeViewNode : any ) : void
  {
    this.treeViewNode = treeViewNode;
  }

  // Members controlled by the user constructing a tree.
  private name : string;
  private title : string;
  private id : string;
  private attributes : TreeNodeAttribute[];
  private subtreeAttributes : TreeNodeAttribute[];
  private classes : string[];
  private subtreeClasses : string[];
  private callback : ( path : string, node : TreeSpecificationNode ) => any;
  private parent : TreeSpecificationNode;
  private children : TreeSpecificationNode[];
  private customData : any;
  private useIcon : boolean;

  // Members referring to DOM elements created for the tree. The type fo these
  // members depend on the library used to generate the TreeView widget.
  private treeViewNode : any;
}


export class TreeNodeAttribute
{
  constructor( key : string, value : string )
  {
    this.key = key;
    this.value = value;
  }

  key : string;
  value : string;
}



// Main function. Run when not used as a library.
var main = function()
{
  console.log("Running through Node.js");
}


// Determine if we are running through Node.js. If so, call main().
declare var require : any;
declare var module : any;
if ( typeof require != "undefined" )
{
  if ( require.main === module )
  {
    main();
  }
}


} // Module Widget.
} // Module Agx.