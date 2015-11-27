using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Spiked3;

namespace NavPlan3
{
    public class NavPoint : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion

#if WORLD

        public Wgs84 Wgs { get { return _Wgs; } set { _Wgs = value; OnPropertyChanged(); } }
        Wgs84 _Wgs;
#else
        public Point XY { get { return _XY; } set { _XY = value; OnPropertyChanged(); } } 
        Point _XY = new Point(0,0); 
#endif

        public bool isAction { get { return _isAction; } set { _isAction = value; OnPropertyChanged(); } }
        bool _isAction = false;
    }

    public class NavPointCollection : ObservableCollection<NavPoint>
    {
        // todo this is not getting json serialized?
        public float InitialHeading { get { return _InitialHeading; } set { _InitialHeading = value; OnPropertyChanged(new PropertyChangedEventArgs("InitialHeading")); } }
        float _InitialHeading = 0;

        protected override void InsertItem(int index, NavPoint item)
        {
            item.PropertyChanged += NavPointChanged;
            base.InsertItem(index, item);
        }

        private void NavPointChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(""));
        }

        protected override void RemoveItem(int index)
        {
            NavPoint item = Items[index];
            item.PropertyChanged -= NavPointChanged;
            base.RemoveItem(index);
        }
        
    }
}
