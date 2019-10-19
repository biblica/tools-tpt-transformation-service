var jobId = app.scriptArgs.getValue("jobId");
var projectName = app.scriptArgs.getValue("projectName");

// Change baseDir to match your environment
var baseDir = 'C:\\Work\\Projects\\';
var outputDir = 'C:\\Work\\Output\\';

var projectDir = baseDir + projectName + '\\';
var outputFile = 'C:\\Work\\Output\\preview-' + jobId + '.pdf';

var doc = app.open (baseDir + 'template_cav.idml');

doc.preflightOptions.preflightOff = true;

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
while( lastTextFrame.contents.length > 0) {
    var priorLastTF = lastTextFrame.previousTextFrame;
    priorLastTF.nextTextFrame = null;
    lastTextFrame.previousTextFrame = null;
    
    var newSpread = doc.spreads.add(LocationOptions.AFTER, spread);
    var newLeftTf = masterLeftTextFrame.duplicate(newSpread.pages[0]);
    var newRightTf  = masterRightTextFrame.duplicate(newSpread.pages[1]);
    
    priorLastTF.nextTextFrame = newLeftTf;
    newLeftTf.nextTextFrame = newRightTf;
    lastTextFrame.previousTextFrame = newRightTf;
   
    spread = newSpread;
}

for( var p = doc.pages.length - 1; p >= 0; p--){
    if(doc.pages[p].textFrames[0].contents.length == 0){
            doc.pages[p].remove()
     }
}

doc.preflightOptions.preflightOff = false;

story.exportFile(ExportFormat.PDF_TYPE, outputFile);
doc.close();
