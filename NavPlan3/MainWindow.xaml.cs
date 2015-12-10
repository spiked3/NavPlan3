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
using Microsoft.Win32;
using System.IO;

namespace NavPlan3
{
    // for now Ive given up on a comprehensive planning app, this is a quick and dirty
    public partial class MainWindow : Window
    {

        string Broker = "192.168.42.1";
        //string Broker = "127.0.0.1";

        [JsonIgnore]
        static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };

        public bool isDirty
        {
            get { return (bool)GetValue(isDirtyProperty); }
            set { SetValue(isDirtyProperty, value); }
        }
        public static readonly DependencyProperty isDirtyProperty =
            DependencyProperty.Register("isDirty", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

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

        public string LastFileName
        {
            get { return (string)GetValue(LastFileNameProperty); }
            set { SetValue(LastFileNameProperty, value); }
        }

        public static readonly DependencyProperty LastFileNameProperty =
            DependencyProperty.Register("LastFileName", typeof(string), typeof(MainWindow));

        public MainWindow()
        {
            InitializeComponent();

            LoadFile(Properties.Settings.Default.LastFileName);
            NavPoints.CollectionChanged += NavPointsChanged;
            NavPointsChanged(this, null);
        }

        private void LoadFile(string f)
        {
            try
            {
                NavPoints = JsonConvert.DeserializeObject<NavPointCollection>(File.ReadAllText(f), settings);
                foreach (NavPoint n in NavPoints)
                    n.PropertyChanged += NavPointChanged;
                LastFileName = f;
                StatusText = $"Loaded collection {LastFileName}";
                isDirty = false;
            }
            catch (Exception)
            {
                System.Diagnostics.Debugger.Break();
                StatusText = "Creating new collection";
                New(this, null);
                LastFileName = null;
            }
        }

        void Load(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "JSON Plan Files|*.json|All Files|*.*", FileName = LastFileName };
            if (d.ShowDialog() ?? false)
            {
                if (File.Exists(d.FileName))
                    LoadFile(d.FileName);
            }
        }

        private void  SaveAs(object sender, RoutedEventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog { Filter = "JSON Files|*.json|All Files|*.*", DefaultExt = "json" };
            if (d.ShowDialog() ?? false)
            {
                LastFileName = d.FileName;
                File.WriteAllText(LastFileName, JsonConvert.SerializeObject(NavPoints, settings));
                StatusText = $"Saved collection {LastFileName}";
                isDirty = false;
            }
        }

        void New(object sender, RoutedEventArgs e)
        {
            NavPoints = new NavPointCollection { InitialHeading = 0 };
            NavPoints.CollectionChanged += NavPointsChanged;
            // todo this should trigger text update, but does not
            NavPoints.Add(new NavPoint { });    // always an origin point
            isDirty = true;
        }

        private void NavPointsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // point 0 is Origin

            if (NavPoints.Count > 2)
            {
#if WORLD
                Utm origin = (Utm)NavPoints[0].Wgs;
                Utm waypoint1 = (Utm)NavPoints[1].Wgs;
                NavPoints.InitialHeading = (float)Math.Atan2(waypoint1.Easting - origin.Easting, waypoint1.Northing - origin.Northing);
                NavPoints.InitialHeading *= (float)(180 / Math.PI);

                StringBuilder b = new StringBuilder();
                b.Append($"{{\"ResetHdg\":{NavPoints.InitialHeading},\n\"WayPoints\":[\n");

                bool firstTime = true;

                foreach (NavPoint w in NavPoints)
                {
                    if (w == NavPoints[0])
                        continue;

                    if (w.Wgs != null)
                    {
                        if (!firstTime)
                            b.Append(",\n");
                        else
                            firstTime = false;

                        Utm thisUtm = (Utm)w.Wgs;
                        double x = thisUtm.Easting - origin.Easting;
                        double y = thisUtm.Northing - origin.Northing;

                        b.Append($"[{x:F3}, {y:F3}, {(w.isAction ? 1 : 0)}]");    // Turn/Move version
                    }
                }
                NavPointText = b.Append($"\n]}}").ToString();
#else
                NavPoints.InitialHeading = (float)Math.Atan2(NavPoints[1].XY.X, NavPoints[1].XY.Y);
                NavPoints.InitialHeading *= (float)(180 / Math.PI);

                StringBuilder b = new StringBuilder();
                b.Append($"{{\"ResetHdg\":{NavPoints.InitialHeading},\n\"WayPoints\":[\n");

                bool firstTime = true;

                foreach (NavPoint w in NavPoints)
                {
                    if (w == NavPoints[0])
                        continue;

                    if (!firstTime)
                        b.Append(",\n");
                    else
                        firstTime = false;

                    double x = w.XY.X - NavPoints[0].XY.X;
                    double y = w.XY.Y - NavPoints[0].XY.Y;

                    b.Append($"[{x:F3}, {y:F3}, {(w.isAction ? 1 : 0)}]");    // Turn/Move version
                }
                NavPointText = b.Append($"\n]}}").ToString();
#endif
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            NavPoint n = new NavPoint { };
            n.PropertyChanged += NavPointChanged;
            NavPoints.Add(n);
        }

        private void NavPointChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NavPointsChanged(sender, null);
            isDirty = true;
        }

        private void WpDelete(object sender, RoutedEventArgs e)
        {
            NavPoint p = (NavPoint)((Button)sender).DataContext;
            NavPoints.Remove(p);
            isDirty = true;
        }

        private void Publish_Click(object sender, RoutedEventArgs e)
        {
            MqttClient Mq;

            NavPointsChanged(this, null);   // todo overwrites user supplied initial heading
            Clipboard.SetText(NavPointText);
            try
            {
                Mq = new MqttClient(Broker);
                Mq.Connect("pNavPlan3");
                Mq.Publish("Navplan/WayPoints", Encoding.ASCII.GetBytes(NavPointText));
                System.Threading.Thread.Sleep(200);
                Mq.Disconnect();
                StatusText = $"Connected to MQTT @ { Broker},  NavPoints published";
            }
            catch (Exception ex)
            {
                StatusText = $"Mqtt Exception: {ex.Message}";
                System.Diagnostics.Debugger.Break();
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                var i = NavPoints.IndexOf((NavPoint)listBox1.SelectedItem);
                if (i > 0)
                    NavPoints.Move(i, i - 1);
                else
                    StatusText = "No item selected";
            }
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isDirty)
            {
                var x = MessageBox.Show("Points have changed, save?", "Save", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (x == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                if (x == MessageBoxResult.Yes)
                    SaveAs(sender, null);   // todo does not handle cancel during save :(
            }

            Properties.Settings.Default.LastFileName = LastFileName;
            Properties.Settings.Default.Save();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
