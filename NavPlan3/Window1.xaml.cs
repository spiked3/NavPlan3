using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NavPlan3
{
    public partial class Window1 : Window
    {
        public Point FromPoint
        {
            get { return (Point)GetValue(FromPointProperty); }
            set { SetValue(FromPointProperty, value); }
        }
        public static readonly DependencyProperty FromPointProperty =
            DependencyProperty.Register("FromPoint", typeof(Point), typeof(Window1), new PropertyMetadata(new Point(0,0)));

        public Point ToPoint
        {
            get { return (Point)GetValue(ToPointProperty); }
            set { SetValue(ToPointProperty, value); }
        }
        public static readonly DependencyProperty ToPointProperty =
            DependencyProperty.Register("ToPoint", typeof(Point), typeof(Window1), new PropertyMetadata(new Point(0,0)));

        public Double Heading
        {
            get { return (Double)GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }
        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register("Heading", typeof(double), typeof(Window1), new PropertyMetadata(0.0));

        public Window1()
        {
            InitializeComponent();
        }

        private void Go_Click(object sender, RoutedEventArgs e)
        {
            Heading = (Math.Atan2(ToPoint.X - FromPoint.X, (ToPoint.Y - FromPoint.Y)) * 180 / Math.PI);
            FromPoint = ToPoint;
            ToPoint = new Point(0, 0);
        }
    }
}
