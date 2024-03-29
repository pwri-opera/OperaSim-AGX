var agx;
(function (agx) {
    (function (treeview) {
        /**
        A factory for tree views. Pass a TreeSpecificationNode root and a HTML div to
        the 'Instantiate' method and it will produce a tree view.
        */
        var Builder = (function () {
            function Builder() {
            }
            Builder.Instantiate = function (treeRoot, htmlDiv) {
                var domTree = $('<ul>');
                domTree.addClass('filetree');

                domTree.treeview({
                    collapsed: true,
                    animated: "fast"
                });

                Builder.TraverseTreeSpecification(treeRoot, domTree, "");

                domTree.treeview({
                    collapsed: true,
                    animated: "fast"
                });

                // Fix the inverted signs.
                $(".open", domTree).removeClass("expandable").addClass("collapsable").removeClass("lastExpandable").addClass("lastCollapsable");
                $(".open-hitarea", domTree).removeClass("expandable-hitarea").addClass("collapsable-hitarea").removeClass("lastExpandable-hitarea").addClass("lastCollapsable-hitarea");

                $(htmlDiv).html(domTree);
                return domTree;
            };

            /**
            
            Recursive function that walks down the given TreeSpecificationNode and
            produces a 'ul'/'li' HTML tree augmented according to the
            TreeSpecificationNode and the rules of the JavaScript TreeView library.
            
            */
            Builder.TraverseTreeSpecification = function (inputNode, outputNode, parentPath) {
                if (typeof parentPath === "undefined") { parentPath = ""; }
                var numChildren = inputNode.getNumChildren();
                var haveChildren = numChildren > 0;

                // Construct the current path.
                if (parentPath == '') {
                    var path = inputNode.getName();
                } else {
                    var path = parentPath + '.' + inputNode.getName();
                }

                // Create the 'li' element representing the current tree specification node.
                var listElement = $('<li>');
                outputNode.append(listElement);

                listElement.attr('name', inputNode.getName());
                inputNode.setTreeViewNode(listElement);

                // Set the id of the 'li' node.
                if (inputNode.getId()) {
                    listElement.attr('id', inputNode.getId());
                }

                for (var i = 0; i < inputNode.getNumSubtreeClasses(); ++i) {
                    listElement.addClass(inputNode.getSubtreeClass(i));
                }

                for (var i = 0; i < inputNode.getNumSubtreeAttributes(); ++i) {
                    var attribute = inputNode.getSubtreeAttributeByIndex(i);
                    listElement.attr(attribute.key, attribute.value);
                }

                // Add the name of the node. Leaf nodes need some extra stuff, which is added here as well.
                var titleHolder = $('<span>');
                if (inputNode.getCallback()) {
                    var clickable = $('<a>');
                    listElement.append(clickable);
                    clickable.append(titleHolder);
                } else {
                    listElement.append(titleHolder);
                }
                if (inputNode.getTitle()) {
                    titleHolder.append(inputNode.getTitle());
                } else {
                    titleHolder.append(inputNode.getName());
                }

                for (var i = 0; i < inputNode.getNumAttributes(); ++i) {
                    var attribute = inputNode.getAttributeByIndex(i);
                    titleHolder.attr(attribute.key, attribute.value);
                }

                for (var i = 0; i < inputNode.getNumClasses(); ++i) {
                    titleHolder.addClass(inputNode.getClass(i));
                }

                // Set the appropriate icon, and traverse into any children.
                if (haveChildren) {
                    // The node has children, so let's make a subtree.
                    var subtreeRoot = $('<ul>');
                    listElement.append(subtreeRoot);

                    for (var i = 0; i < numChildren; ++i) {
                        Builder.TraverseTreeSpecification(inputNode.getChild(i), subtreeRoot, path);
                    }

                    // Add icon.
                    titleHolder.addClass('folder');
                } else if (inputNode.getUseIcon()) {
                    // No children, add icon.
                    titleHolder.addClass('file');
                }

                // Add callback.
                if (inputNode.getCallback()) {
                    (function (callback, path, sourceNode) {
                        $(clickable).click(function () {
                            callback(path, sourceNode);
                            return true;
                        });
                    })(inputNode.getCallback(), path, inputNode);
                }
            };
            return Builder;
        })();
        treeview.Builder = Builder;

        /**
        Tree description class. The user describe the tree that is to be created using
        a connected set of these nodes.
        */
        var TreeSpecificationNode = (function () {
            function TreeSpecificationNode(name, title, id) {
                if (typeof title === "undefined") { title = name; }
                if (typeof id === "undefined") { id = null; }
                this.name = name;
                this.title = title;
                this.id = id;
                this.attributes = new Array();
                this.subtreeAttributes = new Array();
                this.classes = new Array();
                this.subtreeClasses = new Array();
                this.callback = null;
                this.parent = null;
                this.children = new Array();
                this.treeViewNode = null;
                this.customData = null;
                this.useIcon = true;
            }
            TreeSpecificationNode.prototype.getName = function () {
                return this.name;
            };

            TreeSpecificationNode.prototype.getTitle = function () {
                return this.title;
            };

            TreeSpecificationNode.prototype.setTitle = function (title) {
                this.title = title;
            };

            TreeSpecificationNode.prototype.getId = function () {
                return this.id;
            };

            /// Add an HTML attribute that is seen by the current node only. Will not be
            /// part of the sub tree rooted at the current node.
            TreeSpecificationNode.prototype.addAttribute = function (key, value) {
                this.attributes.push(new TreeNodeAttribute(key, value));
            };

            /// /return The number of node-specific attributes.
            TreeSpecificationNode.prototype.getNumAttributes = function () {
                return this.attributes.length;
            };

            /// \return The node-specific attribute on the given index, or null if index is out of range.
            TreeSpecificationNode.prototype.getAttributeByIndex = function (index) {
                if (index >= this.attributes.length || index < 0)
                    return null;

                return this.attributes[index];
            };

            /// \return The node-specific attribute with the given key, or null if there is no such attribute.
            TreeSpecificationNode.prototype.getAttributeByKey = function (key) {
                for (var i = 0; i < this.attributes.length; ++i) {
                    if (this.attributes[i].key == key)
                        return this.attributes[i].value;
                }
                return null;
            };

            /// Add an HTML attribute that is seen by the entire subtree rooted at the current node.
            TreeSpecificationNode.prototype.addSubtreeAttribute = function (key, value) {
                this.subtreeAttributes.push(new TreeNodeAttribute(key, value));
            };

            /// \return The number of attributes assigned to the subtree rooted at the current node.
            TreeSpecificationNode.prototype.getNumSubtreeAttributes = function () {
                return this.subtreeAttributes.length;
            };

            /// \return The subtree attribute at the given index, or null if the given index is out of range.
            TreeSpecificationNode.prototype.getSubtreeAttributeByIndex = function (index) {
                if (index >= this.subtreeAttributes.length || index < 0)
                    return null;

                return this.subtreeAttributes[index];
            };

            TreeSpecificationNode.prototype.getSubtreeAttributeByKey = function (key) {
                for (var i = 0; i < this.subtreeAttributes.length; ++i) {
                    if (this.subtreeAttributes[i].key == key)
                        return this.subtreeAttributes[i].value;
                }

                return null;
            };

            /// Add an HTML class that is seen by the current node only. Will not be
            /// part of the subtree rooted at the current node.
            TreeSpecificationNode.prototype.addClass = function (newClass) {
                this.classes.push(newClass);
            };

            /// \return The number of classes this node will produce.
            TreeSpecificationNode.prototype.getNumClasses = function () {
                return this.classes.length;
            };

            /// \return The class with the given index, or null if the given index is out of range.
            TreeSpecificationNode.prototype.getClass = function (index) {
                if (index >= this.classes.length || index < 0)
                    return null;

                return this.classes[index];
            };

            /// Add an HTML class that is seen by the entire subtree rooted at the current node.
            TreeSpecificationNode.prototype.addSubtreeClass = function (newClass) {
                this.subtreeClasses.push(newClass);
            };

            TreeSpecificationNode.prototype.getNumSubtreeClasses = function () {
                return this.subtreeClasses.length;
            };

            TreeSpecificationNode.prototype.getSubtreeClass = function (index) {
                if (index >= this.subtreeClasses.length || index < 0)
                    return null;

                return this.subtreeClasses[index];
            };

            /// Make the given node a child of the current node.
            TreeSpecificationNode.prototype.addChild = function (child) {
                if (child.parent == null) {
                    this.children.push(child);
                    child.parent = this;
                } else {
                    return false;
                }
            };

            /// \return The child at the given index, or null if the index is out of range.k
            TreeSpecificationNode.prototype.getChild = function (index) {
                if (index >= this.children.length || index < 0)
                    return null;

                return this.children[index];
            };

            /// \return The child with the given name, or null if no such child exists.
            TreeSpecificationNode.prototype.getChildByName = function (name) {
                var numChildren = this.children.length;
                for (var i = 0; i < numChildren; ++i) {
                    if (this.children[i].name == name)
                        return this.children[i];
                }
                return null;
            };

            TreeSpecificationNode.prototype.getParent = function () {
                return this.parent;
            };

            /// \return The number of children this node has.
            TreeSpecificationNode.prototype.getNumChildren = function () {
                return this.children.length;
            };

            /// Set the callback that will be called when the current node is clicked.
            TreeSpecificationNode.prototype.setCallback = function (callback) {
                this.callback = callback;
            };

            TreeSpecificationNode.prototype.getCallback = function () {
                return this.callback;
            };

            TreeSpecificationNode.prototype.setCustomData = function (data) {
                this.customData = data;
            };

            TreeSpecificationNode.prototype.getCustomData = function () {
                return this.customData;
            };

            TreeSpecificationNode.prototype.setUseIcon = function (value) {
                this.useIcon = value;
            };

            TreeSpecificationNode.prototype.getUseIcon = function () {
                return this.useIcon;
            };

            /// Called by the tree view builder to create a mapping from the tree
            /// specification node to the HTML 'li' node created for that specification
            /// node.
            TreeSpecificationNode.prototype.setTreeViewNode = function (treeViewNode) {
                this.treeViewNode = treeViewNode;
            };
            return TreeSpecificationNode;
        })();
        treeview.TreeSpecificationNode = TreeSpecificationNode;

        var TreeNodeAttribute = (function () {
            function TreeNodeAttribute(key, value) {
                this.key = key;
                this.value = value;
            }
            return TreeNodeAttribute;
        })();
        treeview.TreeNodeAttribute = TreeNodeAttribute;

        // Main function. Run when not used as a library.
        var main = function () {
            console.log("Running through Node.js");
        };

        

        if (typeof require != "undefined") {
            if (require.main === module) {
                main();
            }
        }
    })(agx.treeview || (agx.treeview = {}));
    var treeview = agx.treeview;
})(agx || (agx = {})); // Module Agx.
