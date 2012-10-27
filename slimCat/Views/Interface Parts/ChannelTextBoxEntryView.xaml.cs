using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ViewModels;

namespace Views
{
    /// <summary>
    /// Interaction logic for ChannelTextBoxEntryView.xaml
    /// </summary>
    public partial class ChannelTextBoxEntryView : UserControl
    {
        public ChannelTextBoxEntryView()
        {
            InitializeComponent();

            Entry.FocusableChanged += (s, e) =>
                {
                    if ((bool)e.NewValue == true)
                        Entry.Focus();
                };
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            #region Text-only (non-command) shortcuts
            if (e.Key == Key.Return && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                var caretIndex = Entry.CaretIndex;
                Entry.Text = Entry.Text.Insert(caretIndex, "\r");
                Entry.CaretIndex = caretIndex + 1;
            }
            #endregion

            #region BBCode shortcuts
            else if ((e.Key == Key.B || e.Key == Key.I || e.Key == Key.S || e.Key == Key.U || e.Key == Key.L)
                        && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (!String.IsNullOrWhiteSpace(Entry.SelectedText))
                {
                    var key = e.Key.ToString().ToLower();
                    var selected = Entry.SelectedText;

                    if (key != "l")
                        Entry.SelectedText = String.Format("[{0}]{1}[/{0}]", key, selected);
                    else
                    {
                        var toEnter = "[url=" + selected + "]";
                        var caretIndex = Entry.CaretIndex;

                        Entry.SelectedText = toEnter + "[/url]";
                        Entry.CaretIndex = caretIndex + toEnter.Length;
                    }
                }

                else
                {
                    var key = e.Key.ToString().ToLower();
                    var caretIndex = Entry.CaretIndex;
                    if (key == "l")
                        key = "url";

                    Entry.Text = Entry.Text.Insert(caretIndex, String.Format("[{0}][/{0}]", key));
                    Entry.CaretIndex = caretIndex + (key != "url" ? 3 : 5);
                }
            }
            #endregion
        }
    }
}
