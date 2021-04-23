// Retrieve parameters from the script arguments
var bookPathIn = app.scriptArgs.getValue("bookPath");
var pdfPathIn = app.scriptArgs.getValue("pdfPath");
var docPathIn = app.scriptArgs.getValue("docPath");
var docPatternIn = app.scriptArgs.getValue("docPattern");

// Create the INDB and PDF
createBook(bookPathIn, pdfPathIn, docPathIn, docPatternIn);

/**
 * This function creates an InDesign Book (INDB) from existing InDesign Documents (INDD), as well as a PDF of the INDB.
 * @param {string} bookPath This is the file path where the INDB should be created.
 * @param {string} pdfPath This is the file path where the PDF version of the INDB should be created.
 * @param {string} docPath This is the enclosing folder path for all previously-created INDD files that will be added to the INDB.
 * @param {string} docPattern This is the string pattern to use when identifying INDD files to be added to the INDB.
 */
function createBook(bookPath, pdfPath, docPath, docPattern) {

    // Create the book that INDDs will be added to
    var book = app.books.add(bookPath);
    book.automaticPagination = true;

    // Find & sort the INDDs to add to the book
    var docFolder = new Folder(docPath);
    var documents = docFolder.getFiles(docPattern);
    documents.sort(function (f1, f2) {
        return f1.name.localeCompare(f2.name);
    });

    // Add the INDDs to the book
    for (var ctr = 0; ctr < documents.length; ctr++) {
        book.bookContents.add(documents[ctr].fsName);
        book.save();
    }

    // Save the book & export it to PDF
    book.save();
    book.exportFile(ExportFormat.PDF_TYPE, pdfPath);

    // Close & exit
    book.close(SaveOptions.YES);
}