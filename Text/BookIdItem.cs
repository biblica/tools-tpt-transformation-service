/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using CsvHelper.Configuration.Attributes;

namespace TptMain.Text
{
    /// <summary>
    /// Model class for the master list of book numbers, codes, titles, and descriptions.
    /// </summary>
    public class BookIdItem
    {
        /// <summary>
        /// Book number (1-based).
        /// </summary>
        [Index(0)]
        public int BookNum { get; }

        /// <summary>
        /// USFM book number text (e.g., "B2").
        /// </summary>
        [Index(1)]
        public string UsfmBookNum { get; }

        /// <summary>
        /// Unique book code (e.g., "GEN" for "Genesis").
        /// </summary>
        [Index(2)]
        public string BookCode { get; }

        /// <summary>
        /// Non-unique book title (e.g., "Genesis", "Extra material").
        /// </summary>
        [Index(3)]
        public string BookTitle { get; }

        /// <summary>
        /// Book description text (optional, may be null).
        /// </summary>
        [Index(4)]
        [Default(null)]
        public string BookComment { get; }

        /// <summary>
        /// Default ctor.
        /// </summary>
        /// <param name="bookNum">Book number (1-based).</param>
        /// <param name="usfmBookNum">USFM book number text (e.g., "B2"; required).</param>
        /// <param name="bookCode">Book code (e.g., "GEN", "XXA"; required).</param>
        /// <param name="bookName">Non-unique book title (e.g., "Genesis", "Extra material"; required).</param>
        /// <param name="bookComment">Book description text (optional, may be null).</param>
        public BookIdItem(int bookNum, string usfmBookNum, string bookCode, string bookName, string bookComment)
        {
            BookNum = bookNum;
            UsfmBookNum = usfmBookNum ?? throw new ArgumentNullException(nameof(usfmBookNum));
            BookCode = bookCode ?? throw new ArgumentNullException(nameof(bookCode));
            BookTitle = bookName ?? throw new ArgumentNullException(nameof(bookName));
            BookComment = bookComment;
        }

        /// <summary>
        /// Private ctor for serialization.
        /// </summary>
        private BookIdItem()
        {
        }
    }
}
