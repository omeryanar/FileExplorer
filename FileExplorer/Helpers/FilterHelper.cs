using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.Xpf.Core.FilteringUI;
using FileExplorer.Persistence;

namespace FileExplorer.Helpers
{
	public static class FilterHelper
	{
		public static PredefinedFilterCollection FileExtensionFilters { get; private set; }

		public static PredefinedFilterCollection FileSizeFilters { get; private set; }

		public static PredefinedFilterCollection FileTagFilters { get; private set; }

		const string ImageExtensions = "'.apng','.art','.avci','.avcs','.avif','.avifs','.bmp','.cmx','.cod','.dib','.emf','.gif','.heic','.heics','.heif','.heifs','.ico','.icon','.ief','.jfif','.jpe','.jpeg','.jpg','.jxr','.mac','.pbm','.pct','.pgm','.pic','.pict','.png','.pnm','.pnt','.pntg','.pnz','.ppm','.qti','.qtif','.ras','.rf','.rgb','.tif','.tiff','.wbmp','.wdp','.webp','.xbm','.xpm','.xwd'";

		const string VideoExtensions = "'.3g2','.3gp','.3gp2','.3gpp','.asf','.asr','.asx','.avi','.axv','.dif','.divx','.dv','.f4v','.flv','.ivf','.lsf','.lsx','.m1v','.m2t','.m2ts','.m2v','.m4v','.mk3d','.mkv','.mod','.mov','.movie','.mp2','.mp2v','.mp4','.mp4v','.mpa','.mpe','.mpeg','.mpg','.mpv2','.mqv','.mts','.nsc','.ogv','.qt','.ts','.tts','.vbk','.webm','.wm','.wmp','.wmv','.wmx','.wvx'";

		const string AudioExtensions = "'.aa','.aac','.aax','.ac3','.adt','.adts','.aif','.aifc','.aiff','.au','.axa','.caf','.cdda','.flac','.gsm','.m4a','.m4b','.m4p','.m4r','.mid','.midi','.mka','.mp3','.oga','.ogg','.opus','.pls','.ra','.ram','.rmi','.rpm','.sd2','.smd','.smx','.smz','.snd','.spx','.wav','.wave','.wax','.wma'";

		const string ArchiveExtensions = "'.7z','.arj','.bz2','.bzip2','.cab','.chm','.chw','.cpio','.deb','.gz','.gzip','.hxs','.iso','.lha','.lzh','.rar','.rpm','.swm','.tar','.taz','.tbz','.tbz2','.tgz','.wim','.z','.zip'";

		const string DocumentExtensions = "'.doc','.docm','.docx','.dot','.dotm','.dotx','.pot','.potm','.potx','.ppa','.ppam','.pps','.ppsm','.ppsx','.ppt','.pptm','.pptx','.xlm','.xls','.xlsb','.xlsm','.xlsx','.xlt','.xltm','.xltx','.pdf','.rtf','.csv','.xps'";

		const string ExecutableExtensions = "'.bat','.bin','.cmd','.com','.dll','.exe','.jar','.msi','.msp','.mst','.scr'";

		static FilterHelper()
		{
			FileExtensionFilters =
				[
					new PredefinedFilter { Name = "Image", Filter = CriteriaOperator.Parse($"?p IN ({ImageExtensions})") },
					new PredefinedFilter { Name = "Video", Filter = CriteriaOperator.Parse($"?p IN ({VideoExtensions})") },
					new PredefinedFilter { Name = "Audio", Filter = CriteriaOperator.Parse($"?p IN ({AudioExtensions})") },
					new PredefinedFilter { Name = "Archive", Filter = CriteriaOperator.Parse($"?p IN ({ArchiveExtensions})") },
					new PredefinedFilter { Name = "Document", Filter = CriteriaOperator.Parse($"?p IN ({DocumentExtensions})") },
					new PredefinedFilter { Name = "Executable", Filter = CriteriaOperator.Parse($"?p IN ({ExecutableExtensions})") }
				];

			FileSizeFilters =
				[
					new PredefinedFilter { Name = "Empty (0 KB)", Filter = CriteriaOperator.Parse("?p = 0") },
					new PredefinedFilter { Name = "Tiny (0 - 16 KB)", Filter = CriteriaOperator.Parse("?p > 0 AND ?p < 16384") },
					new PredefinedFilter { Name = "Small (16 KB - 1 MB)", Filter = CriteriaOperator.Parse("?p >= 16384 AND ?p < 1048576") },
					new PredefinedFilter { Name = "Medium (1 - 128 MB)", Filter = CriteriaOperator.Parse("?p >= 1048576 AND ?p < 134217728") },
					new PredefinedFilter { Name = "Large (128 MB - 1 GB)", Filter = CriteriaOperator.Parse("?p >= 134217728 AND ?p < 1073741824") },
					new PredefinedFilter { Name = "Huge (1 - 4 GB)", Filter = CriteriaOperator.Parse("?p >= 1073741824 AND ?p < 4294967296") },
					new PredefinedFilter { Name = "Gigantic (> 4 GB)", Filter = CriteriaOperator.Parse("?p >= 4294967296") }
				];

			FileTagFilters = new PredefinedFilterCollection();
			foreach (TagFolder tagFolder in App.Repository.TagFolders.OrderBy(x => x.Name))
				FileTagFilters.Add(new PredefinedFilter { Name = tagFolder.Name, Filter = CriteriaOperator.Parse($"Contains(?p, '{tagFolder.Name}')") });
		}
	}
}
