using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Messenger.Classes
{
    public class TextInfo
    {

        public ImageSource ImgSource { get; set; } 
        public string ImgWidth { get; set; }
        public string ImgHeight { get; set; }
        public Visibility ImgVis { get; set; }


        public string message { get; set; }
        public string foreground { get; set; }
        public string font { get; set; }
        public string fontSize { get; set; }
        public Visibility msgVis { get; set; }


        public string url { get; set; }
        public string linkForeground { get; set; }
        public string linkFont { get; set; }
        public string linkFontSize { get; set; }

        public TextInfo(ImageSource ImgSource = null, string ImgWidth = "0", string ImgHeight = "0", Visibility ImgVis = Visibility.Hidden,
                        string message = "", string foreground = "", string font = "", string fontSize = "0", Visibility msgVis = Visibility.Hidden,
                        string url = "")
        {
            this.ImgSource = ImgSource;
            this.ImgWidth = ImgWidth;
            this.ImgHeight = ImgHeight;
            this.ImgVis = ImgVis;
            this.message = message;
            this.foreground = foreground;
            this.font = font;
            this.fontSize = fontSize;
            this.msgVis = msgVis;
            this.url = url;
            this.linkForeground = foreground;
            this.linkFontSize = fontSize;
            this.linkFont = font;
        }

    }
}
