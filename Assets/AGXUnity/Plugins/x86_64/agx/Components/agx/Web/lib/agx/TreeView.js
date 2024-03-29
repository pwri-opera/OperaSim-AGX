

/**

Create a collapsable tree view from the given tree specification and insert it
into the given container, which shold be something that has a .html method.

The tree specification is a recursive object that defines the content of the
tree. The following describes the format of the tree specification. Optional
members are marked with '*'.

treeSpecificationNode =
{
  name : string,
* title : string,
* id : string,
* attributes ; [{key : string, value : string} , ... ],  OR  {key : string, value : string},
* subtreeAttributes : [{key : string, value : string}, ... ], OR {key : string, value : string},
* classes : [string, ...], OR string,
* subtreeClasses : [string, ...], OR string,
}

In addition to the above, branch nodes should contain the following members.

{
  children : [treeSpecificationNode, ...],
}

In addition to the above, leaf nodes may contain the following members.

{
* clickCallback : function( string, treeSpecificationNode ),
}


If the title is given, then that is the string displayed on the page. If not,
then the name is used instead.

The given specification tree will be augmented with the created GUI elements
as a member named 'treeViewNode'. The type of this member is currently the
return value of 'document.createElement("li")', but this may change in the
future.


\param treeSpecification
\param container

\return The root of the tree view.
*/
function constructTree( treeSpecification, container ) {
  var domTree = document.createElement( 'ul' );
  $(domTree).addClass('filetree');

  traverseTreeSpecification( treeSpecification, domTree, '' );

  $(domTree).treeview( {
    collapsed: true
  } );

  $(container).html( domTree );
  return domTree;
}



function getLocalNode( treeViewNode ) {
  return $('span', treeViewNode)[0];
}


/**
Add a class to the class list that will be applied to the GUI node created by
'constructTree' for the given 'treeSpecificationNode'. The class array will be
created if required.

\param treeSpecificationNode The tree specification node that will be given a class.
\param class A string that is the class to add.
*/
function appendClass( treeSpecificationNode, newClass ) {
  if ( treeSpecificationNode.classes == undefined ) {
    treeSpecificationNode.classes = [];
  }
  treeSpecificationNode.classes.push( newClass );
}


/**
Return the root node of the sub-tree representing the given path in the given tree view.

\param treeViewRoot The root node of the tree view to search through.
\param path '.'-separated list of node names that defines a path in the given tree.
*/
function getTreeViewNode( treeViewRoot, path  ) {
  var pathComponents = path.split( '.' );
  var it = treeViewRoot;

  // Walk down the tree according to the given path.
  for ( var i = 0 ; i < pathComponents.length ; ++i ) {

    // Find the name of the next child.
    var childName = pathComponents[i];

    // Descend.
    it = $(it).children( 'li[name="'+childName+'"]' )[0];
    if ( !it ) {
      // Didn't find the child we're looking for.
      return undefined;
    }

    // If there is more descending to be done, then we need to move the
    // iterator to the actual child list.
    if ( i < pathComponents.length-1 ) {
      it = $(it).children( 'ul' )[0];
      if ( !it ) {
        // We expected to descent more, but the current node didn't have a child list.
        return undefined;
      }
    }
  }

  // Descent complete, return the iterator.
  return it;
}



/**
\cond internal

Internal method.

Recursively walk down the given tree specification and create the ul/li
elements as we go. The given list head should always point to a 'ul'.

The path argument is a string that is built as the recursion descends and
contains the path through the tree to the parent of the current node.
*/
function traverseTreeSpecification( treeSpecificationNode, listHead, parentPath ) {

console.log( "TreeView: traversing node " + treeSpecificationNode.name + " in " + parentPath );

  // Get some data from the specification.
  var childList = treeSpecificationNode.children;
  var haveChildren = childList != undefined;

  // Construct the current path.
  if ( parentPath == '' ) {
    var path = treeSpecificationNode.name;
  }
  else {
    var path = parentPath + '.' + treeSpecificationNode.name;
  }

  // Create the 'li' elemnent representing the current tree specification node.
  var listElement = document.createElement( 'li' );
  $(listHead).append( listElement );

  $(listElement).attr( 'name', treeSpecificationNode.name );
  treeSpecificationNode.treeViewNode = listElement;

  // Set the id of the 'li' node.
  if ( treeSpecificationNode.id ) {
    $(listElement).attr( "id", treeSpecificationNode.id );
  }


  // Add any custom classes to the 'li' node.
  var subtreeClassList = treeSpecificationNode.subtreeClasses;
  if ( subtreeClassList ) {
    if ( subtreeClassList instanceof Array ) {
      // Got list of classes.
      for ( index in subtreeClassList ) {
        $(listElement).addClass( subtreeClassList[index] );
      }
    }
    else {
      // Got a single class.
      $(listElement).addClass( subtreeClassList );
    }
  }

  
  // Add any custom attributes to the 'li' node.
  var subtreeAttributeList = treeSpecificationNode.subtreeAttributes;
  if ( subtreeAttributeList ) {
    if ( subtreeAttributeList instanceof Array ) {
      // Got a list of attributes.
      for ( index in subtreeAttributeList ) {
        $(listElement).attr( subtreeAttributeList[index].key, subtreeAttributeList[index].value );
      }
    }
    else {
      $(listElement).attr( subtreeAttributeList.key, subtreeAttributeList.value );
    }
  }

  // Add the name of the node. Leaf nodes need some extra stuff, which is added here as well.
  var titleHolder = document.createElement( 'span' );
  if ( haveChildren ) {
    $(listElement).append( titleHolder );
  }
  else {
    var clickable = document.createElement( 'a' );
    $(listElement).append( clickable );
    $(clickable).append( titleHolder );
  }
  if ( treeSpecificationNode.title )
  {
    $(titleHolder).append( treeSpecificationNode.title );
  }
  else {
    $(titleHolder).append( treeSpecificationNode.name );
  }



  // Add any custom classes to the 'span' node.
  var classList = treeSpecificationNode.classes;
  if ( classList ) {
    if ( classList instanceof Array ) {
      // Got list of classes.
      for ( index in classList ) {
        $(titleHolder).addClass( classList[index] );
      }
    }
    else {
      // Got a single class.
      $(titleHolder).addClass( classList );
    }
  }

  
  // Add any custom attributes to the 'span' node.
  var attributeList = treeSpecificationNode.attributes;
  if ( attributeList ) {
    if ( attributeList instanceof Array ) {
      // Got a list of attributes.
      for ( index in attributeList ) {
        $(titleHolder).attr( attributeList[index].key, attributeList[index].value );
      }
    }
    else {
      $(titleHolder).attr( attributeList.key, attributeList.value );
    }
  }


  // Either make this node a branch node and add the children, or make it a
  // leaf node and add an onclick callback.
  if ( haveChildren ) {

    // The node has children, so let's make a subtree.
    var subtreeRoot = document.createElement( 'ul' );
    $(listElement).append( subtreeRoot );
    for ( key in childList ) {
      traverseTreeSpecification( childList[key], subtreeRoot, path );
    }

     // Add icon.
    $(titleHolder).addClass( 'folder' );
  }
  else {

    // No children, add callback.
    if ( treeSpecificationNode.clickCallback ) {
      (function ( callback, path, sourceNode ) {
        $(clickable).click( function() {
          callback(path, sourceNode);
          return false;
        });
      })( treeSpecificationNode.clickCallback, path, treeSpecificationNode );
    }
    
    // Add icon.
    $(titleHolder).addClass( 'file' );
  }

}