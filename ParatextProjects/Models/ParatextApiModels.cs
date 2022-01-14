/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
