# File Explorer

File Explorer is a Windows file manager application that has an interface similar to Windows Explorer.
However, it browses and searches folders with many files faster than Windows Explorer.
It also has many features that are not available in Windows Explorer.

File Explorer is portable (requires .NET Framework 4.8), extract the ZIP file below and start using it.
https://github.com/omeryanar/FileExplorer/releases/latest/download/FileExplorer.zip

## Features

### Tabbed Browsing

When you click a folder with the middle mouse button or the CTRL key and the left mouse button, the folder opens in a new tab.
Tabbed browsing lets you keep multiple folders open and switch quickly between them.
You can detach the tabs in to a new window by dragging a tab and move it up or down from the tabs bar. 

![Tabbed Browsing](https://github.com/omeryanar/Resources/blob/master/FileExplorer/TabbedBrowsing.png?raw=true)

### Combined Navigation & Content View

File Explorer has a different content view than Windows Explorer: a combination of the navigation pane and the detailed view.
You can navigate through folders by selecting them in the navigation pane or double-clicking them in the main pane, which also sets the current working directory.
In addition, you can also expand the subfolders of the current working directory in the main pane and view both folders and files in a tree-like fashion.

![Combined View](https://github.com/omeryanar/Resources/blob/master/FileExplorer/CombinedView.png?raw=true)

### Batch Rename

You can rename multiple files by using Excel-style formulas.

![Batch Rename](https://github.com/omeryanar/Resources/blob/master/FileExplorer/BatchRename.png?raw=true)

### Standard Windows and Custom Context Menu

File Explorer normally shows its own context menu when you right-click on an item. If you want to display standard Windows context menu, you can right-click on files or folders while pressing CTRL key.

![Windows Context Menu](https://github.com/omeryanar/Resources/blob/master/FileExplorer/WindowsContextMenu.png?raw=true)

You can create your own custom context menu items and specify what will happen when you click them.
Custom menu items will appear when you right click files and/or folders.
You can filter which menu items will appear for which item types.

* Extension Filter: Menu will appear in files with the specified extensions
* Selection Filter: Menu will appear if selection is single or multiple.
* Item Type Filter: Menu will appear if selection is file or folder.

![Custom Context Menu](https://github.com/omeryanar/Resources/blob/master/FileExplorer/CustomMenuItems.png?raw=true)

You can use expressions while creating custom context menu items.
Assume that you want to create a context menu item that creates thumbnail images when clicked by using a movie thumbnailer software, such as MTN.exe (http://moviethumbnail.sourceforge.net/)

Then you can  use the following expression: '-P -i -o .jpg -c 3 -r 4 "' + [Path] + '"'

It will create a thumbnail .jpg image of 4 rows and 3 columns with the same location and name of the video file.
You can tweak the arguments for your needs.

![Custom Context Menu Expression](https://github.com/omeryanar/Resources/blob/master/FileExplorer/CustomMenuExpression.png?raw=true)

### Custom Columns

You can create calculated columns by using Excel-style formulas.

![Custom Columns](https://github.com/omeryanar/Resources/blob/master/FileExplorer/CustomColumns.png?raw=true)

### Preview Pane

The Preview Pane at the rightmost of the application supports preview for the following file types:

* Image Files (.png, .gif, .ico, .bmp, .jpg, .jpeg, .tif, .tiff, .svg)
* Word Files (.rtf, .doc, .docx, .docm, .dot, .dotm, .dotx, .odt, .epub, .htm, .html, .mht)
* Excel Files (.xls, .xlt, .xlsx, .xltx, .xlsb, .xlsm, .xltm, .csv)
* Text Files (.txt, .xml, .cs, .sql ...)
* PDF Files (.pdf)

Note that some extensions (such as Word and Excel) may take a long time to load. You can disable them in Extension Manager.
You can also set priority for extensions that can preview the same file types.
For example, both Text and RichText extesions can preview HTML files. If you set one of them as preferred, HTML files will be previewed using this extension.

![Preview Pane](https://github.com/omeryanar/Resources/blob/master/FileExplorer/ExtensionManager.png?raw=true)

### Filtering

You can filter file and folders by using Excel-style drop-down filter.

![Excel Filtering](https://github.com/omeryanar/Resources/blob/master/FileExplorer/ExcelFiltering.png?raw=true)

The Filter Editor allows you to build complex filters. You can add filter conditions and use logical operators to group filters.

![Filter Editor](https://github.com/omeryanar/Resources/blob/master/FileExplorer/FilterEditor.png?raw=true)

### Conditional Formatting

File Explorer provides a conditional formatting feature, which allows you to change individual cells or rows' appearance based on specific conditions.
This feature helps to highlight critical information, identify trends and compare data.

![Conditional Formatting](https://github.com/omeryanar/Resources/blob/master/FileExplorer/ConditionalFormatting.png?raw=true)

### Save&Load Layout

You can save the layout of the current folder (and all its subfolders if you want) with all your customizations: sorting, grouping, filtering, custom columns etc.
The next time you browse this folder, the saved layout will be loaded automatically.

![Save&Load Layout](https://github.com/omeryanar/Resources/blob/master/FileExplorer/SaveLoadLayout.png?raw=true)

### Tree View Sorting

One of the limitations of Windows Explorer is that it only allows sorting of folders in list view.
File Explorer also allows you to sort the subfolders of the current folder in the tree view.

![Tree View Sorting](https://github.com/omeryanar/Resources/blob/master/FileExplorer/TreeViewSort.png?raw=true)

### Drag&Drop Support

File Explorer fully supports drag-and-drop operations with other applications, including Windows Explorer.

![Drag&Drop Support](https://github.com/omeryanar/Resources/blob/master/FileExplorer/DragDropSupport.png?raw=true)

### Row Number

You can show or hide row numbers in the file explorer.

![Row Number](https://github.com/omeryanar/Resources/blob/master/FileExplorer/RowNumber.png?raw=true)

### Printing and Exporting

You can print folder contents with all the customizations you make or export them to different file formats (such as Excel or PDF).

![Printing](https://github.com/omeryanar/Resources/blob/master/FileExplorer/Printing.png?raw=true)

### Themes

File Explorer comes with 12 themes (light & dark) , including Windows 10, Windows 11, Visual Studio and Office 2019.

![VS Dark Theme](https://github.com/omeryanar/Resources/blob/master/FileExplorer/VSDarkTheme.png?raw=true)
