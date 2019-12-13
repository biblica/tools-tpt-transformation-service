// Get job ID & project name from script args
var jobId = app.scriptArgs.getValue("jobId");
var projectName = app.scriptArgs.getValue("projectName");
var bookFormat = app.scriptArgs.getValue("bookFormat");

// Set top-level base and output dirs
var idttDir = 'C:\\Work\\IDTT\\';
var idmlDir = 'C:\\Work\\IDML\\';
var pdfDir = 'C:\\Work\\PDF\\';

// Set project input dir and output file
var projectDir = idttDir + projectName + '\\';
var pdfPath = pdfDir + 'preview-' + jobId + '.pdf';

// Open input template and build PDF
var doc = app.open(idmlDir + 'preview-' + jobId + '.idml');
doc.preflightOptions.preflightOff = true;

for (var i = 1; i < doc.paragraphStyles.count(); i++) {
    if (doc.paragraphStyles[i].basedOn === "[No Paragraph Style]") {
        doc.paragraphStyles[i].composer = "Adobe World-Ready Paragraph Composer";
        doc.paragraphStyles[i].appliedLanguage = app.languagesWithVendors.itemByName("Japanese");
        doc.paragraphStyles[i].appliedFont = "MS Gothic"
    }
    if (doc.paragraphStyles[i].name === "DefaultHeading") {
        doc.paragraphStyles[i].composer = "Adobe World-Ready Paragraph Composer";
        doc.paragraphStyles[i].appliedLanguage = app.languagesWithVendors.itemByName("Japanese");
        doc.paragraphStyles[i].appliedFont = "MS Gothic"
    }
}

for (var i = 1; i < doc.characterStyles.count(); i++) {
    if (doc.characterStyles[i].appliedFont === "Myriad Pro") {
        doc.characterStyles[i].appliedFont = "MS Gothic";
        doc.characterStyles[i].appliedLanguage = app.languagesWithVendors.itemByName("Japanese");
    }
}

var spread = doc.spreads[1];
var masterPages = doc.masterSpreads[0].pages;
var masterLeftTextFrame = masterPages[0].textFrames.firstItem();
var masterRightTextFrame = masterPages[1].textFrames.firstItem();
var lastTextFrame = doc.pages.lastItem().textFrames.lastItem();

var layer = doc.layers.lastItem();
var pageItem = layer.pageItems.lastItem();
var placement = pageItem.place(projectDir + 'books-1.txt');
var story = placement[0];

// Manually add spreads and connect them to provide the rigth amount of space for the text
while (lastTextFrame.contents.length > 0) {
    var priorLastTF = lastTextFrame.previousTextFrame;
    priorLastTF.nextTextFrame = null;
    lastTextFrame.previousTextFrame = null;

    var newSpread = doc.spreads.add(LocationOptions.AFTER, spread);
    var newLeftTf = masterLeftTextFrame.duplicate(newSpread.pages[0]);
    var newRightTf = masterRightTextFrame.duplicate(newSpread.pages[1]);

    priorLastTF.nextTextFrame = newLeftTf;
    newLeftTf.nextTextFrame = newRightTf;
    lastTextFrame.previousTextFrame = newRightTf;

    spread = newSpread;
}
for (var p = doc.pages.length - 1; p >= 0; p--) {
    if (doc.pages[p].textFrames[0].contents.length == 0) {
        doc.pages[p].remove()
    }
}

doc.preflightOptions.preflightOff = false;

story.exportFile(ExportFormat.PDF_TYPE, pdfPath);
doc.close();
