#include "CustomFootnotes.jsx";

///////////////////////////////////////////////////////////////////////////////
// This is an InDesign Server script used to generate previews of a publishing
// typeset. 
// 
// These previews are intended to:
// 1) Guide translators by helping them understand what changes during the 
//    translation process will look like in the typesetting process and to 
//    resolve surprises early.
// 2) Give typesetters a jumping off point for new translations.
// 3) Provide an early draft of a typeset for new translations.
//
// This script does require a few arguments to run successfully:
// 1) jobId: The job GUID that assist in tracking the associated InDesign
//    template file and to output a PDF with a matching job identifier.
//    Example: '096e0cf3-8d8a-4d11-b787-cce23ce2bad3'
// 2) projectName: The Bible translation Paratext project short name used to
//    locate and load the translation's tagged text content.
//    Example: 'spaNVI15'
// 3) bookFormat: The book output format. This impacts which template is used 
//    when generating a typsetting preview. As-of 2020-04-09, there's only two 
//    book formats: 'cav' and 'tbotb'.
// 3) customFootnoteList: List of custom footnotes as a CSV string in the 
//    order to be used. If not empty, will be used as markers in typesets. 
//    EG: "a,d,e,ñ,h,Ä"
///////////////////////////////////////////////////////////////////////////////

// Extract the preview job parameters from the script arguments.
var jobId = app.scriptArgs.getValue("jobId");
var projectName = app.scriptArgs.getValue("projectName");
var bookFormat = app.scriptArgs.getValue("bookFormat");
var customFootnotes = app.scriptArgs.getValue("customFootnoteList");

// Set top-level base and output dirs
var idttDir = 'C:\\Work\\IDTT\\';
var idmlDir = 'C:\\Work\\IDML\\';
var pdfDir = 'C:\\Work\\PDF\\';

// Set project input dir and output file
var txtDir = idttDir + bookFormat + '\\' + projectName + '\\';
var bookPath = idmlDir + 'preview-' + jobId + '.indb';
var pdfPath = pdfDir + 'preview-' + jobId + '.pdf';

// Create book to aggregate individual docs
var book = app.books.add(bookPath);
book.automaticPagination = true;

// Find & sort tagged text documents to read
var sortFunction = function sortFunction(f1, f2) {
    return f1.name.localeCompare(f2.name);
}

var txtFolder = new Folder(txtDir);
var txtFiles1 = txtFolder.getFiles("books-*.txt");
txtFiles1.sort(sortFunction);
var txtFiles2 = txtFolder.getFiles("book-*.txt");
txtFiles2.sort(sortFunction);
var txtFiles = txtFiles1.concat(txtFiles2);

// Create IDS document for each source, then add to book
for (var ctr = 0; ctr < txtFiles.length; ctr++) {

    var txtFile = txtFiles[ctr];
    var docPath = idmlDir + 'preview-' + jobId + '-' + txtFile.name.replace('.txt', '') + '.indd';

    // Load template as starting point
    var doc = app.open(idmlDir + 'preview-' + jobId + '.idml');

    // Optimizations: turning off checking/auto-modifying capabilities to speed up performance
    doc.preflightOptions.preflightOff = true;

    // Setup doc, composer, etc. for Japanese
    for (var i = 1; i < doc.paragraphStyles.count(); i++) {
        if (doc.paragraphStyles[i].basedOn === "[No Paragraph Style]") {
            doc.paragraphStyles[i].composer = "Adobe World-Ready Paragraph Composer";
            doc.paragraphStyles[i].appliedLanguage = app.languagesWithVendors.itemByName("Japanese");
            doc.paragraphStyles[i].appliedFont = "MS Gothic";
        }
        if (doc.paragraphStyles[i].name === "DefaultHeading") {
            doc.paragraphStyles[i].composer = "Adobe World-Ready Paragraph Composer";
            doc.paragraphStyles[i].appliedLanguage = app.languagesWithVendors.itemByName("Japanese");
            doc.paragraphStyles[i].appliedFont = "MS Gothic";
        }
    }
    for (var i = 1; i < doc.characterStyles.count(); i++) {
        if (doc.characterStyles[i].appliedFont === "Myriad Pro") {
            doc.characterStyles[i].appliedFont = "MS Gothic";
            doc.characterStyles[i].appliedLanguage = app.languagesWithVendors.itemByName("Japanese");
        }
    }

    try {
        // Place text
        var layer = doc.layers.lastItem();
        var pageItem = layer.pageItems.lastItem();
        var placement = pageItem.place(txtFile);

        // Add custom footnotes
        if (customFootnotes && customFootnotes.length > 0) {
            addCustomFootnotes(doc, customFootnotes);
        }

        // re-enable the preflight and smart-reflow for the typesetters benefit
        doc.preflightOptions.preflightOff = false;

        // Save INDD file
        doc.save(docPath);
        doc.close(SaveOptions.YES);

        // Add to book
        book.bookContents.add(docPath);
        book.save();
    } catch (ex) {
        throw "Can't create document: " + docPath + "\n"
        + "\tproject: " + projectName + "\n"
        + "\tformat: " + bookFormat + "\n"
        + "\tfootnotes: " + customFootnotes + "\n"
        + "\tcause: " + ex;
    }
}

// Save book & export to PDF
book.save();
book.exportFile(ExportFormat.PDF_TYPE, pdfPath);

// Close & exit
book.close(SaveOptions.YES);
