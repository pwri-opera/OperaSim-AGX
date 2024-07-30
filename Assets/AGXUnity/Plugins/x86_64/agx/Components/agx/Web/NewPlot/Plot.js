/// <reference path="../lib/agx/Util.ts"/>
/// <reference path="../lib/agx/Frame.ts"/>
/// <reference path="../lib/agx/NewPlot.ts"/>


$(function () {
    initTranslator();

    // Cache the worker script.
    newPlotCsvWorker = new Worker("../lib/agx/NewPlotCsvWorker.js");

    document.oncontextmenu = function (event) {
        event.preventDefault();
    };

    var options = {
        figureLabel: ""
    };

    function init(plot) {
        plot.hooks.processOptions.push(function (plot, options) {
            if (options.figureLabel != "") {
                plot.hooks.drawBackground.push(function (plot, ctx) {
                    var options = plot.getOptions();
                    var div = plot.getPlaceholder();

                    ctx.save();
                    ctx.font = "bold 20px sans-serif";
                    ctx.textAlign = 'center';
                    ctx.fillText(options.figureLabel, div.width() / 2, 35);
                    ctx.restore();
                });
            }
        });
    }

    $.plot.plugins.push({
        init: init,
        options: options,
        name: 'figureLabel',
        version: '1.0'
    });

    var socketHandler = initSocket();

    $.contextMenu({
        selector: '.plotWindow',
        callback: function (key, options) {
            socketHandler.onContextMenuItemClicked(this.parent(".plotContext").attr("id"), key);
        },
        items: {
            "csvExport": { name: "Export to CSV" },
            "pngExport": { name: "Export to PNG" },
            "resetZoom": { name: "Reset zoom" }
        }
    }, true);

    var progressForm = $("#progress-form").dialog({
        autoOpen: false,
        height: 180,
        width: 185,
        modal: true,
        resizable: false,
        buttons: {
            Cancel: function () {
                progressForm.dialog("close");
            }
        },
        open: function (event, ui) {
            $(".ui-dialog-titlebar-close").hide();
        }
    });

    var emptyPlotForm = $("#empty-plot-form").dialog({
        autoOpen: false,
        height: 110,
        width: 245,
        modal: true,
        resizable: false,
        draggable: false,
        closeOnEscape: false,
        buttons: {},
        open: function (event, ui) {
            $(".ui-dialog-titlebar-close").hide();
        }
    });
});

