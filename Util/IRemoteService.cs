/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace TptMain.Util
{
    /// <summary>
    /// This interface defines a class that communicates with a remote repository.
    /// </summary>
    public interface IRemoteService
    {
        /// <summary>
        /// This method retrieves a file as a <c>Stream</c>.
        /// </summary>
        /// <param name="filename">The filename to retrieve.</param>
        /// <returns>A <c>Stream</c> containing the file data.</returns>
        public Stream GetFileStream(string filename);

        /// <summary>
        /// This method retrieves all the available filenames.
        /// </summary>
        /// <returns>The available filenames.</returns>
        public List<string> ListAllFiles(string prefix);

        /// <summary>
        /// This method uploads a file.
        /// </summary>
        /// <param name="filename">The name the file will have in the remote repository.</param>
        /// <param name="filestream">The file as a <c>Stream</c>.</param>
        /// <returns>The <c>HttpStatusCode</c> returned by the remote repository.</returns>
        HttpStatusCode PutFileStream(string filename, Stream filestream);

        /// <summary>
        /// This method asynchonously uploads a file.
        /// </summary>
        /// <param name="filename">The name the file will have in the remote repository.</param>
        /// <param name="filestream">The file as a <c>Stream</c>.</param>
        /// <returns>An operation that results in the <c>HttpStatusCode</c> returned by the remote repository.</returns>
        Task<HttpStatusCode> PutFileStreamAsync(string filename, Stream filestream);

        /// <summary>
        /// This method deletes a file.
        /// </summary>
        /// <param name="filename">The name of the file to delete from the remote repository.</param>
        /// <returns>The <c>HttpStatusCode</c> returned by the remote repository.</returns>
        HttpStatusCode DeleteFile(string filename);
    }
}
