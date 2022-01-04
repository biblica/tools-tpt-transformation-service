/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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