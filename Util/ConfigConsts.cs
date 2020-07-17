using TptMain.Models;

namespace TptMain.Util
{
    /// <summary>
    /// Constants of Configuration parameter keys.
    /// </summary>
    public static class ConfigConsts
    {
        /// <summary>
        /// IDML directory config key.
        /// </summary>
        public const string IdmlDocDirKey = "Docs:IDML:Directory";

        /// <summary>
        /// IDTT directory config key.
        /// </summary>
        public const string IdttDocDirKey = "Docs:IDTT:Directory";

        /// <summary>
        /// Paratext directory config key.
        /// </summary>
        public const string ParatextDocDirKey = "Docs:Paratext:Directory";

        /// <summary>
        /// PDF directory config key.
        /// </summary>
        public const string PdfDocDirKey = "Docs:PDF:Directory";

        /// <summary>
        /// Zip directory config key.
        /// </summary>
        public const string ZipDocDirKey = "Docs:Zip:Directory";

        /// <summary>
        /// Max document age in seconds config key.
        /// </summary>
        public const string MaxDocAgeInSecKey = "Docs:MaxAgeInSec";

        /// <summary>
        /// Max concurrent jobs config key.
        /// </summary>
        public const string MaxConcurrentJobsKey = "Jobs:MaxConcurrent";

        /// <summary>
        /// InDesign server uri config key.
        /// </summary>
        public const string IdsUriKey = "InDesign:ServerUri";

        /// <summary>
        /// InDesign server request timeout config key.
        /// </summary>
        public const string IdsTimeoutInSecKey = "InDesign:TimeoutInSec";

        /// <summary>
        /// InDesign server preview script config key.
        /// </summary>
        public const string IdsPreviewScriptDirKey = "InDesign:PreviewScriptDirectory";

        /// <summary>
        /// InDesign server preview script name format config key.
        /// </summary>
        public const string IdsPreviewScriptNameFormatKey = "InDesign:PreviewScriptNameFormat";

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

    }
}