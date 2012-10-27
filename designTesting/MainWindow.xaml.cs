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

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int count = 0;
        public MainWindow()
        {
            InitializeComponent();

            Reader.Document = new FlowDocument();
            KeepToCurrentFlowDocument bottom = new KeepToCurrentFlowDocument(Reader);
        }

        private void test(object sender, RoutedEventArgs e)
        {
            var fd = Reader.Document as FlowDocument;
            var para = new Paragraph();
            para.Inlines.Add(new Run(count++.ToString()));
            fd.Blocks.Add(para);
        }
    }

    /// <summary>
    /// Keeps the user on the most recent page if things change and if they were on the latest page
    /// </summary>
    public class KeepToCurrentFlowDocument : IDisposable
    {
        #region fields
        bool _couldChangeBefore;
        bool _canChangeNow;
        FlowDocumentPageViewer _reader;
        #endregion

        #region constructor
        public KeepToCurrentFlowDocument(FlowDocumentPageViewer reader)
        {
            _reader = reader;

            _couldChangeBefore = _reader.CanGoToNextPage;

            _reader.Document.DocumentPaginator.PagesChanged += KeepToBottom;
        }
        #endregion

        #region methods
        public void KeepToBottom(object sender, PagesChangedEventArgs e)
        {
            _canChangeNow = _reader.CanGoToNextPage;
            if (_canChangeNow && !_couldChangeBefore)
                _reader.NextPage();

            _couldChangeBefore = _canChangeNow;
        }
        #endregion

        #region IDispose
        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool managed)
        {
            if (managed)
            {
                _reader.Document.DocumentPaginator.PagesChanged -= KeepToBottom;
                _reader = null;
            }
        }
        #endregion
    }
}
