using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Messenger.Classes.Server.Display_Parts
{
    public class Emoji
    {

        public string key { get; set; }
        private BitmapImage bitImage;
        public BitmapImage image {
            get
            {
                return this.bitImage;
            }
            set
            {
                this.bitImage = value;
                this.bitImage.Freeze();
                Image emojii = new Image();
                emojii.Source = bitImage;
                this.imgCtrl = emojii;
                this.imgSource = emojii.Source;
            }
        }
        public Image imgCtrl { get; set; }
        public ImageSource imgSource { get; set; }

    }
}
