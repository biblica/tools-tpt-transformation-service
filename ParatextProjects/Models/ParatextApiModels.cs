using System.Collections.Generic;

namespace TptMain.ParatextProjects.Models
{
    /// <summary>
    /// Enumeration of the available Paratext Project member roles.
    /// </summary>
    public enum MemberRole
    {
        pt_administrator,
        pt_consultant,
        pt_observer,
        pt_translator,
        pt_read,
        pt_write_note
    }

    /// <summary>
    /// Paratext Project Member model.
    /// </summary>
    public class ProjectMember
    {
        public MemberRole Role { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
    }

    /// <summary>
    /// Paratext Project model.
    /// </summary>
    public class Project
    {
        public List<IdentificationSystemId> Identification_SystemId { get; set; }
        public string Identification_ShortName { get; set; }
    }

    /// <summary>
    /// Paratext Identification System ID model.
    /// </summary>
    public class IdentificationSystemId
    {
        public string Type { get; set; }
        public string Text { get; set; }
        public string Name { get; set; }
        public string Fullname { get; set; }
    }
}
