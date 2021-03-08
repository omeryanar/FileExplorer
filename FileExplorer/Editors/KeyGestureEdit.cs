using System;
using System.Collections.Generic;
using System.Windows.Input;
using DevExpress.Xpf.Editors;

namespace FileExplorer.Editors
{
    public class KeyGestureEdit : ComboBoxEdit
    {
        public KeyGestureEdit()
        {
            SelectedIndex = 0;
            IsTextEditable = false;
            ItemsSource = GetShortcutKeyList();
        }

        private List<string> GetShortcutKeyList()
        {
            List<string> shortcuts = new List<string> { String.Empty };

            for (int i = 1; i <= 24; i++)
                shortcuts.Add(String.Format("F{0}", i));

            for (int i = 0; i < 10; i++)
                shortcuts.Add(String.Format("NumPad{0}", i));

            foreach (ModifierKeys modifier in Enum.GetValues(typeof(ModifierKeys)))
            {
                foreach (Key key in Enum.GetValues(typeof(Key)))
                {
                    if ((modifier > ModifierKeys.None && modifier < ModifierKeys.Shift) &&
                        (key >= Key.A && key <= Key.Z))
                        shortcuts.Add(String.Format("{0}+{1}", modifier, key));
                }
            }

            List<Tuple<ModifierKeys, ModifierKeys>> doubleModifiers = new List<Tuple<ModifierKeys, ModifierKeys>>
            {
                new Tuple<ModifierKeys, ModifierKeys>(ModifierKeys.Control, ModifierKeys.Alt),
                new Tuple<ModifierKeys, ModifierKeys>(ModifierKeys.Control, ModifierKeys.Shift),
                new Tuple<ModifierKeys, ModifierKeys>(ModifierKeys.Alt, ModifierKeys.Shift)
            };
            foreach (Tuple<ModifierKeys, ModifierKeys> modifiers in doubleModifiers)
            {
                foreach (Key key in Enum.GetValues(typeof(Key)))
                {
                    if (key >= Key.A && key <= Key.Z)
                        shortcuts.Add(String.Format("{0}+{1}+{2}", modifiers.Item1, modifiers.Item2, key));
                }
            }

            return shortcuts;
        }
    }
}
