using System;
using System.Collections.Generic;

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
        /// The font we want to use in our preview generation instead; Otherwise, null to use the default.
        /// </summary>
        public string OverrideFont { get; set;}

        /// <summary>
        /// The text direction of the content.
        /// </summary>
        public TextDirection TextDirection { get; set; }
    }
}