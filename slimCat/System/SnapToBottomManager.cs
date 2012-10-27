using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace System
{
    public class SnapToBottomManager
    {
        private ScrollViewer _toManage;
        private DependencyObject _messages;

        public SnapToBottomManager(DependencyObject messages)
        {
            _messages = messages;
            _toManage = (ScrollViewer)FindChild(_messages);
        }

        public void AutoDownScroll(bool keepAtCurrent, bool forceDown = false)
        {
            if (_toManage == null)
                _toManage = (ScrollViewer)FindChild(_messages);

            if (_toManage != null)
            {
                if (forceDown)
                    _toManage.ScrollToBottom();

                else if (ShouldAutoScroll())
                    _toManage.ScrollToBottom();

                else if (keepAtCurrent && _toManage.VerticalOffset >= 1)
                    _toManage.ScrollToVerticalOffset(_toManage.VerticalOffset - 1);
            }
        }

        private bool ShouldAutoScroll()
        {
            return (_toManage.ScrollableHeight - _toManage.VerticalOffset <= 4);
        }

        private static ScrollViewer FindChild(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {

                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is ScrollViewer)
                    return child as ScrollViewer;

                DependencyObject grandchild = FindChild(child);

                if (grandchild != null)
                    return grandchild as ScrollViewer;
            }

            return default(ScrollViewer);
        }
    }
}
