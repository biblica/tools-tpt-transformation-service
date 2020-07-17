using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TptMain.ParatextProjects.Models
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class ldml
    {

        private ldmlIdentity identityField;

        private ldmlCharacters charactersField;

        private ldmlDelimiters delimitersField;

        private ldmlLayout layoutField;

        private ldmlCollations collationsField;

        private ldmlSpecial specialField;

        /// <remarks/>
        public ldmlIdentity identity
        {
            get
            {
                return this.identityField;
            }
            set
            {
                this.identityField = value;
            }
        }

        /// <remarks/>
        public ldmlCharacters characters
        {
            get
            {
                return this.charactersField;
            }
            set
            {
                this.charactersField = value;
            }
        }

        /// <remarks/>
        public ldmlDelimiters delimiters
        {
            get
            {
                return this.delimitersField;
            }
            set
            {
                this.delimitersField = value;
            }
        }

        /// <remarks/>
        public ldmlLayout layout
        {
            get
            {
                return this.layoutField;
            }
            set
            {
                this.layoutField = value;
            }
        }

        /// <remarks/>
        public ldmlCollations collations
        {
            get
            {
                return this.collationsField;
            }
            set
            {
                this.collationsField = value;
            }
        }

        /// <remarks/>
        public ldmlSpecial special
        {
            get
            {
                return this.specialField;
            }
            set
            {
                this.specialField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlIdentity
    {

        private ldmlIdentityVersion versionField;

        private ldmlIdentityGeneration generationField;

        private ldmlIdentityLanguage languageField;

        /// <remarks/>
        public ldmlIdentityVersion version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        public ldmlIdentityGeneration generation
        {
            get
            {
                return this.generationField;
            }
            set
            {
                this.generationField = value;
            }
        }

        /// <remarks/>
        public ldmlIdentityLanguage language
        {
            get
            {
                return this.languageField;
            }
            set
            {
                this.languageField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlIdentityVersion
    {

        private string numberField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string number
        {
            get
            {
                return this.numberField;
            }
            set
            {
                this.numberField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn://www.sil.org/ldml/0.1")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "urn://www.sil.org/ldml/0.1", IsNullable = false)]
    public partial class exemplarCharacters
    {

        private string typeField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlIdentityGeneration
    {

        private System.DateTime dateField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.DateTime date
        {
            get
            {
                return this.dateField;
            }
            set
            {
                this.dateField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlIdentityLanguage
    {

        private string typeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlCharacters
    {

        private ldmlCharactersExemplarCharacters[] exemplarCharactersField;

        private exemplarCharacters[] specialField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("exemplarCharacters")]
        public ldmlCharactersExemplarCharacters[] exemplarCharacters
        {
            get
            {
                return this.exemplarCharactersField;
            }
            set
            {
                this.exemplarCharactersField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("exemplarCharacters", Namespace = "urn://www.sil.org/ldml/0.1", IsNullable = false)]
        public exemplarCharacters[] special
        {
            get
            {
                return this.specialField;
            }
            set
            {
                this.specialField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlCharactersExemplarCharacters
    {

        private string typeField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlDelimiters
    {

        private ldmlDelimitersSpecial specialField;

        private string quotationStartField;

        private string quotationEndField;

        private string alternateQuotationStartField;

        private string alternateQuotationEndField;

        /// <remarks/>
        public ldmlDelimitersSpecial special
        {
            get
            {
                return this.specialField;
            }
            set
            {
                this.specialField = value;
            }
        }

        /// <remarks/>
        public string quotationStart
        {
            get
            {
                return this.quotationStartField;
            }
            set
            {
                this.quotationStartField = value;
            }
        }

        /// <remarks/>
        public string quotationEnd
        {
            get
            {
                return this.quotationEndField;
            }
            set
            {
                this.quotationEndField = value;
            }
        }

        /// <remarks/>
        public string alternateQuotationStart
        {
            get
            {
                return this.alternateQuotationStartField;
            }
            set
            {
                this.alternateQuotationStartField = value;
            }
        }

        /// <remarks/>
        public string alternateQuotationEnd
        {
            get
            {
                return this.alternateQuotationEndField;
            }
            set
            {
                this.alternateQuotationEndField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlDelimitersSpecial
    {

        private matchedpairsMatchedpair[] matchedpairsField;

        private quotationmarks quotationmarksField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute("matched-pairs", Namespace = "urn://www.sil.org/ldml/0.1")]
        [System.Xml.Serialization.XmlArrayItemAttribute("matched-pair", IsNullable = false)]
        public matchedpairsMatchedpair[] matchedpairs
        {
            get
            {
                return this.matchedpairsField;
            }
            set
            {
                this.matchedpairsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("quotation-marks", Namespace = "urn://www.sil.org/ldml/0.1")]
        public quotationmarks quotationmarks
        {
            get
            {
                return this.quotationmarksField;
            }
            set
            {
                this.quotationmarksField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn://www.sil.org/ldml/0.1")]
    public partial class matchedpairsMatchedpair
    {

        private string openField;

        private string closeField;

        private bool paraCloseField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string open
        {
            get
            {
                return this.openField;
            }
            set
            {
                this.openField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string close
        {
            get
            {
                return this.closeField;
            }
            set
            {
                this.closeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool paraClose
        {
            get
            {
                return this.paraCloseField;
            }
            set
            {
                this.paraCloseField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn://www.sil.org/ldml/0.1")]
    [System.Xml.Serialization.XmlRootAttribute("quotation-marks", Namespace = "urn://www.sil.org/ldml/0.1", IsNullable = false)]
    public partial class quotationmarks
    {

        private string quotationContinueField;

        private string alternateQuotationContinueField;

        private quotationmarksQuotation[] quotationField;

        private string paraContinueTypeField;

        /// <remarks/>
        public string quotationContinue
        {
            get
            {
                return this.quotationContinueField;
            }
            set
            {
                this.quotationContinueField = value;
            }
        }

        /// <remarks/>
        public string alternateQuotationContinue
        {
            get
            {
                return this.alternateQuotationContinueField;
            }
            set
            {
                this.alternateQuotationContinueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("quotation")]
        public quotationmarksQuotation[] quotation
        {
            get
            {
                return this.quotationField;
            }
            set
            {
                this.quotationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string paraContinueType
        {
            get
            {
                return this.paraContinueTypeField;
            }
            set
            {
                this.paraContinueTypeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn://www.sil.org/ldml/0.1")]
    public partial class quotationmarksQuotation
    {

        private string openField;

        private string closeField;

        private string continueField;

        private byte levelField;

        private string typeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string open
        {
            get
            {
                return this.openField;
            }
            set
            {
                this.openField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string close
        {
            get
            {
                return this.closeField;
            }
            set
            {
                this.closeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string @continue
        {
            get
            {
                return this.continueField;
            }
            set
            {
                this.continueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte level
        {
            get
            {
                return this.levelField;
            }
            set
            {
                this.levelField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlLayout
    {

        private ldmlLayoutOrientation orientationField;

        /// <remarks/>
        public ldmlLayoutOrientation orientation
        {
            get
            {
                return this.orientationField;
            }
            set
            {
                this.orientationField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlLayoutOrientation
    {

        private string characterOrderField;

        /// <remarks/>
        public string characterOrder
        {
            get
            {
                return this.characterOrderField;
            }
            set
            {
                this.characterOrderField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlCollations
    {

        private string defaultCollationField;

        private ldmlCollationsCollation collationField;

        /// <remarks/>
        public string defaultCollation
        {
            get
            {
                return this.defaultCollationField;
            }
            set
            {
                this.defaultCollationField = value;
            }
        }

        /// <remarks/>
        public ldmlCollationsCollation collation
        {
            get
            {
                return this.collationField;
            }
            set
            {
                this.collationField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlCollationsCollation
    {

        private string crField;

        private ldmlCollationsCollationSpecial specialField;

        private string typeField;

        /// <remarks/>
        public string cr
        {
            get
            {
                return this.crField;
            }
            set
            {
                this.crField = value;
            }
        }

        /// <remarks/>
        public ldmlCollationsCollationSpecial special
        {
            get
            {
                return this.specialField;
            }
            set
            {
                this.specialField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlCollationsCollationSpecial
    {

        private string simpleField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "urn://www.sil.org/ldml/0.1")]
        public string simple
        {
            get
            {
                return this.simpleField;
            }
            set
            {
                this.simpleField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ldmlSpecial
    {

        private externalresourcesFont[] externalresourcesField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute("external-resources", Namespace = "urn://www.sil.org/ldml/0.1")]
        [System.Xml.Serialization.XmlArrayItemAttribute("font", IsNullable = false)]
        public externalresourcesFont[] externalresources
        {
            get
            {
                return this.externalresourcesField;
            }
            set
            {
                this.externalresourcesField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn://www.sil.org/ldml/0.1")]
    public partial class externalresourcesFont
    {

        private string nameField;

        private string typesField;

        private decimal sizeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string types
        {
            get
            {
                return this.typesField;
            }
            set
            {
                this.typesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal size
        {
            get
            {
                return this.sizeField;
            }
            set
            {
                this.sizeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn://www.sil.org/ldml/0.1")]
    [System.Xml.Serialization.XmlRootAttribute("matched-pairs", Namespace = "urn://www.sil.org/ldml/0.1", IsNullable = false)]
    public partial class matchedpairs
    {

        private matchedpairsMatchedpair[] matchedpairField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("matched-pair")]
        public matchedpairsMatchedpair[] matchedpair
        {
            get
            {
                return this.matchedpairField;
            }
            set
            {
                this.matchedpairField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn://www.sil.org/ldml/0.1")]
    [System.Xml.Serialization.XmlRootAttribute("external-resources", Namespace = "urn://www.sil.org/ldml/0.1", IsNullable = false)]
    public partial class externalresources
    {

        private externalresourcesFont[] fontField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("font")]
        public externalresourcesFont[] font
        {
            get
            {
                return this.fontField;
            }
            set
            {
                this.fontField = value;
            }
        }
    }
}
