using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace HFT
{

    public class TDXTrade
    {
        ///基本版
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void OpenTdx();
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void CloseTdx();
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern int Logon(string IP, short Port, string Version, short YybID, string AccountNo, string TradeAccount, string JyPassword, string TxPassword, StringBuilder ErrInfo);
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void Logoff(int ClientID);
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void QueryData(int ClientID, int Category, StringBuilder Result, StringBuilder ErrInfo);
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void SendOrder(int ClientID, int Category, int PriceType, string Gddm, string Zqdm, float Price, int Quantity, StringBuilder Result, StringBuilder ErrInfo);
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void CancelOrder(int ClientID, string ExchangeID, string hth, StringBuilder Result, StringBuilder ErrInfo);
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void GetQuote(int ClientID, string Zqdm, StringBuilder Result, StringBuilder ErrInfo);
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void Repay(int ClientID, string Amount, StringBuilder Result, StringBuilder ErrInfo);

        ///普通批量版
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void QueryHistoryData(int ClientID, int Category, string StartDate, string EndDate, StringBuilder Result, StringBuilder ErrInfo);
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void QueryDatas(int ClientID, int[] Category, int Count, IntPtr[] Result, IntPtr[] ErrInfo);
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void SendOrders(int ClientID, int[] Category, int[] PriceType, string[] Gddm, string[] Zqdm, float[] Price, int[] Quantity, int Count, IntPtr[] Result, IntPtr[] ErrInfo);
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void CancelOrders(int ClientID, string[] ExchangeID, string[] hth, int Count, IntPtr[] Result, IntPtr[] ErrInfo);
        [DllImport("trade.dll", CharSet = CharSet.Ansi)]
        public static extern void GetQuotes(int ClientID, string[] Zqdm, int Count, IntPtr[] Result, IntPtr[] ErrInfo);

        public int ClientID;//目前仅限单账户，单一ClientID
        public StringBuilder ErrInfo = new StringBuilder(256);
        public StringBuilder Result = new StringBuilder(1024 * 1024);

        public String ErrorMsg;

        public TDXTrade()
        {


        }
        
        public bool TDXOpen()
        {
            OpenTdx();//打开通达信

            //登录
            ClientID = Logon("218.18.103.39", 7708, "6.0", 1100, "110099100137", "110099100137", "037934", string.Empty, ErrInfo);
            ErrorMsg = ErrInfo.ToString();

            if (ClientID == -1)
                return false;
            else
                return true;

            //登录第二个帐号
            //int ClientID2 = Logon("111.111.111.111", 7708, "4.20", 0, "5555555555", "1111", "555", string.Empty, ErrInfo);
        }

        public void TDXClose()
        {
            Logoff(ClientID);//注销
            CloseTdx();//关闭通达信
        }

        public bool IsTDXConnected()
        {
            //查询资金，看是否已连接
            QueryData(ClientID, 0, Result, ErrInfo);//查询资金

            ErrorMsg = ErrInfo.ToString();

            if (ErrorMsg != string.Empty)
                return false;
            else
                return true;

        }

        public String QueryCash()
        {
            //查询资金，看是否已连接
            QueryData(ClientID, 0, Result, ErrInfo);//查询资金


            ErrorMsg = ErrInfo.ToString();

            if (ErrorMsg != string.Empty)
                return null;
            else
                return Result.ToString();

        }

        public void TDXTest()
        {

            //DLL是32位的,因此必须把C#工程生成的目标平台从Any CPU改为X86,才能调用DLL;
            //必须把Trade.dll等4个DLL复制到Debug和Release工程目录下;

            //普通下单
            //SendOrder(ClientID, 0, 0, "B880368732", "511880", 103.3f, 100, Result, ErrInfo);


            //批量下单，流速由券商柜台控制，每笔最好不超过10只
            int Count = 2;

            int[] CategoryList = new int[Count];
            int[] PriceTypeList = new int[Count];
            string[] GddmList = new string[Count];
            string[] ZqdmList = new string[Count];
            float[] PriceList = new float[Count];
            int[] QuantityList = new int[Count];


            string[] Results = new string[ZqdmList.Length];
            string[] ErrInfos = new string[ZqdmList.Length];
            IntPtr[] ErrInfoPtr = new IntPtr[ZqdmList.Length];
            IntPtr[] ResultPtr = new IntPtr[ZqdmList.Length];

            for (int i = 0; i < ZqdmList.Length; i++)
            {
                ResultPtr[i] = Marshal.AllocHGlobal(1024 * 1024);
                ErrInfoPtr[i] = Marshal.AllocHGlobal(256);
            }
            

            for (int i = 0; i < Count; i++)
            {
                CategoryList[i] = 0;//第i个单的类别(0为买,1为卖)
                PriceTypeList[i] = 0;//第i个单的报价方式
            
                GddmList[i] = "B880368732";//第i个单的股东代码，上海
                //GddmList[i] = "0899094024";//第i个单的股东代码，深圳
                ZqdmList[i] = "511990";//第i个单的证券代码
                PriceList[i] = 101.0f;//第i个单的价格
          
                QuantityList[i] = 100;//第i个单的数量
            }

            SendOrders(ClientID, CategoryList, PriceTypeList, GddmList, ZqdmList, PriceList, QuantityList, Count, ResultPtr, ErrInfoPtr);

            for (int i = 0; i < ZqdmList.Length; i++)
            {
                Results[i] = Marshal.PtrToStringAnsi(ResultPtr[i]);
                ErrInfos[i] = Marshal.PtrToStringAnsi(ErrInfoPtr[i]);

                Marshal.FreeHGlobal(ResultPtr[i]);
                Marshal.FreeHGlobal(ErrInfoPtr[i]);
            }

            //GetQuote(ClientID, "601988", Result, ErrInfo);//查询五档报价
        }
    }
}
