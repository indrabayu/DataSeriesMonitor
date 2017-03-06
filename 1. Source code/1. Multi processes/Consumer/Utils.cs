using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace CONSUMER
{
    static class Utils
    {
        //public static byte[] ToByteArray(this int value)
        //{
        //    byte[] input_Value = BitConverter.GetBytes(value);
        //    if (BitConverter.IsLittleEndian)
        //        Array.Reverse(input_Value);
        //    return input_Value;
        //}

        //public static int ToInt32(this byte[] input_Value)
        //{
        //    byte[] output_Value = new byte[input_Value.Length];
        //    Array.Copy(input_Value, output_Value, input_Value.Length);
        //    if (BitConverter.IsLittleEndian)
        //        Array.Reverse(output_Value);
        //    return BitConverter.ToInt32(output_Value, 0);
        //}

        //public static byte[] ToByteArray(this long value)
        //{
        //    byte[] input_Value = BitConverter.GetBytes(value);
        //    if (BitConverter.IsLittleEndian)
        //        Array.Reverse(input_Value);
        //    return input_Value;
        //}

        //public static long ToInt64(this byte[] array)
        //{
        //    byte[] output_Value = new byte[array.Length];
        //    Array.Copy(array, output_Value, array.Length);
        //    if (BitConverter.IsLittleEndian)
        //        Array.Reverse(output_Value);
        //    return BitConverter.ToInt64(output_Value, 0);
        //}

        public static string Beautify(this string str)
        {
            if (CONSUMER.Beautify)
            {
                var arr = str.Replace("-00", "-0").ToCharArray();
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] == '0' && (i + 1) != arr.Length && arr[i + 1] != '.')
                        arr[i] = ' ';
                    else
                        break;
                }
                return new string(arr);
            }
            else
            {
                return str;
            }
        }

        public static void MaximizeConsole()
        {
            ShowWindow(ThisConsole, MAXIMIZE);
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]

        private static extern IntPtr GetConsoleWindow();
        private static IntPtr ThisConsole = GetConsoleWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int HIDE = 0;
        private const int MAXIMIZE = 3;
        private const int MINIMIZE = 6;
        private const int RESTORE = 9;
    }
}