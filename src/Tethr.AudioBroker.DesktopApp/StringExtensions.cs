using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Tethr.AudioBroker.DesktopApp
{
    internal static class StringExtensions
    {
	    public static string NullIfEmpty(this string value)
	    {
		    return string.IsNullOrEmpty(value) ? null : value;
	    }

	    public static string TextValueOrNull(this TextBox value)
	    {
		    return string.IsNullOrEmpty(value.Text) ? null : value.Text;
	    }
    }
}
