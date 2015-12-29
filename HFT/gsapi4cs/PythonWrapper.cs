using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
//using IronPython;
//using Microsoft.Scripting.Hosting;
using System.Diagnostics;

namespace HFT
{
    public class PythonWrapper
    {
        public static void PutOrder(string strCookie, string SH, string SZ, string ticker, string direction, double priceListed, int quantityListed)
        {
            StockProfile stk = new StockProfile { };
            stk.JSESSIONID = strCookie;
            stk.baseURL = "http://trade1.cicconline.com/genOrder.do?actionType=makeOrder&stkName=&newPrice=&usableAmt=&canBuyQty=&canSellQty=0&partedOrderFlag=0&OrderTypeFlag=-1&SHB=%D7%EE%D3%C5%CE%E5%B5%B5%CA%A3%D7%AA%C2%F2%5E%D7%EE%D3%C5%CE%E5%B5%B5%CA%A3%B3%B7%C2%F2&SHBValue=WB%5EVB&SHS=%D7%EE%D3%C5%CE%E5%B5%B5%CA%A3%B3%B7%C2%F4%5E%D7%EE%D3%C5%CE%E5%B5%B5%CA%A3%D7%AA%C2%F4&SHSValue=VS%5EWS&SZB=%B1%BE%B7%BD%D7%EE%D3%C5%BC%DB%B8%F1%C2%F2%5E%C8%AB%B6%EE%B3%C9%BD%BB%C2%F2%5E%D7%EE%D3%C5%CE%E5%B5%B5%C2%F2%5E%BC%B4%CA%B1%B3%C9%BD%BB%C2%F2%5E%B6%D4%CA%D6%D7%EE%D3%C5%BC%DB%B8%F1%C2%F2&SZBValue=XB%5EWB%5EVB%5E2B%5EYB&SZS=%B1%BE%B7%BD%D7%EE%D3%C5%BC%DB%B8%F1%C2%F4%5E%D7%EE%D3%C5%CE%E5%B5%B5%C2%F4%5E%C8%AB%B6%EE%B3%C9%BD%BB%C2%F4%5E%BC%B4%CA%B1%B3%C9%BD%BB%C2%F4%5E%B6%D4%CA%D6%D7%EE%D3%C5%BC%DB%B8%F1%C2%F4&SZSValue=XS%5EVS%5EWS5E2S%5EYS&pricestrategy=ZT&orderName=0&orderStyle=&Submit3=%CF%C2%B5%A5";

            if (ticker[0] == '6' || ticker[0] == '5') stk.exchId = 0;
            else if (ticker[0] == '0' || ticker[0] == '3') stk.exchId = 1;

            if (ticker[0] == '6' || ticker[0] == '0') stk.stkType = "A0";
            else if (ticker[0] == '5') stk.stkType = "A8";
            else if (ticker[0] == '3') stk.stkType = "C8";

            stk.stkId = ticker;
            //stk.acctId = "802100016419";
            stk.acctId = "";

            if (ticker[0] == '6' || ticker[0] == '5') stk.regId = SH;
            else if (ticker[0] == '0' || ticker[0] == '3') stk.regId = SZ;

            stk.orderType = direction;


            stk.orderPrice = priceListed;
            //stk.orderPrice = 5.8;
            stk.orderQty = quantityListed;

            stk.commandURL = "&exchId=" + stk.exchId.ToString() + "&stkType=" + stk.stkType + "&stkId=" + stk.stkId + "&acctId=" + stk.acctId + "&regId=" + stk.regId + "&orderType=" + stk.orderType + "&orderPrice=" + stk.orderPrice.ToString() + "&orderQty=" + stk.orderQty.ToString();
            stk.sendURL = stk.baseURL + stk.commandURL;

            string PythonFileName = @"SendOrder.py";
            RunPythonScript(stk.JSESSIONID, stk.sendURL, PythonFileName);

            Console.WriteLine("The order has been sent.");
        }

        public static void RunPythonScript(string JSESSIONID, string SendURL, string PythonFileName)
        {
            Process p = new Process();
            //string path = "D:\\Visual Studio 2013\\Projects\\ConsoleApplication2\\ConsoleApplication2\\" + PythonFileName;
            string path = "D:\\" + PythonFileName;
            string path1 = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + PythonFileName;

            string sArg = path + " " + JSESSIONID + " " + SendURL;

            Console.WriteLine(sArg);

            p.StartInfo.FileName = @"python";
            p.StartInfo.Arguments = sArg;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.RedirectStandardError = false;
            p.StartInfo.CreateNoWindow = false;

            p.Start();
            /*
            p.BeginOutputReadLine();

            p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            */
            //Console.ReadLine();

            p.WaitForExit();
        }
        static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AppendText(e.Data + Environment.NewLine);
            }
        }
        public delegate void AppendTextCallback(string text);
        public static void AppendText(string text)
        {
            Console.WriteLine(text);

        }
    }

    public class StockProfile
    {
        //账户cookie
        public string JSESSIONID { get; set; }
        //0:上交所；1：深交所
        public int exchId { get; set; }
        //A0:A股主板；A3：分级基金；A8：场内货币基金；C8：创业板
        public string stkType { get; set; }
        //六位数字ID
        public string stkId { get; set; }
        //账户ID
        public string acctId { get; set; }
        //对应交易所股东代码
        public string regId { get; set; }
        //B：买入；S：卖出
        public string orderType { get; set; }
        //下单价格
        public double orderPrice { get; set; }
        //下单量
        public int orderQty { get; set; }
        //初始URL
        public string baseURL { get; set; }
        //执行URL
        public string commandURL { get; set; }
        public string sendURL { get; set; }
    }
}
