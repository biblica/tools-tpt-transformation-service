// Place a table in an (inline) frame and convert its 
// footnote numbers from Arabic to Roman.

/*-----------------------------------------------------------------------

1. Place an inline table in an inline frame.
2. Convert Arabic footnote references and numbers to Roman.

To use, place the cursor anywhere in a table and run the script.

The real footnote cues are hidden by a character style, 
the letters (i.e. the fake cues) are inserted immediately after the real cue.

-----------------------------------------------------------------------*/

/**
 * Add custom footnotes to an InDesign document.
 * @param {Document} doc InDesign Document object.
 * @param {string} footnoteMarkers CSV string of footnotes.
 */
function addCustomFootnotes(doc, footnoteMarkers) {

	if (!app.documents.length) exit();
	if (parseInt(app.version) < 14) {
		alert('This function works in CC2019 and later');
		exit();
	}

	// These are the symbols used as the new custom footnote markers.

	// Normalizing footnotes. EG: "'а, б, в, г, д, е, ж, з, и, й, к, л, м, н, о, п, р, с, т, у, ф, х, ц, ч, ш, щ, ы, э, ю, я'" -> "абвгдежзийклмнопрстуфхцчшщыэюя"
	var symbols = footnoteMarkers
		.replace(/[, ]/g, "");

	if (typeof symbols !== 'undefined') {
		symbols = symbols.split('');
		symbols.unshift('');
		var symbolsLe = symbols.length - 1;
	} else {
		symbols = '';
	}

	var sep = doc.footnoteOptions.separatorText;
	var table;

	// Style for the custom footnotes. We'll base it 
	// from the style set in the footnote options.
	// The script simply clones the style, the user
	// will have to add any format changes.
	var footnoteStyleName;

	// The style set in the footnote options, 
	// applied to the footnote references
	var footnoteReferenceStyleName = 'Table note reference';

	// The style used for the fake numbers in the notes
	var footnoteNumberStyleName = 'Table note number';

	// The style that hides the real footnote references and numbers
	var hideStyleName = 'hide';

	// Object style for inline tables
	var objectStyleName = 'Inline table';

	function addMissingStyles() {

		var fnoteStyle = doc.footnoteOptions.footnoteTextStyle;
		var fnoteStyleName = fnoteStyle.name.replace(/[\[\]]/g, '') + ' table';
		if (!doc.paragraphStyles.item(fnoteStyleName).isValid) {
			doc.paragraphStyles.add({
				name: fnoteStyleName,
				basedOn: fnoteStyle
			});
		}

		if (!doc.characterStyles.item(footnoteReferenceStyleName).isValid) {
			doc.characterStyles.add({
				name: footnoteReferenceStyleName,
				position: Position.SUPERSCRIPT
			});
		}

		footnoteReferenceStyleName = doc.characterStyles.item(footnoteReferenceStyleName);
		try {
			footnoteReferenceStyleName.fontStyle = 'Italic';
		} catch (_) {
		}

		if (!doc.characterStyles.item(footnoteNumberStyleName).isValid) {
			doc.characterStyles.add({
				name: footnoteNumberStyleName,
				basedOn: footnoteReferenceStyleName
			})
		}

		if (!doc.characterStyles.item(hideStyleName).isValid) {
			doc.characterStyles.add({
				name: hideStyleName,
				pointSize: 0.1,
				horizontaScale: 1
			});
		}

		if (!doc.objectStyles.item(objectStyleName).isValid) {
			doc.objectStyles.add({
				name: objectStyleName,
				basedOn: doc.objectStyles[2],
				enableStroke: false,
				strokeWeight: 0,
				enableAnchoredObjectOptions: true,
				anchoredObjectSettings: {
					anchoredPosition: AnchorPosition.INLINE_POSITION
				},
				enableTextFrameAutoSizingOptions: true,
				textFramePreferences: {
					autoSizingType: AutoSizingTypeEnum.HEIGHT_ONLY,
					autoSizingReferencePoint: AutoSizingReferenceEnum.TOP_CENTER_POINT
				}
			});
		};

		footnoteStyleName = doc.paragraphStyles.item(fnoteStyleName);
		footnoteNumberStyleName = doc.characterStyles.item(footnoteNumberStyleName);
		hideStyleName = doc.characterStyles.item(hideStyleName);
		objectStyleName = doc.objectStyles.item(objectStyleName);
	}

	//-------------------------------------------------------------------------
	// Converted some PHP code to JS to convert numbers to letters. Found at 
	// http://studiokoi.com/blog/article/converting_numbers_to_letters_quickly_in_php

	function numberToLetter(num) {
		num -= 1;
		var letter = String.fromCharCode(num % 26 + 97);
		if (num >= 26) {
			letter = numberToLetter(Math.floor(num / 26)) + letter;
		}
		return letter;
	}


	function numberToSymbol(n) {
		function numToSymbol(x, sym) {
			var s = '';
			sym = sym || symbols[symbolsLe];
			for (var i = 0; i < x; i++) {
				s += sym;
			}
			return s;
		}
		return numToSymbol(Math.floor(n / symbolsLe), symbols[n % symbolsLe]) + symbols[n % symbolsLe];
	}


	function getCue(n) {
		if (!symbols.length) {
			return numberToLetter(n);
		}
		return numberToSymbol(n);
	}

	//-------------------------------------------------------------
	// First undo any lettering, maybe we're updating a frame 
	// in which a notes were added or removed after the conversion.

	function undoLetters(table) {
		// Remove the contents of the cue style. InDesign then
		// removes the character-style instance
		app.findGrepPreferences = app.changeGrepPreferences = null;
		app.findGrepPreferences.appliedCharacterStyle = footnoteReferenceStyleName;
		table.changeGrep();

		// Then delete the note numbers.
		app.findGrepPreferences.appliedCharacterStyle = null;
		app.findGrepPreferences.findWhat = '^[a-z].*(?=~F)';

		table.changeGrep();
	}

	//----------------------------------------------------------
	// Add the letters at the cues and the numbers.

	function applyLetters(table) {

		var i, ix;
		var txt;
		//var fn = frame.parentStory.footnotes.everyItem().getElements();
		var fn = table.footnotes.everyItem().getElements();

		// 1. The table itself. Insert the letters and add their style,
		// and apply the hiding style to the cues.

		for (i = fn.length - 1; i >= 0; i--) {
			ix = fn[i].storyOffset.index;
			txt = fn[i].storyOffset.parent.texts[0];
			txt.insertionPoints[ix + 1].appliedCharacterStyle = footnoteReferenceStyleName;
			txt.insertionPoints[ix + 1].contents = getCue(i + 1);
			txt.characters[ix].appliedCharacterStyle = hideStyleName;
		}

		// 2. The notes. Insert the letter.
		app.findGrepPreferences = null;
		app.findGrepPreferences.findWhat = '~F' + sep;
		app.findChangeGrepOptions.includeFootnotes = true;
		fn = table.findGrep();
		for (i = fn.length - 1; i >= 0; i--) {
			fn[i].texts[0].applyParagraphStyle(footnoteStyleName, false);
			fn[i].insertionPoints[0].contents = getCue(i + 1);// + '\u2002';
			fn[i].paragraphs[0].characters[0].appliedCharacterStyle = footnoteNumberStyleName;
			fn[i].appliedCharacterStyle = hideStyleName;
		}
	}


	//--------------------------------------------------------------
	app.scriptPreferences.measurementUnit = MeasurementUnits.POINTS;

	// Create styles if necessary.
	addMissingStyles();

	// Grab each story, and apply custom footnote markers to the pre-existing footnoes.

	var storyElements = doc.stories.everyItem().getElements();

	for (var i = storyElements.length - 1; i >= 0; i--) {
		applyLetters(storyElements[i]);
	}
}