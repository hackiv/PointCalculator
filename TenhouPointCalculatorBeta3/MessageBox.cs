using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TenhouPointCalculatorBeta3
{
    class MessageBox
    {
        public static void Show(string txt)
        {
            var activity = MainActivity.Context as Activity;
            var adb = new AlertDialog.Builder(activity);
            activity?.RunOnUiThread(() =>
            {
                adb.SetMessage(txt);
                adb.Show();
            });
        }
    }
}