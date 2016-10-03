using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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

namespace Messenger.Classes.PictureScreen
{
    /// <summary>
    /// Interaction logic for PictureScreen.xaml
    /// </summary>
    public partial class PictureScreen : INotifyPropertyChanged
    {

        BitmapSource bitmap;

        private double _height;
        public double CustomHeight
        {
            get { return _height; }
            set
            {
                if (value != _height)
                {
                    _height = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("CustomHeight"));
                }
            }
        }
        private double _width;
        public double CustomWidth
        {
            get { return _width; }
            set
            {
                if (value != _width)
                {
                    _width = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("CustomWidth"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public PictureScreen()
        {
            InitializeComponent();
            this.DataContext = this;
            CustomWidth = 50;
            CustomHeight = 50;
            Application.Current.MainWindow = this;
        }

        public void setPicture(string image, double width, double height, string nameOfPerson, MetroWindow window)
        {
            Application.Current.MainWindow = this;
            string imgTxt = image;
            Dispatcher.Invoke(new Action(() =>
            {
                System.Drawing.Image img = Server.Server.Base64ToImage(imgTxt);
                Bitmap temp = new Bitmap(img);
                bitmap = Server.Server.ConvertBitmap(temp);
                CustomWidth = width;
                CustomHeight = height;
                pictureSent.Source = bitmap;
                this.Title = nameOfPerson;

                Application.Current.MainWindow = window;
            }));
        }

        private async void btnCopyPicture_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                System.Windows.Clipboard.SetImage(bitmap);
            }));
            await this.ShowMessageAsync("Success", "Copied to clipboard!");
        }
    }
}
