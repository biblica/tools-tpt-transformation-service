/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace TptMain.Models
{
    /// <summary>
    /// Defines parameters that select which books of the Bible to include in a Preview Job
    /// </summary>
    public class BibleSelectionParams
    {
        /// <summary>
        /// Unique identifier for params
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The Paratext short project name
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// A comma-separated list of Bible books to include
        /// </summary>
        public string SelectedBooks { get; set; }
        
        /// <summary>
        /// Whether to include ancillary material
        /// </summary>
        public bool IncludeAncillary { get; set; } = false;
    }
}