using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSDemo
{
    class PrintHelper
    {
        public static void PrintIntArr(int[] arr, int nLen, string strTitle)
        {
            if (nLen > 0)
            {
                Console.Write("{0}:", strTitle);
            }
            else
            {
                Console.WriteLine("{0}:", strTitle);
            }

            for (int i = 0; i < nLen; i++)
            {
                if (i != nLen - 1)
                {
                    Console.Write("{0} ", arr[i]);
                }
                else
                {
                    Console.WriteLine("{0}", arr[i]);
                }
            }
        }
        public static void PrintIntArr(uint[] arr, int nLen, string strTitle)
        {
            if (nLen > 0)
            {
                Console.Write("{0}:", strTitle);
            }
            else
            {
                Console.WriteLine("{0}:", strTitle);
            }

            for (int i = 0; i < nLen; i++)
            {
                if (i != nLen - 1)
                {
                    Console.Write("{0} ", arr[i]);
                }
                else
                {
                    Console.WriteLine("{0}", arr[i]);
                }
            }
        }

        public static void PrintObject(Object obj)
        {
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(obj.GetType());

            x.Serialize(Console.Out, obj);
        }
    }
}

