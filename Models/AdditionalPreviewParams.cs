/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

namespace TptMain.Models
{
    /// <summary>
    /// Model for tracking parameters derived from Paratext project settings.
    /// </summary>
    public class AdditionalPreviewParams
    {
        /// <summary>
        /// Unique identifier for params
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Project's custom footnote markers.
        /// </summary>
        public string CustomFootnoteMarkers { get; set; }

        /// <summary>
        /// Custom book list CSV of USX composite keys. EG: "001GEN,005DEU". Empty/null means all.
        /// </summary>
        public string CustomBookListUsxCompKeys { get; set; }

        /// <summary>
        /// The font we want to use in our preview generation instead; Otherwise, null to use the default.
        /// </summary>
        public string OverrideFont { get; set;}

        /// <summary>
        /// The text direction of the content.
        /// </summary>
        public TextDirection TextDirection { get; set; }
    }
}