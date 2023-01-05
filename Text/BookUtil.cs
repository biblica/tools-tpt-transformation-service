/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;

namespace TptMain.Text
{
    /// <summary>
    /// Bible text-related utilities.
    /// </summary>
    public class BookUtil
    {
        /// <summary>
        /// Book id list, from resource file.
        /// </summary>
        public static readonly IList<BookIdItem> BookIdList;

        /// <summary>
        /// Map of book codes to IDs.
        /// </summary>
        public static readonly IDictionary<string, BookIdItem> BookIdsByCode;

        /// <summary>
        /// Map of book codes to USX composite keys. EG: GEN => 001GEN
        /// </summary>
        public static readonly IDictionary<string, string> UsxCompKeyByCode;

        /// <summary>
        /// Map of book numbers (1-based) to IDs.
        /// </summary>
        public static readonly IDictionary<int, BookIdItem> BookIdsByNum;

        /// <summary>
        /// List of Ancillary books by their canonical book ID.
        /// </summary>
        public static readonly List<string> AncillaryBooks = new List<string>()
        {
            "FRT",
            "BAK",
            "OTH",
            "INT",
            "CNC",
            "GLO",
            "TDX",
            "NDX",
            "XXA",
            "XXB",
            "XXC",
            "XXD",
            "XXE",
            "XXF",
            "XXG"
        };

        static BookUtil()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            using var streamReader = new StreamReader(executingAssembly.GetManifestResourceStream("TptMain.Resources.book-ids-1.csv")!);

            var csvConfig = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = false,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null
            };

            using var csvReader = new CsvReader(streamReader, csvConfig);

            BookIdList = csvReader.GetRecords<BookIdItem>().ToImmutableList();
            BookIdsByCode = BookIdList.ToImmutableDictionary(idItem => idItem.BookCode);
            UsxCompKeyByCode = BookIdList.ToImmutableDictionary(
                keySelector: idItem => idItem.BookCode,
                elementSelector: idItem => $"{idItem.BookNum:000}{idItem.BookCode}");
            BookIdsByNum = BookIdList.ToImmutableDictionary(idItem => idItem.BookNum);
        }
    }
}
