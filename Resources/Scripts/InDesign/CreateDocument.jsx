// Add helper functions
#include "addCustomFootnotes.jsxinc";
#include "updateFont.jsxinc";

// Retrieve parameters from the script arguments
var txtFilePathIn = app.scriptArgs.getValue("txtFilePath");
var idmlPathIn = app.scriptArgs.getValue("idmlPath");
var docOutputPathIn = app.scriptArgs.getValue("docOutputPath");
var customFootnotesIn = app.scriptArgs.getValue("customFootnoteList");
var overrideFontIn = app.scriptArgs.getValue("overrideFont");

// Create the INDD
createDocument(txtFilePathIn, idmlPathIn, docOutputPathIn, customFootnotesIn, overrideFontIn);

/**
 * This function creates and InDesign Document (INDD) from a template (IDML) and tagged text (IDTT).
 * @param {string} txtFilePath The file path of the tagged text to place into the INDD
 * @param {string} idmlPath The file path where the IDML is located
 * @param {string} docOutputPath The file path where the INDD should be output
 * @param {string} customFootnotes Any custom footnotes as a list
 * @param {string} overrideFont A font to use rather than what is specified in the IDML
 */
function createDocument(txtFilePath, idmlPath, docOutputPath, customFootnotes, overrideFont) {
    // Load the IDML into the new INDD and turn off checking/auto-modifying capabilities to speed up performance
    var doc = app.open(idmlPath);
    doc.preflightOptions.preflightOff = true;

    // If an override font was specified, update the fonts in the IDML
    if (typeof overrideFont === 'string' && overrideFont.length) {
        updateFont(doc, overrideFont);
    }

    // Place IDTT text into the new document
    var txtFile = new File(txtFilePath);
    var layer = doc.layers.lastItem();
    var pageItem = layer.pageItems.lastItem();
    var placement = pageItem.place(txtFile);

    // Add any custom footnotes
    if (customFootnotes && customFootnotes.length > 0) {
        addCustomFootnotes(doc, customFootnotes);
    }

    // Re-enable preflight and smart-reflow for the typesetters' benefit
    doc.preflightOptions.preflightOff = false;

    // Save the INDD
    doc.save(docOutputPath);
    doc.close(SaveOptions.YES);
}