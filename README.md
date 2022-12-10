# File Explorer

File Explorer is a windows file manager application that has an interface similar to Windows Explorer.
However, it browses and searches folders with many files faster than windows explorer.
It also has many features that are not available in windows explorer.

File Explorer is portable (requires .NET Framework 4.5.2), extract the ZIP file below and start using it.
https://github.com/omeryanar/FileExplorer/releases/latest/download/FileExplorer.zip

## Features

### Tabbed Browsing

When you click a folder with the middle mouse button or the CTRL key and the left mouse button, the folder opens in a new tab.
Tabbed browsing lets you keep multiple folders open and switch quickly between them.
You can detach the tabs in to a new window by dragging a tab and move it up or down from the tabs bar. 

![Tabbed Browsing](https://github.com/omeryanar/Resources/blob/master/FileExplorer/TabbedBrowsing.png?raw=true)

### Batch Rename

You can rename multiple files by using excel-style formulas.

![Batch Rename](https://github.com/omeryanar/Resources/blob/master/FileExplorer/BatchRename.png?raw=true)

### Custom Context Menu

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

You can create calculated columns by using excel-style formulas.

![Custom Columns](https://github.com/omeryanar/Resources/blob/master/FileExplorer/CustomColumns.png?raw=true)

### Filtering

You can filter file and folders by using excel-style drop-down filter.

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

You can print folder contents with all the customizations you make or export them to different file formats (such as Excel or PDF)

![Printing](https://github.com/omeryanar/Resources/blob/master/FileExplorer/Printing.png?raw=true)

### Standard&Touch Themes

File Explorer comes with 6 standard and 4 touch themes, including Visual Studio Light & Dark Themes.

![Themes](https://github.com/omeryanar/Resources/blob/master/FileExplorer/Themes.png?raw=true)

![VS Dark Theme](https://github.com/omeryanar/Resources/blob/master/FileExplorer/VSDarkTheme.png?raw=true)
