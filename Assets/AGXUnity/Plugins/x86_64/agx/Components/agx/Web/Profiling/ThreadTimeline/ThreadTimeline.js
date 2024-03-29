

console.log("Page load.");

var svgRoot;

var jobName;
var jobTime;
var jobStartLabel;
var jobEndLabel;
var jobExtraData;
var labels = [];

var scrollX = 0;
var scrollY = 0;


function getChar(event) {
  if (event.which == null) {
    return String.fromCharCode(event.keyCode); // IE
  } else if (event.which!=0 && event.charCode!=0) {
    return String.fromCharCode(event.which);   // the rest
  } else {
    return null; // special key
  }
}



function scaleAttribute(tag, attribute, ratio)
{
    var currentValue = Number(tag.getAttribute(attribute));
    var newValue = ratio * currentValue;
    tag.setAttribute(attribute, newValue);
}

function scaleAttributes(tagName, attribute, ratio, optionalId)
{
    var allTags = document.getElementsByTagName(tagName);
    for(var i = 0; i < allTags.length; i++)
    {
        var tag = allTags[i]

        if (tag.getAttribute("no-scale"))
        {
            continue
        }

        if (optionalId != null)
        {
            var id = tag.getAttribute("id")
            if (id != optionalId)
                continue
        }
        scaleAttribute(tag, attribute, ratio)
    }
}



function setAttributes(tagName, haveAttribute, attribute, value)
{
    var allTags = document.getElementsByTagName(tagName)
    for (var i = 0; i < allTags.length; i++) {
        var tag = allTags[i]
        if (!tag.getAttribute(haveAttribute))
            continue;

        tag.setAttribute(attribute, value)
    }
}



function zoom(amount)
{
    scaleAttributes("rect", "x", amount)
    scaleAttributes("rect", "width", amount)
    scaleAttributes("line", "x1", amount)
    scaleAttributes("line", "x2", amount)
    scaleAttributes("text", "x", amount, "tickLabel")
    scaleAttribute(svgRoot, "width", amount)

    scrollX = amount * scrollX;
    window.scrollTo(scrollX, scrollY)
}



function zoomOn(sourceBox)
{
    jobStart = Number(sourceBox.getAttribute("x"));
    jobDuration = Number(sourceBox.getAttribute("width"));
    jobEnd = jobStart+jobDuration;

    scrollX = jobStart;
    neededZoom = window.innerWidth / (1.1 * jobDuration);
    zoom(neededZoom);
    window.scrollTo(scrollX-10, scrollY);
}



document.onkeypress = function(event)
{
    var key = getChar(event || window.event);
    if (!key)
        return;

    if (key == '+')
    {
        zoom(1.25);
    }
    else if (key == '-')
    {
        zoom(0.8);
    }
    else if (key == '*')
    {
        zoom(1.0141988);
    }
    else if (key == '/')
    {
        zoom(0.986);
    }
}


document.onscroll = function(event)
{
    scrollX = event.currentTarget.defaultView.pageXOffset;

    for (var i = 0; i < labels.length; ++i)
    {
        labels[i].setAttribute("x", scrollX+20);
    }

    setAttributes("rect", "no-scale", "x", scrollX)
}

function init(evt)
{
    console.log("Page init");

    svgRoot = document.getElementById("svgRoot");

    jobName = document.getElementById("jobName");
    jobTime = document.getElementById("jobTime");
    jobStartLabel = document.getElementById("jobStart");
    jobEndLabel = document.getElementById("jobEnd");
    jobExtraData = document.getElementById("jobExtraData");

    labels.push(jobName);
    labels.push(jobTime);
    labels.push(jobStartLabel);
    labels.push(jobEndLabel);
    labels.push(jobExtraData);

    for (var i = 0; i < 10; ++i) {
        labels.push(document.getElementById("expensiveTask_"+i));
    }
}

function s(sourceBox, name, runTime, startTime, endTime, extraDataTitle, extraData)
{
    sourceBox.setAttribute("stroke", "green");
    sourceBox.setAttribute("stroke-width", 5);
    jobName.firstChild.nodeValue = "Job: " + name;
    jobTime.firstChild.nodeValue = "Time: " + runTime;
    jobStartLabel.firstChild.nodeValue = "Start: " + startTime;
    jobEndLabel.firstChild.nodeValue = "End: " + endTime;
    if (extraDataTitle != "")
    {
        jobExtraData.firstChild.nodeValue = extraDataTitle + ": " + extraData;
    }
    else
    {
        jobExtraData.firstChild.nodeValue = "";
    }

    jobStart = Number(sourceBox.getAttribute("x"))
    jobDuration = Number(sourceBox.getAttribute("width"))
    jobEnd = jobStart+jobDuration

    jobStartLine = document.getElementById("jobStartLine")
    jobStartLine.setAttribute("x1", jobStart)
    jobStartLine.setAttribute("x2", jobStart)

    jobEndLine = document.getElementById("jobEndLine")
    jobEndLine.setAttribute("x1", jobEnd)
    jobEndLine.setAttribute("x2", jobEnd)

}

function c(sourceBox)
{
    sourceBox.setAttribute("stroke", "none")
    for (var i = 0; i < labels.length; ++i)
    {
        labels[i].nodeValue = ""
    }
}

