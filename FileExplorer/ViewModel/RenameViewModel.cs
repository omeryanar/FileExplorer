using System;
using DevExpress.Xpf.Editors;
using DevExpress.XtraEditors.DXErrorProvider;
using FileExplorer.Core;

namespace FileExplorer.ViewModel
{
    public class RenameViewModel
    {
        public virtual string Name { get; set; }

        public virtual string Parent { get; set; }

        public virtual string Extension { get; set; }

        public virtual DateTime DateCreated { get; set; }

        public virtual DateTime DateModified { get; set; }

        public virtual DateTime DateAccessed { get; set; }

        public virtual bool ShowPatternButtons { get; set; }

        public virtual bool ShowAdvancedOptions { get; set; }

        public void ValidateName(ValidationEventArgs e)
        {
            string value = e.Value?.ToString();

            if (String.IsNullOrWhiteSpace(value) || value.ContainsInvalidFileNameCharacters())
            {
                e.IsValid = false;
                e.ErrorType = ErrorType.Critical;
            }
        }

        public void ChangeFormat(string formatType)
        {
            switch (formatType)
            {
                case "Aa Aa":
                    Name = Name.TitleCase();
                    break;
                case "AA AA":
                    Name = Name.ToUpperInvariant();
                    break;
                case "Aa aa":
                    Name = Name.SentenceCase();
                    break;
                case "aa aa":
                    Name = Name.ToLowerInvariant();
                    break;
                case "aA aA":
                    Name = Name.ToggleCase();
                    break;
                case "Parent":
                    Name = Parent;
                    break;
            }
        }
    }
}
