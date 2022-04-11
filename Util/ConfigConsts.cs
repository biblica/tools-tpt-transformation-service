/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace TptMain.Util
{
    /// <summary>
    /// Constants of Configuration parameter keys.
    /// </summary>
    public static class ConfigConsts
    {
        /// <summary>
        /// Processed Job Files Root Directory Key
        /// </summary>
        public const string ProcessedJobFilesRootDirKey = "Docs:Job:Processed:RootDirectory";

        /// <summary>
        /// Paratext directory config key.
        /// </summary>
        public const string ParatextDocDirKey = "Docs:Paratext:Directory";

        /// <summary>
        /// Max document age in seconds config key.
        /// </summary>
        public const string MaxDocAgeInSecKey = "Docs:MaxAgeInSec";

        /// <summary>
        /// Max document upload size in bytes config key.
        /// </summary>
        public const string MaxDocUploadSizeInBytesKey = "Docs:Upload:MaxSizeInBytes";

        /// <summary>
        /// Max doc uploads per request config key.
        /// </summary>
        public const string MaxDocUploadsPerRequestKey = "Docs:Upload:MaxUploadsPerRequest";

        /// <summary>
        /// Max project name length for uploaded projects config key
        /// (Paratext-sourced projects have their restrictions).
        /// </summary>
        public const string MaxProjectNameLengthForUploadsKey = "Docs:Upload:MaxProjectNameLength";

        /// <summary>
        /// Max username length for uploaded projects config key
        /// (Paratext-sourced projects have their restrictions).
        /// </summary>
        public const string MaxUserNameLengthForUploadsKey = "Docs:Upload:MaxUserNameLength";

        /// <summary>
        /// Job processing interval in seconds config key.
        /// </summary>
        public const string JobProcessIntervalInSecKey = "Jobs:Processing:ProcessIntervalInSec";

        /// <summary>
        /// InDesign servers section config key.
        /// </summary>
        public const string IdsServersSectionKey = "InDesign:Servers";

        /// <summary>
        /// InDesign server request timeout config key.
        /// </summary>
        public const string IdsTimeoutInSecKey = "InDesign:TimeoutInSec";

        /// <summary>
        /// InDesign server preview script config key.
        /// </summary>
        public const string IdsPreviewScriptDirKey = "InDesign:PreviewScriptDirectory";

        /// <summary>
        /// Paratext Server URI param key.
        /// </summary>
        public const string ParatextApiServerUriKey = "Paratext:API:ServerUri";

        /// <summary>
        /// Paratext API Username param key.
        /// </summary>
        public const string ParatextApiUsernameKey = "Paratext:API:Username";

        /// <summary>
        /// Paratext API Password param key.
        /// </summary>
        public const string ParatextApiPasswordKey = "Paratext:API:Password";

        /// <summary>
        /// Paratext API ProjectCacheAgeInSec param key.
        /// </summary>
        public const string ParatextApiProjectCacheAgeInSecKey = "Paratext:API:ProjectCacheAgeInSec";

        /// <summary>
        /// Paratext API Password param key.
        /// </summary>
        public const string ParatextApiAllowedMemberRolesKey = "Paratext:API:AllowedMemberRoles";

        /// <summary>
        /// Template generation timeout config key.
        /// </summary>
        public const string TemplateGenerationTimeoutInSecKey = "Job:Template:TimeoutInSec";

        /// <summary>
        /// TaggedText generation timeout config key.
        /// </summary>
        public const string TaggedTextGenerationTimeoutInSecKey = "Job:TaggedText:TimeoutInSec";

    }
}