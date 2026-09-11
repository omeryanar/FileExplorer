using DevExpress.Xpf.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace FileExplorer.Editors
{
    public class MaterialColorEdit : PopupColorEdit
    {
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            DefaultColor = ColorHelper.ColorFromHex("#FFFFEB3B");
            ChipSize = ChipSize.Large;
            ColumnCount = 19;
            Palettes.Clear();

            List<Color> defaultColors = new List<Color>();
            List<Color> lightColors = new List<Color>();
            List<Color> darkColors = new List<Color>();

            for (int i = 9; i > 0; i--)
            {
                for (int j = i; j < MaterialColors.Count; j += 10)
                {
                    if (i > 5)
                        darkColors.Add(ColorHelper.ColorFromHex(MaterialColors.Keys.ElementAt(j)));
                    else if (i < 5)
                        lightColors.Add(ColorHelper.ColorFromHex(MaterialColors.Keys.ElementAt(j)));
                    else if (i == 5)
                        defaultColors.Add(ColorHelper.ColorFromHex(MaterialColors.Keys.ElementAt(j)));
                }
            }

            Palettes.Add(new CustomPalette("Material Colors", defaultColors));
            Palettes.Add(new CustomPalette("Material Dark Colors", darkColors));
            Palettes.Add(new CustomPalette("Material Light Colors", lightColors));
        }

        protected override string GetColorNameCore(Color color)
        {
            string key = color.ToString();
            if (MaterialColors.ContainsKey(key))
                return MaterialColors[key];

            return key;
        }

        public static readonly Dictionary<string, string> MaterialColors = new()
        {
            {"#FFFFEBEE","Red 50"},
            {"#FFFFCDD2","Red 100"},
            {"#FFEF9A9A","Red 200"},
            {"#FFE57373","Red 300"},
            {"#FFEF5350","Red 400"},
            {"#FFF44336","Red 500"},
            {"#FFE53935","Red 600"},
            {"#FFD32F2F","Red 700"},
            {"#FFC62828","Red 800"},
            {"#FFB71C1C","Red 900"},

            {"#FFFCE4EC","Pink 50"},
            {"#FFF8BBD0","Pink 100"},
            {"#FFF48FB1","Pink 200"},
            {"#FFF06292","Pink 300"},
            {"#FFEC407A","Pink 400"},
            {"#FFE91E63","Pink 500"},
            {"#FFD81B60","Pink 600"},
            {"#FFC2185B","Pink 700"},
            {"#FFAD1457","Pink 800"},
            {"#FF880E4F","Pink 900"},

            {"#FFF3E5F5","Purple 50"},
            {"#FFE1BEE7","Purple 100"},
            {"#FFCE93D8","Purple 200"},
            {"#FFBA68C8","Purple 300"},
            {"#FFAB47BC","Purple 400"},
            {"#FF9C27B0","Purple 500"},
            {"#FF8E24AA","Purple 600"},
            {"#FF7B1FA2","Purple 700"},
            {"#FF6A1B9A","Purple 800"},
            {"#FF4A148C","Purple 900"},

            {"#FFEDE7F6","Deep Purple 50"},
            {"#FFD1C4E9","Deep Purple 100"},
            {"#FFB39DDB","Deep Purple 200"},
            {"#FF9575CD","Deep Purple 300"},
            {"#FF7E57C2","Deep Purple 400"},
            {"#FF673AB7","Deep Purple 500"},
            {"#FF5E35B1","Deep Purple 600"},
            {"#FF512DA8","Deep Purple 700"},
            {"#FF4527A0","Deep Purple 800"},
            {"#FF311B92","Deep Purple 900"},

            {"#FFE8EAF6","Indigo 50"},
            {"#FFC5CAE9","Indigo 100"},
            {"#FF9FA8DA","Indigo 200"},
            {"#FF7986CB","Indigo 300"},
            {"#FF5C6BC0","Indigo 400"},
            {"#FF3F51B5","Indigo 500"},
            {"#FF3949AB","Indigo 600"},
            {"#FF303F9F","Indigo 700"},
            {"#FF283593","Indigo 800"},
            {"#FF1A237E","Indigo 900"},

            {"#FFE3F2FD","Blue 50"},
            {"#FFBBDEFB","Blue 100"},
            {"#FF90CAF9","Blue 200"},
            {"#FF64B5F6","Blue 300"},
            {"#FF42A5F5","Blue 400"},
            {"#FF2196F3","Blue 500"},
            {"#FF1E88E5","Blue 600"},
            {"#FF1976D2","Blue 700"},
            {"#FF1565C0","Blue 800"},
            {"#FF0D47A1","Blue 900"},

            {"#FFE1F5FE","Light Blue 50"},
            {"#FFB3E5FC","Light Blue 100"},
            {"#FF81D4FA","Light Blue 200"},
            {"#FF4FC3F7","Light Blue 300"},
            {"#FF29B6F6","Light Blue 400"},
            {"#FF03A9F4","Light Blue 500"},
            {"#FF039BE5","Light Blue 600"},
            {"#FF0288D1","Light Blue 700"},
            {"#FF0277BD","Light Blue 800"},
            {"#FF01579B","Light Blue 900"},

            {"#FFE0F7FA","Cyan 50"},
            {"#FFB2EBF2","Cyan 100"},
            {"#FF80DEEA","Cyan 200"},
            {"#FF4DD0E1","Cyan 300"},
            {"#FF26C6DA","Cyan 400"},
            {"#FF00BCD4","Cyan 500"},
            {"#FF00ACC1","Cyan 600"},
            {"#FF0097A7","Cyan 700"},
            {"#FF00838F","Cyan 800"},
            {"#FF006064","Cyan 900"},

            {"#FFE0F2F1","Teal 50"},
            {"#FFB2DFDB","Teal 100"},
            {"#FF80CBC4","Teal 200"},
            {"#FF4DB6AC","Teal 300"},
            {"#FF26A69A","Teal 400"},
            {"#FF009688","Teal 500"},
            {"#FF00897B","Teal 600"},
            {"#FF00796B","Teal 700"},
            {"#FF00695C","Teal 800"},
            {"#FF004D40","Teal 900"},

            {"#FFE8F5E9","Green 50"},
            {"#FFC8E6C9","Green 100"},
            {"#FFA5D6A7","Green 200"},
            {"#FF81C784","Green 300"},
            {"#FF66BB6A","Green 400"},
            {"#FF4CAF50","Green 500"},
            {"#FF43A047","Green 600"},
            {"#FF388E3C","Green 700"},
            {"#FF2E7D32","Green 800"},
            {"#FF1B5E20","Green 900"},

            {"#FFF1F8E9","Light Green 50"},
            {"#FFDCEDC8","Light Green 100"},
            {"#FFC5E1A5","Light Green 200"},
            {"#FFAED581","Light Green 300"},
            {"#FF9CCC65","Light Green 400"},
            {"#FF8BC34A","Light Green 500"},
            {"#FF7CB342","Light Green 600"},
            {"#FF689F38","Light Green 700"},
            {"#FF558B2F","Light Green 800"},
            {"#FF33691E","Light Green 900"},

            {"#FFF9FBE7","Lime 50"},
            {"#FFF0F4C3","Lime 100"},
            {"#FFE6EE9C","Lime 200"},
            {"#FFDCE775","Lime 300"},
            {"#FFD4E157","Lime 400"},
            {"#FFCDDC39","Lime 500"},
            {"#FFC0CA33","Lime 600"},
            {"#FFAFB42B","Lime 700"},
            {"#FF9E9D24","Lime 800"},
            {"#FF827717","Lime 900"},

            {"#FFFFFDE7","Yellow 50"},
            {"#FFFFF9C4","Yellow 100"},
            {"#FFFFF59D","Yellow 200"},
            {"#FFFFF176","Yellow 300"},
            {"#FFFFEE58","Yellow 400"},
            {"#FFFFEB3B","Yellow 500"},
            {"#FFFDD835","Yellow 600"},
            {"#FFFBC02D","Yellow 700"},
            {"#FFF9A825","Yellow 800"},
            {"#FFF57F17","Yellow 900"},

            {"#FFFFF8E1","Amber 50"},
            {"#FFFFECB3","Amber 100"},
            {"#FFFFE082","Amber 200"},
            {"#FFFFD54F","Amber 300"},
            {"#FFFFCA28","Amber 400"},
            {"#FFFFC107","Amber 500"},
            {"#FFFFB300","Amber 600"},
            {"#FFFFA000","Amber 700"},
            {"#FFFF8F00","Amber 800"},
            {"#FFFF6F00","Amber 900"},

            {"#FFFFF3E0","Orange 50"},
            {"#FFFFE0B2","Orange 100"},
            {"#FFFFCC80","Orange 200"},
            {"#FFFFB74D","Orange 300"},
            {"#FFFFA726","Orange 400"},
            {"#FFFF9800","Orange 500"},
            {"#FFFB8C00","Orange 600"},
            {"#FFF57C00","Orange 700"},
            {"#FFEF6C00","Orange 800"},
            {"#FFE65100","Orange 900"},

            {"#FFFBE9E7","Deep Orange 50"},
            {"#FFFFCCBC","Deep Orange 100"},
            {"#FFFFAB91","Deep Orange 200"},
            {"#FFFF8A65","Deep Orange 300"},
            {"#FFFF7043","Deep Orange 400"},
            {"#FFFF5722","Deep Orange 500"},
            {"#FFF4511E","Deep Orange 600"},
            {"#FFE64A19","Deep Orange 700"},
            {"#FFD84315","Deep Orange 800"},
            {"#FFBF360C","Deep Orange 900"},

            {"#FFEFEBE9","Brown 50"},
            {"#FFD7CCC8","Brown 100"},
            {"#FFBCAAA4","Brown 200"},
            {"#FFA1887F","Brown 300"},
            {"#FF8D6E63","Brown 400"},
            {"#FF795548","Brown 500"},
            {"#FF6D4C41","Brown 600"},
            {"#FF5D4037","Brown 700"},
            {"#FF4E342E","Brown 800"},
            {"#FF3E2723","Brown 900"},

            {"#FFFAFAFA","Grey 50"},
            {"#FFF5F5F5","Grey 100"},
            {"#FFEEEEEE","Grey 200"},
            {"#FFE0E0E0","Grey 300"},
            {"#FFBDBDBD","Grey 400"},
            {"#FF9E9E9E","Grey 500"},
            {"#FF757575","Grey 600"},
            {"#FF616161","Grey 700"},
            {"#FF424242","Grey 800"},
            {"#FF212121","Grey 900"},

            {"#FFECEFF1","Blue Grey 50"},
            {"#FFCFD8DC","Blue Grey 100"},
            {"#FFB0BEC5","Blue Grey 200"},
            {"#FF90A4AE","Blue Grey 300"},
            {"#FF78909C","Blue Grey 400"},
            {"#FF607D8B","Blue Grey 500"},
            {"#FF546E7A","Blue Grey 600"},
            {"#FF455A64","Blue Grey 700"},
            {"#FF37474F","Blue Grey 800"},
            {"#FF263238","Blue Grey 900"}
        };
    }
}
