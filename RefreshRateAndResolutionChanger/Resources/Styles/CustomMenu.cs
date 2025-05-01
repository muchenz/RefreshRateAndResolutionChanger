using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RefreshRateAndResolutionChanger.Resources.Styles
{
    public class CustomMenuItem : MenuItem
    {
        public static readonly DependencyProperty SubmenuItemHeightProperty =
            DependencyProperty.Register("SubmenuItemHeight", typeof(int), typeof(CustomMenuItem),
                new PropertyMetadata(0));
        //new FrameworkPropertyMetadata(24, FrameworkPropertyMetadataOptions.Inherits));

        // for acces XALM
        public int SubmenuItemHeight
        {
            get { return (int)GetValue(SubmenuItemHeightProperty); }
            set { SetValue(SubmenuItemHeightProperty, value); }
        }
    }


    public class CustomMenu : Menu
    {
        public static readonly DependencyProperty SubmenuItemHeightProperty =
            DependencyProperty.Register("SubmenuItemHeight", typeof(int), typeof(CustomMenu), new PropertyMetadata(0));

        // for acces XALM

        public int SubmenuItemHeight
        {
            get { return (int)GetValue(SubmenuItemHeightProperty); }
            set { SetValue(SubmenuItemHeightProperty, value); }
        }

    }
}
