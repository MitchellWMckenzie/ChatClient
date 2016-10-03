using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace Messenger.Classes
{
    public class Users : INotifyPropertyChanged
    {

        private string color;
        private string name;
        private MainWindow window;
        private ImageSource source;
        private int width = 0;

        public Users(string Name, string Color)
        {
            this.name = Name;
            this.color = Color;
        }

        public void setMainWindow(MainWindow window)
        {
            this.window = window;
        }

        public string Color {
            get 
            { return color; }
            set
            {
                color = value;
                if (PropertyChanged != null)
                {
                    OnPropertyChanged("Color");
                }
            }
        }


        public ImageSource Source
        {
            get
            {
                return source;
            }
            set
            {
                this.source = value;
            }
        }
        public int Width
        {
            get
            {
                return width;
            }

            set
            {
                width = value;
                OnPropertyChanged("Width");
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => { window.lstUsers.ItemsSource = ""; window.lstUsers.ItemsSource = window.listofUsers; }));
            }
        }

        public String Name {
            get
            {
                return name;
            }
            set
            {
                name = value;
                if (PropertyChanged != null)
                {
                    OnPropertyChanged("Name");
                }

            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
