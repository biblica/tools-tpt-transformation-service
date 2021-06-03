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