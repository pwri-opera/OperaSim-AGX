onmessage = function (e) {
    var data = e.data;
    var enabledCurves = data.enabledCurves;
    var numElements = data.numElements;
    var newLine = data.newLine;
    var csv = "";
    var progress = 0;
    var minProgress = 1;
    var elementsPerPercent = Math.round(numElements / 100);

    if (elementsPerPercent < 1) {
        minProgress = 100 / numElements;
        elementsPerPercent = 1;
    }

    for (var i = 0; i < enabledCurves.length; ++i) {
        var curve = enabledCurves[i];
        if (csv != "")
            csv += ";";

        csv += curve.title + " - " + curve.xTitle;

        if (curve.xUnit != "")
            csv += " (" + curve.xUnit + ")";

        csv += ";" + curve.title + " - " + curve.yTitle;

        if (curve.yUnit != "")
            csv += " (" + curve.yUnit + ")";
    }

    for (var i = 0; i < numElements; ++i) {
        if (i % elementsPerPercent == 0) {
            progress += minProgress;
            postMessage({ cmd: "progress", progress: progress });
        }

        for (var j = 0; j < enabledCurves.length; ++j) {
            var curve = enabledCurves[j];

            if (j == 0)
                csv += newLine;
            else
                csv += ";";

            var x = "";
            var y = "";

            if (curve.values.length > i) {
                x = curve.values[i][0];
                y = curve.values[i][1];
            }

            csv += x + ";";
            csv += y;
        }
    }

    postMessage({ cmd: "finished", csv: csv });
};
