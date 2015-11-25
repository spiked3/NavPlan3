using Newtonsoft.Json;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using uPLibrary.Networking.M2Mqtt;

using Spiked3;

namespace NavPlan3
{
    // for now Ive given up on a comprehensive planning app, this is a quick and dirty for local points only
    public partial class MainWindow : Window
    {
        public NavPointCollection NavPoints
        {
            get { return (NavPointCollection)GetValue(NavPointsProperty); }
            set { SetValue(NavPointsProperty, value); }
        }
        public static readonly DependencyProperty NavPointsProperty =
            DependencyProperty.Register("NavPoints", typeof(NavPointCollection), typeof(MainWindow));

        public string NavPointText
        {
            get { return (string)GetValue(NavPointTextProperty); }
            set { SetValue(NavPointTextProperty, value); }
        }
        public static readonly DependencyProperty NavPointTextProperty =
            DependencyProperty.Register("NavPointText", typeof(string), typeof(MainWindow), new PropertyMetadata("to be replaced by NavPoints text"));

        public string StatusText
        {
            get { return (string)GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }
        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register("StatusText", typeof(string), typeof(MainWindow), new PropertyMetadata("StatusText"));

        public MainWindow()
        {
            InitializeComponent();
            NavPoints = JsonConvert.DeserializeObject<NavPointCollection>(Properties.Settings.Default.NavPoints);
            if (NavPoints == null)
                NavPoints = new NavPointCollection();
            NavPoints.CollectionChanged += NavPoints_Changed;
            NavPoints_Changed(this, null);
        }

        void Recalc()
        {
            // point 0 is Origin
            Utm origin = (Utm)NavPoints[0].Wgs;
            
            double initialHeading = 0;
            StringBuilder b = new StringBuilder();
            b.Append($"{{\"ResetHdg\":{initialHeading},\n\"WayPoints\":[\n");

            bool firstTime = true;
            
            foreach (NavPoint w in NavPoints)
            {
                if (w == NavPoints[0])
                    continue;

                if (!firstTime)
                    b.Append(",\n");
                else
                    firstTime = false;

                Utm thisUtm = (Utm)w.Wgs;
                double x = thisUtm.Easting - origin.Easting;
                double y = thisUtm.Northing - origin.Northing;

                //b.Append($"[{w.XY.X}, {w.XY.Y}, {(w.isAction ? 1 : 0)}]");    // Turn/Move version
                b.Append($"[{x:F3}, {y:F3}, {(w.isAction ? 1 : 0)}]");    // Turn/Move version
            }
            NavPointText = b.Append($"\n]}}").ToString();
        }

        private void NavPoints_Changed(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Recalc();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            NavPoints.Add(new NavPoint { });
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Recalc();
            Clipboard.SetText(NavPointText);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.NavPoints = JsonConvert.SerializeObject(NavPoints);
            Properties.Settings.Default.Save();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (listBox1.SelectedItem != null)
                NavPoints.Remove((NavPoint)listBox1.SelectedItem);
            else
                StatusText = "No item selected";
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                var i = NavPoints.IndexOf((NavPoint)listBox1.SelectedItem);
                if (i > 0)
                    NavPoints.Move(i, i - 1);
            }
            else
                StatusText = "No item selected";
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                var i = NavPoints.IndexOf((NavPoint)listBox1.SelectedItem);
                if (i < NavPoints.Count - 1)
                    NavPoints.Move(i, i + 1);
            }
            else
                StatusText = "No item selected";
        }

        private void Publish_Click(object sender, RoutedEventArgs e)
        {
            MqttClient Mq;
            string broker = "192.168.42.1";
            //string broker = "127.0.0.1";

            Refresh_Click(this, null);
            Mq = new MqttClient(broker);
            Mq.Connect("pNavPlan3");
            System.Diagnostics.Trace.WriteLine($"Connected to MQTT @ {broker}", "1");
            var jsn = NavPointText;
            Mq.Publish("Navplan/WayPoints", Encoding.ASCII.GetBytes(jsn));
            System.Threading.Thread.Sleep(200);

            Mq.Disconnect();
            StatusText = "NavPoints published";
        }
    }
}
