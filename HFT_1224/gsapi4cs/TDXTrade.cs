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

        public  DataTable QueryStock()
        {
            QueryData(ClientID, 1, Result, ErrInfo);//查询股份

            DataTable dt = new DataTable();
            dt.Columns.Add("Ticker");
            dt.Columns.Add("Name");
            dt.Columns.Add("Position");
            dt.Columns.Add("CanSell");
            dt.Columns.Add("TodayBuy");
            dt.Columns.Add("Cost");
            dt.Columns.Add("OrderAvgPrice");
            dt.Columns.Add("NowPrice");
            ErrorMsg = ErrInfo.ToString();

            if (ErrorMsg != string.Empty)
                return dt;
            else
            {
                string[] Strvalue = Result.ToString().Split('\n');
                for (int i = 1; i < Strvalue.Length; i++)
                {
                    string[] Str = Strvalue[i].Split('\t');
                    DataRow dr = dt.NewRow();
                    dr["Ticker"] = Str[0];
                    dr["Name"] = Str[1];
                    dr["Position"] =Convert.ToInt32(Str[2]);
                    dr["CanSell"] = Convert.ToInt32(Str[3]);
                    dr["TodayBuy"] =Convert.ToInt32(Str[4]);
                    dr["Cost"] = Convert.ToDouble(Str[5]);
                    dr["OrderAvgPrice"] = Convert.ToDouble(Str[6]);
                    dr["NowPrice"] = Convert.ToDouble(Str[7]);
                    dt.Rows.Add(dr);

                }

                return dt;
            }
      
        
        }

        public DataTable QueryList()
        {
            QueryData(ClientID, 2, Result, ErrInfo);//查询委托

            DataTable dt = new DataTable();
            dt.Columns.Add("Ticker");
            dt.Columns.Add("Name");
            dt.Columns.Add("Direction");
            dt.Columns.Add("Sign");
            dt.Columns.Add("QuantityListed");
            dt.Columns.Add("PriceListed");
            dt.Columns.Add("QuantityExecuted");
            dt.Columns.Add("QuantityCancled");
            dt.Columns.Add("ListTime");
            dt.Columns.Add("ListNum");
            dt.Columns.Add("ListSign");
            dt.Columns.Add("StockHolder");
            dt.Columns.Add("Exchange");

            dt.PrimaryKey = new DataColumn[] { dt.Columns["ListNum"] };
            ErrorMsg = ErrInfo.ToString();

            if (ErrorMsg != string.Empty)
                return dt;
            else
            {
                string[] Strvalue = Result.ToString().Split('\n');
                for (int i = 1; i < Strvalue.Length; i++)
                {
                    string[] Str = Strvalue[i].Split('\t');
                    DataRow dr = dt.NewRow();
                    dr["Ticker"] = Str[0];
                    dr["Name"] = Str[1];
                    dr["Direction"] = Str[2];
                    dr["Sign"]=Convert.ToInt32(Str[3]);
                    dr["QuantityListed"] = Convert.ToInt32(Str[4]);
                    dr["PriceListed"] = Convert.ToDouble(Str[5]);
                    dr["QuantityExecuted"] = Convert.ToInt32(Str[6]);
                    dr["QuantityCancled"] = Convert.ToInt32(Str[7]);
                    dr["ListTime"] = Str[8];
                    //委托编号
                    dr["ListNum"] = Convert.ToInt32(Str[9]);
                    dr["ListSign"] = Str[10];
                    dr["StockHolder"] =Str[11];
                    dr["Exchange"] = Str[12];
                    dt.Rows.Add(dr);

                }

                return dt;
            }


        }

        public DataTable QueryOrder()
        {
            QueryData(ClientID, 3, Result, ErrInfo);//查询成交
            DataTable dt = new DataTable();
            dt.Columns.Add("Ticker");
            dt.Columns.Add("Name");
            dt.Columns.Add("Direction");
            dt.Columns.Add("Sign");      
            dt.Columns.Add("QuantityExecuted");
            dt.Columns.Add("PriceExecuted");        
            dt.Columns.Add("AmtExecuted");
            dt.Columns.Add("Abstract");          
            dt.Columns.Add("ListNum");
            dt.Columns.Add("OrderNum");
            dt.Columns.Add("OrderTime");         
            dt.Columns.Add("StockHolder");
            dt.Columns.Add("Exchange");
          


            ErrorMsg = ErrInfo.ToString();
            if (ErrorMsg != string.Empty)
                return dt;
            else
            {
                string[] Strvalue = Result.ToString().Split('\n');
                for (int i = 1; i < Strvalue.Length; i++)
                {
                    string[] Str = Strvalue[i].Split('\t');
                    DataRow dr = dt.NewRow();
                    dr["Ticker"] = Str[0];
                    dr["Name"] = Str[1];
                    dr["Direction"] = Str[2];
                    dr["Sign"] = Convert.ToInt32(Str[3]);
                    dr["QuantityExecuted"] = Convert.ToInt32(Str[4]);
                    dr["PriceExecuted"] = Convert.ToDouble(Str[5]);
                    dr["AmtExecuted"] = Convert.ToDouble(Str[6]);
                    dr["Abstract"] =Str[7];
                    //委托编号
                    dr["ListNum"] = Str[8];
                    //成交编号
                    dr["OrderNum"] = Convert.ToInt32(Str[9]);
                    dr["OrderTime"] = Str[10];
                    dr["StockHolder"] = Str[11];
                    dr["Exchange"] = Str[12];
                    dt.Rows.Add(dr);

                }

                return dt;
            }


        }

        public DataTable QueryCancel()
        {
            QueryData(ClientID, 4, Result, ErrInfo);//查询可撤

            DataTable dt = new DataTable();
            dt.Columns.Add("Ticker");
            dt.Columns.Add("Name");
            dt.Columns.Add("Direction");
            dt.Columns.Add("Sign");
            dt.Columns.Add("QuantityListed");
            dt.Columns.Add("PriceListed");
            dt.Columns.Add("QuantityExecuted");
            dt.Columns.Add("QuantityCancled");
            dt.Columns.Add("ListTime");
            dt.Columns.Add("ListNum");
            dt.Columns.Add("ListSign");
            dt.Columns.Add("StockHolder");
            dt.Columns.Add("Exchange");
            dt.PrimaryKey = new DataColumn[] { dt.Columns["ListNum"] };


            ErrorMsg = ErrInfo.ToString();
            if (ErrorMsg != string.Empty)
                return dt;
            else
            {
                string[] Strvalue = Result.ToString().Split('\n');
                for (int i = 1; i < Strvalue.Length; i++)
                {
                    string[] Str = Strvalue[i].Split('\t');
                    DataRow dr = dt.NewRow();
                    dr["Ticker"] = Str[0];
                    dr["Name"] = Str[1];
                    dr["Direction"] = Str[2];
                    dr["Sign"] = Convert.ToInt32(Str[3]);
                    dr["QuantityListed"] = Convert.ToInt32(Str[4]);
                    dr["PriceListed"] = Convert.ToDouble(Str[5]);
                    dr["QuantityExecuted"] = Convert.ToInt32(Str[6]);
                    dr["QuantityCancled"] = Convert.ToInt32(Str[7]);
                    dr["ListTime"] = Str[8];
                    //委托编号
                    dr["ListNum"] = Convert.ToInt32(Str[9]);
                    dr["ListSign"] = Str[10];
                    dr["StockHolder"] = Str[11];
                    dr["Exchange"] = Str[12];
                    dt.Rows.Add(dr);
                }

                return dt;
            }


        }

        public void cancel()
        { 
             //CancelOrder(int ClientID, char* ExchangeID, char* hth, char* Result, char* ErrInfo);
            CancelOrder( ClientID, "1", "11223",Result,  ErrInfo);
            if (string.IsNullOrEmpty(Convert.ToString(ErrInfo)))
            {
                //正常撤单
            }
            else
            {
                //撤单出错
            }
        }




        public void Send()
        {
             //num为委托编号
             string num = "";
             SendOrder(ClientID, 0, 0, "B880368732", "511880", 103.3f, 100, Result, ErrInfo);
             if (!string.IsNullOrEmpty(Convert.ToString(ErrInfo)))
             {
                 //下单被拒
             }
             else
             {
                 foreach (char item in Convert.ToString(Result))
                 {
                     if (item >= 48 && item <= 58)
                     {
                         num += item;
                     }
                 }
                 num = num.Substring(0, num.Length - 1);
             }


        
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
