/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace TptMain.Models
{
    /// <summary>
    /// Defines the parameters to be used for a typesetting preview
    /// </summary>
    public class TypesettingParams
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Font size in points.
        /// </summary>
        public float? FontSizeInPts { get; set; }

        /// <summary>
        /// Font leading in points.
        /// </summary>
        public float? FontLeadingInPts { get; set; }

        /// <summary>
        /// Page width in points.
        /// </summary>
        public float? PageWidthInPts { get; set; }

        /// <summary>
        /// Page height in points.
        /// </summary>
        public float? PageHeightInPts { get; set; }

        /// <summary>
        /// Page header in points.
        /// </summary>
        public float? PageHeaderInPts { get; set; }

        /// <summary>
        /// Book format, either TBOTB or CAV.
        /// </summary>
        public BookFormat? BookFormat { get; set; }

        /// <summary>
        /// (Optional) Whether or not to use hyphenation. Defaults to false.
        /// </summary>
        public bool UseHyphenation { get; set; } = false;
        
        /// <summary>
        /// (Optional) Whether or not to use custom footnotes. Defaults to false.
        /// 
        /// Note: The footnotes would be pulled from the project's respective Paratext footnote caller sequence.
        /// </summary>
        public bool UseCustomFootnotes { get; set; } = false;

        /// <summary>
        /// (Optional) Whether or not to use the project's defined font. Defaults to true.
        /// </summary>
        public bool UseProjectFont { get; set; } = true;

        /// <summary>
        /// Whether to include Headings
        /// </summary>
        public bool IncludeHeadings { get; set; }

        /// <summary>
        /// Whether to include Footnotes
        /// </summary>
        public bool IncludeFootnotes { get; set; }

        /// <summary>
        /// Whether to include Chapter Numbers
        /// </summary>
        public bool IncludeChapterNumbers { get; set; }

        /// <summary>
        /// Whether to include Verse Numbers
        /// </summary>
        public bool IncludeVerseNumbers { get; set; }

        /// <summary>
        /// Whether to include ParallelPassages
        /// </summary>
        public bool IncludeParallelPassages { get; set; }

        /// <summary>
        /// Whether to include Acrostic Poetry
        /// </summary>
        public bool IncludeAcrosticPoetry { get; set; }
    }
}