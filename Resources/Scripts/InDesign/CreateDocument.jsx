/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
// Add helper functions
#include "addCustomFootnotes.jsxinc";
#include "updateFont.jsxinc";

// Retrieve parameters from the script arguments
var txtFilePathIn = app.scriptArgs.getValue("txtFilePath");
var idmlPathIn = app.scriptArgs.getValue("idmlPath");
var docOutputPathIn = app.scriptArgs.getValue("docOutputPath");
var customFootnotesIn = app.scriptArgs.getValue("customFootnoteList");
var overrideFontIn = app.scriptArgs.getValue("overrideFont");
var textDirection = app.scriptArgs.getValue("textDirection");

// Create the INDD
createDocument(txtFilePathIn, idmlPathIn, docOutputPathIn, customFootnotesIn, overrideFontIn, textDirection);

/**
 * This function creates and InDesign Document (INDD) from a template (IDML) and tagged text (IDTT).
 * @param {string} txtFilePath The file path of the tagged text to place into the INDD
 * @param {string} idmlPath The file path where the IDML is located
 * @param {string} docOutputPath The file path where the INDD should be output
 * @param {string} customFootnotes Any custom footnotes as a list
 * @param {string} overrideFont A font to use rather than what is specified in the IDML
 * @param {string} textDirection The direction of the character orientation or text. EG: RTL, LTR, UNSPECIFIED.
 */
function createDocument(txtFilePath, idmlPath, docOutputPath, customFootnotes, overrideFont, textDirection) {
    // Load the IDML into the new INDD and turn off checking/auto-modifying capabilities to speed up performance
    var doc = app.open(idmlPath);
    doc.preflightOptions.preflightOff = true;

    // If an override font was specified, update the fonts in the IDML
    if (typeof overrideFont === 'string' && overrideFont.length) {
        updateFont(doc, overrideFont);
    }

    for (var i = 1; i < doc.paragraphStyles.count(); i++) {
        // The composer should always be Adobe World-Ready.
        doc.paragraphStyles[i].composer = "Adobe World-Ready Paragraph Composer";

        // Address when the text direction is RTL
        if ("RTL" == textDirection) {
            doc.paragraphStyles[i].paragraphDirection = ParagraphDirectionOptions.RIGHT_TO_LEFT_DIRECTION;
            // flip justification for RTL.
            if (doc.paragraphStyles[i].justification == Justification.LEFT_ALIGN) {
                doc.paragraphStyles[i].justification = Justification.RIGHT_ALIGN;
            }
            if (doc.paragraphStyles[i].justification == Justification.LEFT_JUSTIFIED) {
                doc.paragraphStyles[i].justification = Justification.RIGHT_JUSTIFIED;
            }
        }
    }

    // Set the story direction to RTL when appropriate.
    if ("RTL" == textDirection) {
        doc.storyPreferences.storyDirection = StoryDirectionOptions.RIGHT_TO_LEFT_DIRECTION;
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