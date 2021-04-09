namespace TptMain.Models
{
    /// <summary>
    /// Defines the parameters to be used for a typesetting preview
    /// </summary>
    public class TypesettingParams
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Font size in points.
        /// </summary>
        public float? FontSizeInPts { get; set; }

        /// <summary>
        /// Font leading in points.
        /// </summary>
        public float? FontLeadingInPts { get; set; }

        /// <summary>
        /// Page width in points.
        /// </summary>
        public float? PageWidthInPts { get; set; }

        /// <summary>
        /// Page height in points.
        /// </summary>
        public float? PageHeightInPts { get; set; }

        /// <summary>
        /// Page header in points.
        /// </summary>
        public float? PageHeaderInPts { get; set; }

        /// <summary>
        /// Book format, either TBOTB or CAV.
        /// </summary>
        public BookFormat? BookFormat { get; set; }

        /// <summary>
        /// (Optional) Whether or not to use custom footnotes. Defaults to false.
        /// 
        /// Note: The footnotes would be pulled from the project's respective Paratext footnote caller sequence.
        /// </summary>
        public bool UseCustomFootnotes { get; set; } = false;

        /// <summary>
        /// (Optional) Whether or not to use the project's defined font. Defaults to true.
        /// </summary>
        public bool UseProjectFont { get; set; } = true;

        /// <summary>
        /// Whether to include Intros
        /// </summary>
        public bool IncludeIntros { get; set; }

        /// <summary>
        /// Whether to include Footnotes
        /// </summary>
        public bool IncludeFootnotes { get; set; }

        /// <summary>
        /// Whether to include Chapter Numbers
        /// </summary>
        public bool IncludeChapterNumbers { get; set; }

        /// <summary>
        /// Whether to include Verse Numbers
        /// </summary>
        public bool IncludeVerseNumbers { get; set; }

        /// <summary>
        /// Whether to include ParallelPassages
        /// </summary>
        public bool IncludeParallelPassages { get; set; }

        /// <summary>
        /// Whether to include Acrostic Poetry
        /// </summary>
        public bool IncludeAcrosticPoetry { get; set; }
    }
}