var WebSocketHandler = (function () {
    function WebSocketHandler(url) {
        var _this = this;
        this.hasResizeRequest = false;
        this.isThrottled = false;
        this.maxResizeFrequency = 30;
        this.hasBeenOpened = false;
        this.hasEmptyWindow = false;
        console.log('Creating websocket with url \'' + url + '\'');

        this.socket = agx.CreateWebSocket(url);
        this.nextMessageId = 1;
        this.promiseTable = {};

        this.socket.onopen = function (event) {
            _this.onOpen(event);
        };
        this.socket.onclose = function (event) {
            _this.onClose(event);
        };
        this.socket.onerror = function (event) {
            _this.onError(event);
        };
        this.socket.onmessage = function (event) {
            _this.onMessage(event);
        };

        this.plotWindows = {};

        window.onresize = function (event) {
            _this.onResize();
        };
    }
    WebSocketHandler.prototype.onResize = function () {
        var _this = this;
        this.hasResizeRequest = true;

        if (this.isThrottled)
            return;

        var body = $("body");
        var minSize = 300;
        var plotMargin = 2 * parseInt($(".plotContext").css("margin-left"));
        var paddingWidth = parseInt($(".plotContext").css("padding-left")) + parseInt($(".plotContext").css("padding-right")) + plotMargin;
        var paddingHeight = parseInt($(".plotContext").css("padding-top")) + parseInt($(".plotContext").css("padding-bottom")) + plotMargin;
        var plotWindowKeys = Object.keys(this.plotWindows);
        var numWindows = plotWindowKeys.length;

        var bodyMargin = 2 * parseInt($("body").css("margin-left"));
        var windowWidth = $(window).width() - bodyMargin;
        var windowHeight = $(window).height() - bodyMargin;

        var numColumns = 1;
        var numRows = numWindows;
        var maxColumns = Math.floor(windowWidth / minSize);
        var maxRows = Math.floor(windowHeight / minSize);

        var plotContextWidth;
        var plotContextHeight;
        var foundConfiguration = true;

        do {
            foundConfiguration = true;
            plotContextWidth = (windowWidth - numColumns * paddingWidth) / numColumns;
            plotContextHeight = (windowHeight - numRows * paddingHeight) / numRows;

            // Check if we can keep the windows stacked vertically.
            if (plotContextHeight < minSize) {
                // Try increasing the number of columns, but keep num rows and columns close to each other.
                if (numColumns < maxColumns && numColumns < numRows) {
                    numColumns++;
                    numRows = Math.ceil(numWindows / numColumns);

                    foundConfiguration = false;
                } else {
                    // Special case when two rows does not fit, increase width of window instead.
                    if (windowHeight < minSize * 2 && numColumns != numWindows) {
                        numColumns = numWindows;
                        numRows = 1;
                        windowWidth = Math.max(windowWidth, (minSize + paddingWidth) * numColumns);

                        foundConfiguration = false;
                    } else {
                        plotContextHeight = minSize;
                    }
                }
            } else if (numWindows == 1 && plotContextWidth < minSize) {
                // Special case, what about all stacked verically?
                plotContextWidth = minSize;
            }
        } while(!foundConfiguration);

        body.css({ "width": windowWidth });
        $(".plotContext").css({ "width": plotContextWidth, "height": plotContextHeight });

        for (var w = 0; w < plotWindowKeys.length; w++) {
            var plotWindow = this.plotWindows[plotWindowKeys[w]];
            plotWindow.requestRedraw();
        }

        $(".ui-dialog-content:visible").each(function () {
            $(this).dialog('option', 'position', $(this).dialog('option', 'position'));
        });

        $(".ui-widget-overlay").css({ "width": windowWidth, "height": windowHeight });

        this.hasResizeRequest = false;
        this.isThrottled = true;

        window.setTimeout(function () {
            _this.isThrottled = false;
            if (_this.hasResizeRequest)
                _this.onResize();
        }, 1000 / this.maxResizeFrequency);
    };

    /**
    Send a message and create a promise than will be signaled when a reply message
    is received.
    */
    /*  send(message : agx.StructuredMessage ) : agx.StructuredMessagePromise
    {
    message.setMessageId( this.nextMessageId );
    var promise = new agx.StructuredMessagePromise( message );
    this.promiseTable[this.nextMessageId] = promise;
    ++this.nextMessageId;
    this.socket.send( message.packet );
    return promise;
    }
    */
    WebSocketHandler.prototype.onOpen = function (event) {
        console.log("WebSocket.open");

        this.hasBeenOpened = true;
        // var context = plotManager.createPlotContext();
        // context.addPlotCurve(new agx.plot.DataCurve("RigidBody.velocity.z", 2));
        // context.addPlotCurve(new agx.plot.DataCurve("RigidBody.position.z", 2));
        //
        // console.log("Requesting plot data");
        // var dataRequestMessage = context.plotWindow.buildDataRequestMessage();
        // console.log(dataRequestMessage.headerString);
        // this.socket.send(dataRequestMessage.packet);
    };

    WebSocketHandler.prototype.onClose = function (event) {
        console.log("WebSocket.close");
        // THIS BREAKS DfSC! WHAT TO DO?!?!?!
        //window.close();
    };

    WebSocketHandler.prototype.onError = function (event) {
        console.log("WebSocket.error");

        if (!this.hasBeenOpened)
            this.addEmptyWindow();
    };

    WebSocketHandler.prototype.removeDisabledFigures = function () {
        var plotWindowKeysPost = Object.keys(this.plotWindows);
        var numEnabledWindows = 0;

        for (var i = 0; i < plotWindowKeysPost.length; i++)
            if (this.plotWindows[plotWindowKeysPost[i]].enabled)
                numEnabledWindows++;

        if (numEnabledWindows == 0)
            this.addEmptyWindow();
        else
            this.removeEmptyWindow();

        plotWindowKeysPost = Object.keys(this.plotWindows);
        for (var i = 0; i < plotWindowKeysPost.length; i++) {
            if (!this.plotWindows[plotWindowKeysPost[i]].enabled) {
                this.plotWindows[plotWindowKeysPost[i]].div.parent(".plotContext").remove();
                delete this.plotWindows[plotWindowKeysPost[i]];
            }
        }
    };

    WebSocketHandler.prototype.onMessage = function (event) {
        // Why does this happen? Only in Safari?
        if (event.data == '') {
            console.log('Got empty message in event ' + event);
            return;
        }

        var redraw = true;
        var message = agx.StructuredMessage.ParseMessage(event.data);

        //console.log("WebSocket.message: " + message.uri + " with response id " + message.preHeader.idResponse);
        var header = message.header;
        var plotWindowKeys = Object.keys(this.plotWindows);

        /*    console.log(JSON.stringify(header, undefined, 2));*/
        if (message.uri == "Plot.DataPacket") {
            if (header.length == 0)
                redraw = false;

            for (var i = 0; i < header.length; i++) {
                var point = header[i];

                for (var w = 0; w < plotWindowKeys.length; w++) {
                    var window = this.plotWindows[plotWindowKeys[w]];
                    var curve = window.getCurve(point.curveID);

                    //if (!curve) {
                    // curve = new agx.plot.DataCurve(point.curveID);
                    //  plotWindow.addCurve(curve);
                    //}
                    if (curve) {
                        curve.appendDataPoint(point.x, point.y);
                    }
                }
            }
        } else if (message.uri == "Plot.DescriptionPacket") {
            var info = header["info"];
            var windows = info["windows"];

            for (var i = 0; i < plotWindowKeys.length; i++) {
                this.plotWindows[plotWindowKeys[i]].enabled = false;
            }

            for (var i = 0; i < windows.length; i++) {
                var jsonWindow = windows[i];
                var windowName = jsonWindow["name"];
                var windowId = jsonWindow["id"];

                var shouldEnable = jsonWindow["enabled"] && jsonWindow["curves"].length != 0;

                var plotWindow = this.plotWindows[windowId];
                if (!plotWindow) {
                    if (!shouldEnable) {
                        continue;
                    } else {
                        plotWindow = initFlot(windowId, windowName);
                        this.plotWindows[windowId] = plotWindow;
                    }
                }

                plotWindow.enabled = shouldEnable;
                var plotCurveKeys = plotWindow.getIds();

                for (var j = 0; j < plotCurveKeys.length; j++) {
                    var curve = plotWindow.getCurve(plotCurveKeys[j]);
                    if (curve)
                        curve.enabled = false;
                }

                for (var j = 0; j < jsonWindow["curves"].length; j++) {
                    var shouldAddCurve = false;
                    var jsonCurve = jsonWindow["curves"][j];
                    var curve = plotWindow.getCurve(jsonCurve["id"]);
                    if (!curve) {
                        curve = new agx.plot.DataCurve(jsonCurve["id"]);
                        shouldAddCurve = true;
                    }

                    curve.title = jsonCurve["name"];
                    curve.setColor(jsonCurve["color_r"], jsonCurve["color_g"], jsonCurve["color_b"], jsonCurve["color_a"]);
                    curve.lineType = jsonCurve["lineType"];
                    curve.lineWidth = jsonCurve["lineWidth"];
                    curve.symbol = jsonCurve["symbol"];
                    curve.enabled = true;

                    var jsonXAxis = jsonCurve["xAxis"];
                    var jsonYAxis = jsonCurve["yAxis"];

                    curve.xTitle = jsonXAxis["name"];
                    curve.xUnit = jsonXAxis["unit"];
                    curve.xIsLogarithmic = jsonXAxis["isLogarithmic"];
                    curve.xIsTime = jsonXAxis["isTime"];

                    curve.yTitle = jsonYAxis["name"];
                    curve.yUnit = jsonYAxis["unit"];
                    curve.yIsLogarithmic = jsonYAxis["isLogarithmic"];

                    if (shouldAddCurve)
                        plotWindow.addCurve(curve);
                }
            }

            this.removeDisabledFigures();

            // Make sure that we recalculate where the different figures are placed
            this.onResize();
        } else if (message.uri == "Plot.RemoveCurve") {
            var id = header["curveID"];
            for (var w = 0; w < plotWindowKeys.length; w++) {
                var plotWindow = this.plotWindows[plotWindowKeys[w]];
                var curve = plotWindow.getCurve(id);
                if (curve) {
                    plotWindow.removeCurve(curve);
                    if (plotWindow.curves.length == 0) {
                        plotWindow.enabled = false;
                    }
                }
            }

            // Remove unused figures
            this.removeDisabledFigures();

            // Make sure that we recalculate where the different figures are placed
            this.onResize();
        } else if (message.uri == "Plot.TimePacket") {
            redraw = false;
            for (var w = 0; w < plotWindowKeys.length; w++)
                this.plotWindows[plotWindowKeys[w]].drawMarker(header.time);
        }

        if (redraw) {
            var plotWindowKeysPost = Object.keys(this.plotWindows);
            for (var w = 0; w < plotWindowKeysPost.length; w++)
                this.plotWindows[plotWindowKeysPost[w]].requestRedraw();
        }
    };

    WebSocketHandler.prototype.onContextMenuItemClicked = function (windowName, key) {
        var plotWindow = this.plotWindows[windowName];
        if (plotWindow) {
            switch (key) {
                case "csvExport":
                    plotWindow.csvExport();
                    break;

                case "pngExport":
                    plotWindow.pngExport();
                    break;

                case "resetZoom":
                    plotWindow.resetZoom();
                    break;

                default:
                    console.log('onContextMenuItemClicked called with unknown key: ' + key);
                    break;
            }
        }
    };

    WebSocketHandler.prototype.addEmptyWindow = function () {
        if (!this.hasEmptyWindow) {
            var plotWindow = initFlot(WebSocketHandler.EMPTY_WINDOW_NAME, WebSocketHandler.EMPTY_WINDOW_NAME);
            plotWindow.enabled = true;

            this.plotWindows[WebSocketHandler.EMPTY_WINDOW_NAME] = plotWindow;

            var curve = new agx.plot.DataCurve(0);
            curve.title = "Curve";

            curve.xTitle = "Time";
            curve.xUnit = "s";

            curve.yTitle = "";
            curve.yUnit = "";

            plotWindow.addCurve(curve);

            this.onResize();

            $("#empty-plot-form").dialog("open");

            this.hasEmptyWindow = true;
        } else {
            this.plotWindows[WebSocketHandler.EMPTY_WINDOW_NAME].enabled = true;
        }
    };

    WebSocketHandler.prototype.removeEmptyWindow = function () {
        if (this.hasEmptyWindow) {
            var plotWindow = this.plotWindows[WebSocketHandler.EMPTY_WINDOW_NAME];
            plotWindow.enabled = false;

            delete this.plotWindows[WebSocketHandler.EMPTY_WINDOW_NAME];
            $("[id='" + WebSocketHandler.EMPTY_WINDOW_NAME + "']").remove();

            $("#empty-plot-form").dialog("close");

            this.hasEmptyWindow = false;
        }
    };
    WebSocketHandler.EMPTY_WINDOW_NAME = "   Figure   ";
    return WebSocketHandler;
})();

