using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RefreshRateAndResolutionChanger.Resources.Styles.Test
{
        

    public static class MyMarginCustom
    {
        public static readonly DependencyProperty MyMarginProperty =
            DependencyProperty.RegisterAttached("MyMargin", typeof(Thickness), typeof(MyMarginCustom),
                new FrameworkPropertyMetadata(new Thickness(0, 16, 0, 16), FrameworkPropertyMetadataOptions.Inherits));

        // for acces XALM

        public static Thickness GetMyMargin(DependencyObject obj) => (Thickness)obj.GetValue(MyMarginProperty);

        public static void SetMyMargin(DependencyObject obj, Thickness value) => obj.SetValue(MyMarginProperty, value);

    }

    public class MyTestCustomControl2 : Control
    {
        static  MyTestCustomControl2()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MyTestCustomControl2),
           new FrameworkPropertyMetadata(typeof(MyTestCustomControl2)));
        }
        public static readonly DependencyProperty MyMargin2Property =
            DependencyProperty.Register("MyMargin2", typeof(Thickness), typeof(MyTestCustomControl2),
                new FrameworkPropertyMetadata(new Thickness(16, 16, 16, 16), FrameworkPropertyMetadataOptions.Inherits));

        // for acces XALM


        public Thickness MyMargin2
        {
            get { return (Thickness)GetValue(MyMargin2Property); }
            set { SetValue(MyMargin2Property, value); }
        }

    }
}