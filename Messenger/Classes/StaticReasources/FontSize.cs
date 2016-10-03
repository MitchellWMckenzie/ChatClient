using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;

namespace Messenger.Classes.StaticReasources
{
    /// <summary>
    /// Used in the 'styles.xaml' file so font size can be set statically
    /// </summary>
    class FontSize : MarkupExtension
    {
        [TypeConverter(typeof(FontSizeConverter))]
        public double Size { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Size;
        }
    }
}
