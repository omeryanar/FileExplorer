using System;

namespace FileExplorer.Native
{
    public enum FOS : uint
    {
        FOS_PICKFOLDERS = 0x00000020,
        FOS_FORCEFILESYSTEM = 0x00000040,
        FOS_NOVALIDATE = 0x00000100,
        FOS_NOTESTFILECREATE = 0x00010000,
        FOS_DONTADDTORECENT = 0x02000000
    }

    public enum SIGDN : uint
    {
        SIGDN_NORMALDISPLAY = 0x00000000,
        SIGDN_PARENTRELATIVEPARSING = 0x80018001,
        SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8001c001,
        SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
        SIGDN_PARENTRELATIVEEDITING = 0x80031001,
        SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
        SIGDN_FILESYSPATH = 0x80058000,
        SIGDN_URL = 0x80068000
    }

    [Flags]
    public enum FileOperationFlags : uint
    {
        FOF_MULTIDESTFILES = 0x0001,
        FOF_CONFIRMMOUSE = 0x0002,
        FOF_SILENT = 0x0004,  // don't create progress/report
        FOF_RENAMEONCOLLISION = 0x0008,
        FOF_NOCONFIRMATION = 0x0010,  // Don't prompt the user.
        FOF_WANTMAPPINGHANDLE = 0x0020,  // Fill in SHFILEOPSTRUCT.hNameMappings
                                         // Must be freed using SHFreeNameMappings
        FOF_ALLOWUNDO = 0x0040,
        FOF_FILESONLY = 0x0080,  // on *.*, do only files
        FOF_SIMPLEPROGRESS = 0x0100,  // means don't show names of files
        FOF_NOCONFIRMMKDIR = 0x0200,  // don't confirm making any needed dirs
        FOF_NOERRORUI = 0x0400,  // don't put up error UI
        FOF_NOCOPYSECURITYATTRIBS = 0x0800,  // dont copy NT file Security Attributes
        FOF_NORECURSION = 0x1000,  // don't recurse into directories.
        FOF_NO_CONNECTED_ELEMENTS = 0x2000,  // don't operate on connected file elements.
        FOF_WANTNUKEWARNING = 0x4000,  // during delete operation, warn if nuking instead of recycling (partially overrides FOF_NOCONFIRMATION)
        FOF_NORECURSEREPARSE = 0x8000,  // treat reparse points as objects, not containers

        FOFX_NOSKIPJUNCTIONS = 0x00010000,  // Don't avoid binding to junctions (like Task folder, Recycle-Bin)
        FOFX_PREFERHARDLINK = 0x00020000,  // Create hard link if possible
        FOFX_SHOWELEVATIONPROMPT = 0x00040000,  // Show elevation prompts when error UI is disabled (use with FOF_NOERRORUI)
        FOFX_EARLYFAILURE = 0x00100000,  // Fail operation as soon as a single error occurs rather than trying to process other items (applies only when using FOF_NOERRORUI)
        FOFX_PRESERVEFILEEXTENSIONS = 0x00200000,  // Rename collisions preserve file extns (use with FOF_RENAMEONCOLLISION)
        FOFX_KEEPNEWERFILE = 0x00400000,  // Keep newer file on naming conflicts
        FOFX_NOCOPYHOOKS = 0x00800000,  // Don't use copy hooks
        FOFX_NOMINIMIZEBOX = 0x01000000,  // Don't allow minimizing the progress dialog
        FOFX_MOVEACLSACROSSVOLUMES = 0x02000000,  // Copy security information when performing a cross-volume move operation
        FOFX_DONTDISPLAYSOURCEPATH = 0x04000000,  // Don't display the path of source file in progress dialog
        FOFX_DONTDISPLAYDESTPATH = 0x08000000,  // Don't display the path of destination file in progress dialog
    }

    public static class Constants
    {
        public const uint S_OK = 0x0000;

        public const string ShellItemGuid = "43826D1E-E718-42EE-BC55-A1E261C37BFE";
        public const string FileDialogGuid = "42F85136-DB7E-439C-85F1-E4075D135FC8";
        public const string FileOpenDialogGuid = "DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7";
        public const string FileOperationGuid = "3AD05575-8857-4850-9277-11B85BDB8E09";
    }
}
