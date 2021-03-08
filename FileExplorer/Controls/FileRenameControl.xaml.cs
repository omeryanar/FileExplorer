using System;
using System.Collections.Generic;
using DevExpress.Data.Filtering;
using DevExpress.Xpf.Grid;
using FileExplorer.Model;

namespace FileExplorer.Controls
{
    public partial class FileRenameControl : GridControl
    {
        private HashSet<Int32> ManuallyEditedRowIndexList = new HashSet<Int32>();

        public FileRenameControl()
        {
            InitializeComponent();

            CustomUnboundColumnData += (s, e) =>
            {
                if (e.IsGetData && e.Column.FieldName == "RowNumber")
                    e.Value = e.Source.GetRowHandleByListIndex(e.ListSourceRowIndex) + 1;
                else if (e.Column.FieldName == "NewName")
                {
                    FileModel fileModel = GetRowByListIndex(e.ListSourceRowIndex) as FileModel;
                    if (fileModel != null)
                    {
                        if (e.IsSetData)
                        {
                            if (!ManuallyEditedRowIndexList.Contains(e.ListSourceRowIndex))
                                ManuallyEditedRowIndexList.Add(e.ListSourceRowIndex);

                            fileModel.Tag = e.Value;
                        }
                        else if (e.IsGetData)
                        {
                            if (ManuallyEditedRowIndexList.Contains(e.ListSourceRowIndex))
                                e.Value = fileModel.Tag;
                            else
                                fileModel.Tag = e.Value;
                        }
                    }                    
                }
            };

            FixedFilter = CriteriaOperator.Parse("[NewName] IS NULL OR [NewName] IS NOT NULL");
        }
    }
}