function initSocket() {
    var port = agx.getQueryString('port');
    var webSocketUrl = "ws://" + location.hostname + ":" + port;

    return new WebSocketHandler(webSocketUrl);
}

function initFlot(label, name) {
    var plotBase = $("#plotMain");

    var domContext = $($("#plotContext").html());
    domContext.attr("id", label);
    plotBase.append(domContext);

    var plotDiv = $(".plotWindow", domContext);

    return new agx.plot.FlotWindow(plotDiv, name);
    /*  this.plotWindow.zoomCallback = (xRange) => { this.zoomCallback(xRange); };*/
    /*  plotDiv.dblclick(() => { this.resetZoomButtonCallback(); });*/
}

function initTranslator() {
    translator = {
        "exportingTitle": "Exporting...",
        "nothingToPlotTitle": "Nothing to plot",
        "nothingToPlotDescription": "Add Windows and Curves with the agxPlot API."
    };

    var editor = agx.getQueryString("editor");

    if (editor == "dfsc") {
        // This should probably be asked via the WebSocket, by a callback, so the string can be placed in the C# resource file,
        // but the WebSocket might not be opened (if no simulation is active), and the REST API is deprecated.
        translator["nothingToPlotDescription"] = "Add Figures and Curves with the Plot button in the Dynamics ribbon and then Simulate.";
    }

    $("#progress-form").attr("title", translator["exportingTitle"]);
    $("#empty-plot-form").attr("title", translator["nothingToPlotTitle"]);
    $("#empty-plot-form p").html(translator["nothingToPlotDescription"]);
}
