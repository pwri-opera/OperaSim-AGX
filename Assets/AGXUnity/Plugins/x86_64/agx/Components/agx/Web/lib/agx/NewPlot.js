/// <reference path="Frame.ts"/>
/// <reference path="Math.ts"/>
/// <reference path="Timer.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};

var agx;
(function (agx) {
    (function (plot) {
        /**
        A data curve model. Does not know anything about rendering.
        */
        var DataCurve = (function () {
            function DataCurve(id, title) {
                if (typeof title === "undefined") { title = ""; }
                this.id = id;
                this.title = title;
                this.values = null;

                this.enabled = true;
                this.color = "#" + Math.random().toString(16).slice(2, 8);
                this.lineWidth = 1;
                this.lineType = 0;
                this.symbol = 0;

                this.xTitle = "";
                this.xUnit = "";
                this.xIsLogarithmic = false;
                this.xIsTime = false;

                this.yTitle = "";
                this.yUnit = "";
                this.yIsLogarithmic = false;
            }
            DataCurve.prototype.appendDataPoint = function (x, y) {
                if (!this.hasData()) {
                    this.xMin = x;
                    this.xMax = x;
                    this.yMin = y;
                    this.yMax = y;
                    this.values = [];
                }

                // console.log('appendDataPoint: ' + x + ', ' + y);
                if (this.xMin > x)
                    this.xMin = x;
                if (this.xMax < x)
                    this.xMax = x;
                if (this.yMin > y)
                    this.yMin = y;
                if (this.yMax < y)
                    this.yMax = y;
                this.values.push([x, y]);
            };

            DataCurve.prototype.hasData = function () {
                return this.values != null;
            };

            DataCurve.prototype.setColor = function (r, g, b, a) {
                var rStr = ("00" + Math.round(r * 255).toString(16)).substr(-2);
                var gStr = ("00" + Math.round(g * 255).toString(16)).substr(-2);
                var bStr = ("00" + Math.round(b * 255).toString(16)).substr(-2);
                this.color = "#" + rStr + gStr + bStr;
            };
            return DataCurve;
        })();
        plot.DataCurve = DataCurve;

        //////////////////////////////////////////////////////////////////////////////////////////////////
        /**
        Abstract plotting window.
        */
        var Window = (function () {
            function Window(name, id) {
                if (typeof name === "undefined") { name = ""; }
                this.maxRedrawFrequency = 30;
                this.hasRedrawRequest = false;
                this.isThrottled = false;
                if (agx.IsDefined(id)) {
                    // agx.Assert(id >= Window.idCounter);
                    this.id = id;
                    Window.idCounter = id + 1;
                } else {
                    this.id = Window.idCounter++;
                }

                this.name = name;
                this.savedName = null;
                this.curves = [];
                this.curveIdCounter = 0;
            }
            /**
            Add a curve to the plot window.
            */
            Window.prototype.addCurve = function (curve) {
                if (curve.id < 0)
                    curve.id = this.curveIdCounter++;

                var defaultColors = ["#edc240", "#afd8f8", "#cb4b4b", "#4da74d", "#9440ed"];
                if (curve.id < defaultColors.length)
                    curve.color = defaultColors[curve.id];

                if (curve.xIsTime)
                    this.xHasTime = true;

                this.curves.push(curve);
            };

            /**
            Remove the given curve from the window.
            \return True if the curve was found and removed. False otherwise.
            */
            Window.prototype.removeCurve = function (curve) {
                for (var i = 0; i < this.curves.length; ++i) {
                    if (this.curves[i] == curve) {
                        this.curves.splice(i, 1);
                        curve.id = -1;
                        return true;
                    }
                }
                return false;
            };

            /**
            \return The curve with the specified id.
            */
            Window.prototype.getCurve = function (id) {
                for (var i = 0; i < this.curves.length; ++i) {
                    if (this.curves[i].id == id)
                        return this.curves[i];
                }

                return null;
            };

            Window.prototype.getIds = function () {
                var ids = [];
                for (var i = 0; i < this.curves.length; ++i) {
                    ids.push(this.curves[i].id);
                }
                return ids;
            };

            /**
            Request the plot to redraw itself. Throttled to specified max frequency.
            */
            Window.prototype.requestRedraw = function () {
                var _this = this;
                this.hasRedrawRequest = true;

                if (this.isThrottled)
                    return;

                this.redraw(); // Actual drawing

                this.hasRedrawRequest = false;
                this.isThrottled = true;

                window.setTimeout(function () {
                    _this.isThrottled = false;
                    if (_this.hasRedrawRequest)
                        _this.requestRedraw();
                }, 1000 / this.maxRedrawFrequency);
            };

            /////////////////////////////////////////////
            // Always use requestRedraw
            Window.prototype.redraw = function () {
                agx.Abort("Must be overridden in subclass");
            };
            Window.idCounter = 0;
            return Window;
        })();
        plot.Window = Window;

        var FlotCurve = (function () {
            function FlotCurve(curve, xaxis) {
                this.yRange = { min: undefined, max: undefined };

                if (agx.IsDefined(curve) && curve.values && curve.values.length > 0) {
                    var xMin = (agx.IsDefined(xaxis) && xaxis.min) ? xaxis.min : curve.values[0][0];
                    var xMax = (agx.IsDefined(xaxis) && xaxis.max) ? xaxis.max : curve.values[curve.values.length - 1][0];

                    this.label = curve.title;
                    this.color = curve.color;
                    this.data = curve.values;
                    this.xUnit = curve.xUnit;
                    this.yUnit = curve.yUnit;
                }
            }
            return FlotCurve;
        })();
        plot.FlotCurve = FlotCurve;

        /**
        A representation of a Flot instance. It is created inside a given HTML
        div. The window holds a number of curves that can be drawn in the plot
        view.
        */
        var FlotWindow = (function (_super) {
            __extends(FlotWindow, _super);
            /**
            Create a new, empty, plot window in the given HTML div.
            */
            function FlotWindow(targetDiv, name, id) {
                if (typeof name === "undefined") { name = ""; }
                var _this = this;
                _super.call(this, name, id);
                this.maxPanRedrawFrequency = 60;
                this.isExportingCsv = false;
                this.curves = [];
                this.div = targetDiv;
                this.label = name;
                this.leftButtonDown = false;
                this.rightButtonDown = false;
                this.xMarkerDiv = null;
                this.xMarker = null;
                this.plotOptions = {
                    canvas: true,
                    points: {},
                    lines: {},
                    legend: {
                        show: true,
                        position: "ne"
                    },
                    series: {
                        // shadowSize: 0, // drawing is faster without shadows
                        points: {
                            show: false,
                            radius: 0.5
                        },
                        lines: {
                            show: true
                        }
                    },
                    crosshair: {
                        mode: "x"
                    },
                    // zoom: {
                    //   interactive: true
                    // },
                    selection: { mode: "xy" },
                    grid: {
                        hoverable: true,
                        clickable: false,
                        autoHighlight: false,
                        margin: {
                            top: 50
                        }
                    },
                    xaxis: {
                        zoomRange: [null, null],
                        panRange: [null, null],
                        min: null,
                        max: null,
                        tickFormatter: function (val, axis) {
                            return agx.toReadableNumber(val);
                        }
                    },
                    yaxis: {
                        zoomRange: [null, null],
                        panRange: [null, null],
                        min: null,
                        max: null,
                        tickFormatter: function (val, axis) {
                            return agx.toReadableNumber(val);
                        }
                    },
                    figureLabel: name
                };

                this.detailDataRange = null;

                this.flot = $.plot(this.div, [], this.plotOptions);

                this.updateLegendTimeout = null;
                this.latestPosition = null;

                targetDiv.bind("plothover", function (event, pos, item) {
                    _this.latestPosition = pos;
                    if (!_this.updateLegendTimeout && !_this.leftButtonDown && !_this.rightButtonDown)
                        _this.updateLegendTimeout = setTimeout(function () {
                            _this.updateLegend();
                        }, 50);
                });

                targetDiv.bind("mouseleave", function () {
                    setTimeout(function () {
                        $("#toolTip").hide();
                    }, 50);
                });

                targetDiv.bind("plotselected", function (event, ranges) {
                    // console.log(ranges);
                    if (_this.curves.length > 0) {
                        _this.plotOptions.xaxis.min = ranges.xaxis.from;
                        _this.plotOptions.xaxis.max = ranges.xaxis.to;
                        _this.plotOptions.yaxis.min = ranges.yaxis.from;
                        _this.plotOptions.yaxis.max = ranges.yaxis.to;

                        if (_this.zoomCallback) {
                            _this.resetPan(_this.plotOptions.xaxis);
                            _this.zoomCallback(_this.plotOptions.xaxis);
                        }
                    }

                    _this.requestRedraw();
                });

                // targetDiv.on("drag", (event) =>
                // {
                //   console.log("drag: " + event.pageX + ", " + event.pageY);
                // });
                targetDiv.on("dblclick", function (event) {
                    _this.resetZoom();
                });

                targetDiv.on("mousewheel", function (event, delta) {
                    event.preventDefault();

                    var c = _this.flot.offset();
                    c.left = event.pageX - c.left;
                    c.top = event.pageY - c.top;

                    if (delta > 0)
                        _this.flot.zoom({ center: c });
                    else
                        _this.flot.zoomOut({ center: c });

                    if (_this.xMarker)
                        _this.drawMarker(_this.xMarker);

                    _this.synchronizeAxis();
                });

                targetDiv.on("mousedown", function (event) {
                    // console.log("down which: " + event.which);
                    if (event.which == 1)
                        _this.leftButtonDown = true;

                    if (event.which == 3)
                        _this.rightButtonDown = true;

                    _this.prevPageX = event.pageX;
                    _this.prevPageY = event.pageY;

                    event.preventDefault();
                });

                $(document).bind("mousemove", function (event) {
                    if (_this.leftButtonDown)
                        $("#toolTip").hide();

                    if (_this.rightButtonDown) {
                        if (_this.isPanThrottled)
                            return;

                        $("#toolTip").hide();

                        // console.log("move: " + event.pageX + ", " + event.pageY);
                        _this.flot.pan({
                            left: _this.prevPageX - event.pageX,
                            top: _this.prevPageY - event.pageY });
                        _this.prevPageX = event.pageX;
                        _this.prevPageY = event.pageY;

                        _this.synchronizeAxis();

                        // var dataRange = this.getCurveDataRange();
                        //
                        // // Clamp panning to data range
                        // if (this.plotOptions.xaxis.min < dataRange.min)
                        // {
                        //   var violation = dataRange.min - this.plotOptions.xaxis.min;
                        //   this.plotOptions.xaxis.max += violation;
                        //   this.plotOptions.xaxis.min = dataRange.min;
                        //   this.requestRedraw();
                        // }
                        //
                        // if (this.plotOptions.xaxis.max > dataRange.max)
                        // {
                        //   var violation = this.plotOptions.xaxis.max - dataRange.max;
                        //   this.plotOptions.xaxis.min -= violation;
                        //   this.plotOptions.xaxis.max = dataRange.max;
                        //   this.requestRedraw();
                        // }
                        if (_this.xMarker)
                            _this.drawMarker(_this.xMarker);

                        _this.isPanThrottled = true;

                        window.setTimeout(function () {
                            _this.isPanThrottled = false;
                        }, 1000 / _this.maxPanRedrawFrequency);
                    }
                });

                $(document).bind("mouseup", function (event) {
                    if (event.which == 1)
                        _this.leftButtonDown = false;

                    // console.log("up which: " + event.which);
                    if (event.which == 3) {
                        _this.rightButtonDown = false;

                        if (_this.zoomCallback) {
                            _this.synchronizeAxis();
                            var range = _this.updateDetailRange(_this.plotOptions.xaxis);
                            if (range)
                                _this.zoomCallback(range);
                        }
                    }

                    event.preventDefault();
                });

                $(document).bind("mouseout", function (event) {
                    if (event.toElement == null && event.relatedTarget == null) {
                        _this.leftButtonDown = false;
                        _this.rightButtonDown = false;
                    }

                    $("#toolTip").hide();
                });
            }
            FlotWindow.prototype.updateLegend = function () {
                this.updateLegendTimeout = null;

                var pos = this.latestPosition;

                var i, j, dataset = this.flot.getData();
                var x, y, series, distance, found = false, hasRight = true;
                for (i = 0; i < dataset.length; ++i) {
                    var locseries = dataset[i];

                    var locx, locy;
                    var locHasRight = true;
                    var locDistance = Number.POSITIVE_INFINITY;
                    var isInsideCurve = false;

                    if (locseries.data.length == 1) {
                        // Special case when there is only a single point.
                        locx = locseries.data[0][0];
                        locy = locseries.data[0][1];
                    }

                    for (j = 1; j < locseries.data.length; ++j) {
                        var currentPoint = locseries.data[j - 1];
                        var nextPoint = locseries.data[j];

                        // Check that the cursor is between two points.
                        if (pos.x > currentPoint[0] && pos.x < nextPoint[0] || pos.x > nextPoint[0] && pos.x < currentPoint[0]) {
                            // Interpolate.
                            var interpolatedY = currentPoint[1] + (nextPoint[1] - currentPoint[1]) * (pos.x - currentPoint[0]) / (nextPoint[0] - currentPoint[0]);

                            var interpolatedOffset = this.flot.pointOffset({ x: pos.x, y: interpolatedY, xaxis: 1, yaxis: 1 });
                            var posOffset = this.flot.pointOffset({ x: pos.x, y: pos.y, xaxis: 1, yaxis: 1 });
                            var screenDistance = Math.abs(interpolatedOffset.top - posOffset.top);

                            if (screenDistance < locDistance) {
                                isInsideCurve = true;

                                locx = pos.x;
                                locy = interpolatedY;
                                locDistance = screenDistance;
                            }
                        } else if (!isInsideCurve) {
                            if (pos.x < currentPoint[0] && j == 1) {
                                // Special case when the cursor is to the left of the curve.
                                locx = currentPoint[0];
                                locy = currentPoint[1];
                            } else if (pos.x > nextPoint[0] && j == locseries.data.length - 1) {
                                // Special case when the cursor is to the right of the curve.
                                locx = nextPoint[0];
                                locy = nextPoint[1];

                                locHasRight = false;
                            }
                        }
                    }

                    if (locDistance == Number.POSITIVE_INFINITY) {
                        var locOffset = this.flot.pointOffset({ x: locx, y: locy, xaxis: 1, yaxis: 1 });
                        var posOffset = this.flot.pointOffset({ x: pos.x, y: pos.y, xaxis: 1, yaxis: 1 });
                        locDistance = Math.sqrt(Math.pow(locOffset.left - posOffset.left, 2) + Math.pow(locOffset.top - posOffset.top, 2));
                    }

                    if (!found) {
                        distance = locDistance;
                        x = locx;
                        y = locy;
                        series = locseries;
                        hasRight = locHasRight;
                        found = true;
                    } else if (locDistance < distance || locHasRight && !hasRight) {
                        // A curve without enough points can have a smaller distance, so
                        // prioritize curves with points to the right of pointer.
                        // A curve without enough points can have a smaller distance, so
                        // prioritize curves with points to the right of pointer.
                        distance = locDistance;
                        x = locx;
                        y = locy;
                        series = locseries;
                        hasRight = locHasRight;
                    }
                }

                if (found) {
                    var toolTip = $("#toolTip");
                    var flotOffset = this.flot.offset();
                    var offset = this.flot.pointOffset({ x: x, y: y, xaxis: 1, yaxis: 1 });
                    offset.top += this.div.offset().top - 50;
                    offset.left += this.div.offset().left + 10;

                    toolTip.html(series.label + "<br />x = " + agx.toReadableNumber(x) + " " + series.xUnit + "<br />y = " + agx.toReadableNumber(y) + " " + series.yUnit);

                    var padding = 5;
                    var width = toolTip.width() + 2 * padding;
                    var height = toolTip.height() + 2 * padding;
                    var flotWidth = this.flot.width();
                    var flotHeight = this.flot.height();

                    if (offset.top < flotOffset.top + padding)
                        offset.top = flotOffset.top + padding;
                    if (offset.top + height > flotOffset.top + flotHeight)
                        offset.top = flotOffset.top - height + flotHeight;

                    if (offset.left < flotOffset.left + padding)
                        offset.left = flotOffset.left + padding;
                    if (offset.left + width > flotOffset.left + flotWidth)
                        offset.left = flotOffset.left - width + flotWidth;

                    toolTip.css(offset).fadeIn(200);
                }
            };

            FlotWindow.prototype.csvExport = function () {
                this.isExportingCsv = true;

                var numElements = 0;
                var enabledCurves = [];
                var progressForm = $("#progress-form");
                var progress = progressForm.find(".progress");
                progress.html("0%");
                progressForm.dialog("open");

                for (var i = 0; i < this.curves.length; ++i) {
                    var curve = this.curves[i];
                    if (curve.enabled) {
                        numElements = Math.max(curve.values.length, numElements);
                        enabledCurves.push(curve);
                    }
                }

                if (enabledCurves.length > 0) {
                    var flotWindow = this;

                    progressForm.bind('dialogclose', function (event) {
                        // Check if the export was aborted.
                        if (flotWindow.isExportingCsv) {
                            flotWindow.isExportingCsv = false;

                            newPlotCsvWorker.terminate();

                            // This will unfortunately fail if AGX has shutdown, but otherwise it is impossible to abort a slow export.
                            newPlotCsvWorker = new Worker("../lib/agx/NewPlotCsvWorker.js");
                        }

                        progressForm.unbind('dialogclose');
                    });

                    newPlotCsvWorker.onmessage = function (e) {
                        var data = e.data;

                        switch (data.cmd) {
                            case "progress":
                                progress.html(data.progress + "%");
                                break;

                            case "finished":
                                var array = new Array();
                                array[0] = data.csv;
                                var blob = new Blob(array, { type: "application/csv;charset=utf-8" });
                                var blobURL = URL.createObjectURL(blob);

                                flotWindow.downloadURL(blobURL, "csv");

                                setTimeout(function () {
                                    URL.revokeObjectURL(blobURL);
                                }, 1000);

                                flotWindow.isExportingCsv = false;
                                progressForm.dialog("close");
                                break;

                            default:
                                console.log("Got unknown command from CSV worker: " + data.cmd);
                        }
                    };

                    var newLine = "\n";
                    if (window.navigator.platform.indexOf("Win") != -1)
                        newLine = "\r" + newLine;

                    newPlotCsvWorker.postMessage({ enabledCurves: enabledCurves, numElements: numElements, newLine: newLine });
                } else {
                    progressForm.dialog("close");
                }
            };

            FlotWindow.prototype.pngExport = function () {
                var dataURL = $("canvas", this.div)[0].toDataURL("image/png");
                this.downloadURL(dataURL, "png");
            };

            FlotWindow.prototype.downloadURL = function (url, ending) {
                var name = this.name;
                if (name == "")
                    name = "Unnamed";

                var a = $("<a>").css({ "display": "none" }).attr("href", url).attr("download", name + "." + ending).text("Download");
                $("body").append(a);

                setTimeout(function () {
                    a.get(0).click();
                    a.remove();
                }, 100);
            };

            FlotWindow.prototype.resetZoom = function () {
                this.plotOptions.xaxis.min = null;
                this.plotOptions.xaxis.max = null;
                this.plotOptions.yaxis.min = null;
                this.plotOptions.yaxis.max = null;
                this.requestRedraw();
            };

            FlotWindow.prototype.synchronizeAxis = function () {
                var axis = this.flot.getAxes();

                this.plotOptions.xaxis.min = axis.xaxis.min;
                this.plotOptions.xaxis.max = axis.xaxis.max;
                this.plotOptions.yaxis.min = axis.yaxis.min;
                this.plotOptions.yaxis.max = axis.yaxis.max;
            };

            FlotWindow.prototype.printRange = function (range) {
                return range.min + ':' + range.max;
            };

            FlotWindow.prototype.updateDetailRange = function (range) {
                if (!this.detailDataRange) {
                    this.detailDataRange = { min: range.min, max: range.max };
                    return range;
                }

                // console.log('range: ' + this.printRange(range));
                // console.log('detailDataRange: ' + this.printRange(this.detailDataRange));
                // Check if inside current detail range
                if (range.min >= this.detailDataRange.min && range.max <= this.detailDataRange.max)
                    return null;

                var result = { min: range.min, max: range.max };

                // Clamp requested range and extend total detail range
                if (range.min < this.detailDataRange.min) {
                    agx.Assert(range.max > this.detailDataRange.min && range.max < this.detailDataRange.max);
                    result.max = this.detailDataRange.min;
                    this.detailDataRange.min = range.min;
                } else if (range.max > this.detailDataRange.max) {
                    agx.Assert(range.min < this.detailDataRange.max && range.min > this.detailDataRange.min);
                    result.min = this.detailDataRange.max;
                    this.detailDataRange.max = range.max;
                } else {
                    console.log('updateDetailRange failed!!');
                    console.log(range);
                    console.log(this.detailDataRange);
                    this.detailDataRange = { min: range.min, max: range.max };
                }

                // console.log('result: ' + this.printRange(result));
                // console.log('updated detailDataRange: ' + this.printRange(this.detailDataRange));
                return result;
            };

            FlotWindow.prototype.resetPan = function (range) {
                this.detailDataRange = range == undefined ? null : { min: range.min, max: range.max };
            };

            FlotWindow.prototype.drawMarker = function (xPos) {
                if (!this.xHasTime)
                    // Do not draw the marker if no curve on the x axis is time.
                    return;

                if (this.xMarkerDiv) {
                    this.xMarkerDiv.remove();
                    this.xMarkerDiv = null;
                }

                var plotOffset = this.flot.getPlotOffset();
                var height = this.div.height() - plotOffset.top - plotOffset.bottom;
                var canvasPoint = this.flot.p2c({ x1: xPos, y1: 0 });
                var divX = canvasPoint.left + plotOffset.left;

                // Make sure marker is within plot bounds
                if (divX >= plotOffset.left && divX <= this.div.width() - plotOffset.right) {
                    // var markerWidth = 1;
                    // var cssShadow = "rgb(100, 100, 100) -1px 0px 0px 0px";
                    // var cssGradient = "background: -moz-linear-gradient(left,  rgba(255,255,255,0) 0%, rgba(0,0,0,1) 100%);";
                    // cssGradient += "background: -webkit-gradient(linear, left top, right top, color-stop(0%,rgba(255,255,255,0)), color-stop(100%,rgba(0,0,0,1)));";
                    // cssGradient += "background: -webkit-linear-gradient(left,  rgba(255,255,255,0) 0%,rgba(0,0,0,1) 100%);";
                    // cssGradient += "background: -o-linear-gradient(left,  rgba(255,255,255,0) 0%,rgba(0,0,0,1) 100%);";
                    // cssGradient += "background: -ms-linear-gradient(left,  rgba(255,255,255,0) 0%,rgba(0,0,0,1) 100%);";
                    // cssGradient += "background: linear-gradient(to right,  rgba(255,255,255,0) 0%,rgba(0,0,0,1) 100%);";
                    // this.xMarkerDiv = $("<div style=\"width:" + markerWidth + "px; height:" + height + "px; " + cssGradient + "position:absolute; left:" + divX + "px;top:" + plotOffset.top + "px;\"><div style=\"width:2px; height:" + height + "px; background:rgb(100,100,100); position:absolute; left:" + (markerWidth-1) + "px;top:0px;\"></div></div>");
                    this.xMarkerDiv = $("<div style=\"width:1px; height:" + height + "px; background:black; position:absolute; left:" + divX + "px;top:" + plotOffset.top + "px;\"></div>");
                    this.div.append(this.xMarkerDiv);
                }

                this.xMarker = xPos;
            };

            ////////////////////////////////////////////////////////////////////////////
            // Private: use requestRedraw
            FlotWindow.prototype.redraw = function () {
                var totalTime = 0;

                this.activeCurves = new Array();

                var yRange = {};
                var xAxisTitle = "";
                var diffXAxisTitle = false;
                var xAxisUnit = "";
                var diffXAxisUnit = false;
                var xAxisLog = false;
                var diffXAxisLog = false;
                var yAxisLog = false;
                var diffYAxisLog = false;
                var yAxisTitle = "";
                var diffYAxisTitle = false;
                var yAxisUnit = "";
                var diffYAxisUnit = false;

                var first = true;

                var diffWidths = false;
                var width = 1;

                var diffLineType = false;
                var lineType = 0;

                var diffSymbols = false;
                var symbol = 0;

                var xMin = 0;
                var xMax = 0;
                var yMin = 0;
                var yMax = 0;

                for (var i = 0; i < this.curves.length; ++i) {
                    var curve = this.curves[i];

                    if (curve.enabled) {
                        if (first) {
                            width = curve.lineWidth;
                            lineType = curve.lineType;
                            symbol = curve.symbol;

                            xAxisTitle = curve.xTitle;
                            xAxisUnit = curve.xUnit;
                            xAxisLog = curve.xIsLogarithmic;
                            yAxisTitle = curve.yTitle;
                            yAxisUnit = curve.yUnit;
                            yAxisLog = curve.yIsLogarithmic;

                            xMin = curve.xMin;
                            xMax = curve.xMax;
                            yMin = curve.yMin;
                            yMax = curve.yMax;

                            first = false;
                        } else {
                            if (!diffWidths && Math.abs(width - curve.lineWidth) > 0.001) {
                                width = 1;
                                diffWidths = true;
                            }
                            if (!diffLineType && lineType != curve.lineType) {
                                lineType = 0;
                            }
                            if (!diffSymbols && symbol != curve.symbol) {
                                symbol = 3;
                                diffSymbols = true;
                            }
                            if (!diffXAxisTitle && xAxisTitle != curve.xTitle) {
                                xAxisTitle = "";
                                diffXAxisTitle = true;
                            }
                            if (!diffXAxisUnit && xAxisUnit != curve.xUnit) {
                                xAxisUnit = "";
                                diffXAxisUnit = true;
                            }
                            if (!diffXAxisLog && xAxisLog != curve.xIsLogarithmic) {
                                xAxisLog = false;
                                diffXAxisLog = true;
                            }
                            if (!diffYAxisTitle && yAxisTitle != curve.yTitle) {
                                yAxisTitle = "";
                                diffYAxisTitle = true;
                            }
                            if (!diffYAxisUnit && yAxisUnit != curve.yUnit) {
                                yAxisUnit = "";
                                diffYAxisUnit = true;
                            }
                            if (!diffYAxisLog && yAxisLog != curve.yIsLogarithmic) {
                                yAxisLog = false;
                                diffYAxisLog = true;
                            }
                            if (curve.xMin < xMin) {
                                xMin = curve.xMin;
                            }
                            if (curve.xMax > xMax) {
                                xMax = curve.xMax;
                            }
                            if (curve.yMin < yMin) {
                                yMin = curve.yMin;
                            }
                            if (curve.yMax > yMax) {
                                yMax = curve.yMax;
                            }
                        }
                    }
                }

                var funX = null;
                var iFunX = null;
                var funY = null;
                var iFunY = null;
                var xTicks = 6;
                var yTicks = 6;
                if (xAxisLog) {
                    funX = function (v) {
                        if (v == 0)
                            return null;
                        return Math.log(v);
                    };
                    iFunX = function (v) {
                        return Math.exp(v);
                    };
                    var diff = xMax - xMin;
                    var lnDiff = Math.log(diff);
                    var lnStep = lnDiff / 5.0;

                    xTicks = [xMin, xMin + Math.exp(lnStep * 1), xMin + Math.exp(lnStep * 2), xMin + Math.exp(lnStep * 3), xMin + Math.exp(lnStep * 4), xMax];
                }
                if (yAxisLog) {
                    funY = function (v) {
                        if (v == 0)
                            return null;
                        return Math.log(v);
                    };
                    iFunY = function (v) {
                        return Math.exp(v);
                    };
                    var diff = yMax - yMin;
                    var lnDiff = Math.log(diff);
                    var lnStep = lnDiff / 5.0;

                    yTicks = [yMin, yMin + Math.exp(lnStep * 1), yMin + Math.exp(lnStep * 2), yMin + Math.exp(lnStep * 3), yMin + Math.exp(lnStep * 4), yMax];
                }

                var labelX;
                if (xAxisTitle == "") {
                    labelX = xAxisUnit;
                } else {
                    if (xAxisUnit == "") {
                        labelX = xAxisTitle;
                    } else {
                        labelX = xAxisTitle + "(" + xAxisUnit + ")";
                    }
                }
                var labelY;
                if (yAxisTitle == "") {
                    labelY = yAxisUnit;
                } else {
                    if (yAxisUnit == "") {
                        labelY = yAxisTitle;
                    } else {
                        labelY = yAxisTitle + "(" + yAxisUnit + ")";
                    }
                }

                this.plotOptions.points.show = symbol != 3;
                this.plotOptions.points.radius = width;
                this.plotOptions.lines.show = lineType != 2;
                this.plotOptions.lines.lineWidth = width;

                this.plotOptions.xaxis.tickFormatter = function (val, axis) {
                    return agx.toReadableNumber(val) + " " + xAxisUnit;
                };

                this.plotOptions.xaxis.ticks = xTicks;
                this.plotOptions.xaxis.transform = funX;
                this.plotOptions.xaxis.inverseTransform = iFunX;
                this.plotOptions.xaxis.axisLabel = labelX;
                this.plotOptions.xaxis.axisLabelUseCanvas = true;
                this.plotOptions.xaxis.axisLabelPadding = 8;

                this.plotOptions.yaxis.tickFormatter = function (val, axis) {
                    return agx.toReadableNumber(val) + " " + yAxisUnit;
                };

                this.plotOptions.yaxis.ticks = yTicks;
                this.plotOptions.yaxis.transform = funY;
                this.plotOptions.yaxis.inverseTransform = iFunY;
                this.plotOptions.yaxis.axisLabel = labelY;
                this.plotOptions.yaxis.axisLabelUseCanvas = true;
                this.plotOptions.yaxis.axisLabelPadding = 8;

                for (var i = 0; i < this.curves.length; ++i) {
                    var curve = this.curves[i];

                    if (curve.enabled) {
                        var flotCurve = new FlotCurve(curve, this.plotOptions.xaxis);
                        this.activeCurves.push(flotCurve);
                        // if (i == 0)
                        // {
                        //   yRange.min = flotCurve.yRange.min;
                        //   yRange.max = flotCurve.yRange.max;
                        // }
                        // else
                        // {
                        //   yRange.min = Math.min(yRange.min, flotCurve.yRange.min);
                        //   yRange.max = Math.max(yRange.max, flotCurve.yRange.max);
                        // }
                    }
                }

                // Explicity handle yRange (for xMarker to work properly)
                // var yAxis = this.plotOptions.yaxis;
                // var explicitAxis = false;
                // if (!yAxis.min && !yAxis.max)
                // {
                //   explicitAxis = true;
                //   var rangeLength = yRange.max - yRange.min;
                //   var margin = rangeLength * 0.1;
                //   yRange.min -= margin;
                //   yRange.max += margin;
                //   yAxis.min = yRange.min;
                //   yAxis.max = yRange.max;
                // }
                this.flot = $.plot(this.div, this.activeCurves, this.plotOptions);

                if (this.xMarker)
                    this.drawMarker(this.xMarker);
                // if (explicitAxis)
                // {
                //   yAxis.min = null;
                //   yAxis.max = null;
                // }
            };
            return FlotWindow;
        })(agx.plot.Window);
        plot.FlotWindow = FlotWindow;
    })(agx.plot || (agx.plot = {}));
    var plot = agx.plot;
})(agx || (agx = {}));
