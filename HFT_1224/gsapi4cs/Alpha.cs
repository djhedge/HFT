using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using FIXApi;
using TDFAPI;
using System.Threading;

namespace HFT
{
    public partial class Alpha : Form
    {
        public Dictionary<string, SingleStockData> dataDict;

        public DataClass dc;
        public FIXTrade tc;
        public EngineClass ec;
        public MainForm MF;
        
        public class OrderClass
        {
            public string Account {get;set;}
            public string Strategy {get;set;}
          //  public string SubStrategy1 { get; set; }
          //  public string SubStrategy2 { get; set; }
            public string Ticker { get; set; }
            public Int32 Num { get; set; }
            public string Direction { get; set; }
        }

  
   
        public List<OrderClass> OrderClassList;

    
        public Alpha()
        {
            InitializeComponent();
           
        }
  
        private void button1_Click(object sender, EventArgs e)
        {
            //限价
            //string tickerListStr = dc.LoadDataDict();
            //dc.SetSubscription(tickerListStr, SubscriptionType.SUBSCRIPTION_SET);
            //while (dataDict.Count < 1000)
            //    Thread.Sleep(1000);

            if  (OrderClassList.Count==0)
                MessageBox.Show("无下单股票");
            
            foreach (OrderClass oclass in OrderClassList)
            {
                OrderBookEntry entry = new OrderBookEntry();
                entry.Account = oclass.Account;
                //买卖方向
                entry.action = (OrderAction)Enum.Parse(typeof(OrderAction),oclass.Direction);    
                entry.orderTime =Convert.ToInt32(DateTime.Now.ToString("Hmmss"));
                entry.orderDate = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd"));
                entry.ticker = oclass.Ticker;
                entry.strategies = (TradingStrategies)Enum.Parse(typeof(TradingStrategies), oclass.Strategy);
                entry.quantityListed = oclass.Num;
              
                //限价
                //entry.bMarket = false;
                //entry.priceListed = 10;
                //市价
                entry.bMarket = true;
                entry.priceListed = 0;

                //模拟交易时使用资金账户买
                if (Convert.ToString(entry.action).Contains("Buy"))
                    entry.type = OrderType.CashBuy;
            
                else
                    entry.type = OrderType.CashSell;

                Thread orderThread = new Thread(new ParameterizedThreadStart(ec.OrderRouter));
                orderThread.IsBackground = true;
                orderThread.Start(entry);
                Thread.Sleep(100);

            }


           
        }

      
        
        private void button2_Click(object sender, EventArgs e)
        {

            List<string> accountList = new List<string>();
            if (checkBoxGuosenHedge1.Checked)
                accountList.Add("dj1");
            if (checkBoxCiccSeed1.Checked)
                accountList.Add("zj1");
            if (checkBoxCiccSeed2.Checked)
                accountList.Add ("zj2");
            if (checkBoxHY.Checked)
                accountList.Add("hy1");

            List<string> strategyList = new List<string>();

            if (checkBox1.Checked)
                strategyList.Add(Convert.ToString(checkBox1.Text));
           if (checkBox2.Checked)
                strategyList.Add(Convert.ToString(checkBox2.Text));
           
            
            //各个策略调仓股票之和
            OrderClassList = new List<OrderClass>();
               
            
            
            SqlConnection conn = MF.conn;
            DataTable dt = new DataTable();
           
            string sql = "select * from openbasket";
           
            SqlDataAdapter SDA = new SqlDataAdapter(sql, conn);
            SDA.Fill(dt);

            for (int i = 0; i < dt.Rows.Count;i++ )
            {
                DataRow dr = dt.Rows[i];
                string[] strvalue =Convert.ToString(dr["Strategy"]).Split('_');
                string Strategy = strvalue[0];
                string SubStrategy1 = strvalue[1];
                string SubStrategy2 = strvalue[2];
                string account = Convert.ToString(dr["Account"]);

                if (strategyList.IndexOf(Convert.ToString(dr["Strategy"])) < 0)
                    continue;
                if (accountList.IndexOf(account) < 0)
                    continue;

                int weight = Convert.ToInt32(dr["TargetNumIF"]);
                List<OrderClass> tmpList = GetDifOrder(conn, account, Strategy, SubStrategy1, SubStrategy2, weight);
                //加入
                if (tmpList.Count > 0)
                    OrderClassList.AddRange(tmpList);

            }



            DataTable orderDt = new DataTable();
            orderDt.Columns.Add("Account");
            orderDt.Columns.Add("Strategy");
            orderDt.Columns.Add("Ticker");
            orderDt.Columns.Add("Position");
            orderDt.Columns.Add("Direction");
            for (int i = 0; i < OrderClassList.Count; i++)
            {
                DataRow dr = orderDt.NewRow();
                dr["Account"] = OrderClassList[i].Account;
                dr["Strategy"] = OrderClassList[i].Strategy;
                dr["Ticker"] = OrderClassList[i].Ticker;
                dr["Position"] = OrderClassList[i].Num;
                dr["Direction"] = OrderClassList[i].Direction;
                orderDt.Rows.Add(dr);
            
            }






           //dataGridView1.DataSource = OrderClassList;
            dataGridView1.DataSource = orderDt;
           dataGridView1.Refresh();
          
        }




        private List<OrderClass> GetDifOrder(SqlConnection conn,string account,string Strategy,string SubStrategy1,string SubStrategy2,int weight)
        {
        
            List<OrderClass> OrderList=new List<OrderClass>();
            //Inventory表

            string sql = "select A.Ticker,A.PrePosition,A.TodayBuy,A.TodaySell,B.Ticker as BTicker,B.Position as BPosition from" +
              "(select Ticker,PrePosition,TodayBuy,TodaySell from Inventory where Account='" + account + "' and Strategy='" + Strategy + "' and SubStrategy1='" + SubStrategy1 + "' and SubStrategy2='" + SubStrategy2 + "') A left join" +
             "(select Ticker,Position from BasketList where  Strategy='" + Strategy + "' and SubStrategy1='" + SubStrategy1 + "' and SubStrategy2='" + SubStrategy2 + "' and " + weight + "* position>0) B on A.Ticker=B.Ticker" +

             " union " +

            "select A.Ticker,A.PrePosition,A.TodayBuy,A.TodaySell,B.Ticker as BTicker,B.Position as BPosition from" +
              "(select Ticker,PrePosition,TodayBuy,TodaySell from Inventory where Account='" + account + "' and Strategy='" + Strategy + "' and SubStrategy1='" + SubStrategy1 + "' and SubStrategy2='" + SubStrategy2 + "') A right join" +
             "(select Ticker,Position from BasketList where Strategy='" + Strategy + "' and SubStrategy1='" + SubStrategy1 + "'and SubStrategy2='" + SubStrategy2 + "' and " + weight + "* position>0) B on A.Ticker=B.Ticker" +


           " order by A.Ticker";
           
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader sqldr = cmd.ExecuteReader();

            while (sqldr.Read())
            { 
               
                          
                OrderClass TmpClass=new OrderClass();
                TmpClass.Account = account;
                TmpClass.Strategy = Strategy+"_"+SubStrategy1+"_"+SubStrategy2;
                //TmpClass.SubStrategy1 = SubStrategy1;
                //TmpClass.SubStrategy2 = SubStrategy2;
               
               //BPosition为目标数量,PrePosition为昨天数量,TodayBuy为今买,TodaySell为今卖
               //买入
               if (sqldr.IsDBNull(sqldr.GetOrdinal("Ticker")))
               {
                   TmpClass.Ticker = sqldr.GetString(sqldr.GetOrdinal("BTicker"));
                   TmpClass.Num = sqldr.GetInt32(sqldr.GetOrdinal("BPosition")) * weight;
                   TmpClass.Direction = "Buy";
                           
               }
               //卖出
               else if (sqldr.IsDBNull(sqldr.GetOrdinal("BTicker")))
               {
                   TmpClass.Ticker = sqldr.GetString(sqldr.GetOrdinal("Ticker"));
                   //早上开盘调仓为佳，此时TodaySell为0，如果下午调仓, 股票当天如有交易会有影响
                   //可卖数量(<=需要卖的数量)
                   TmpClass.Num = sqldr.GetInt32(sqldr.GetOrdinal("PrePosition")) - sqldr.GetInt32(sqldr.GetOrdinal("TodaySell"));
                   TmpClass.Direction = "Sell";

               }
               //调整个股数量的逻辑,暂不使用
               else
               {
                   //TmpClass.Ticker = sqldr.GetString(sqldr.GetOrdinal("Ticker"));
                   ////需要调整的数量（正为买，负为卖）
                   //int tmpnum = sqldr.GetInt32(sqldr.GetOrdinal("BPosition")) * weight - (sqldr.GetInt32(sqldr.GetOrdinal("PrePosition")) - sqldr.GetInt32(sqldr.GetOrdinal("TodaySell")) + sqldr.GetInt32(sqldr.GetOrdinal("TodayBuy")));
                   ////可卖数量
                   //int cansellnum = sqldr.GetInt32(sqldr.GetOrdinal("PrePosition")) - sqldr.GetInt32(sqldr.GetOrdinal("TodaySell"));
                   //if (tmpnum > 0)
                   //{
                   //    TmpClass.Num = tmpnum;
                   //    TmpClass.Direction = "Buy";
                   //}
                   //else if (tmpnum < 0)
                   //{
                   //    TmpClass.Num = Math.Min(-tmpnum, cansellnum);
                   //    TmpClass.Direction = "Sell";
                   //}
               }

               if (TmpClass.Num == 0)
                   continue;
               else
                   OrderList.Add(TmpClass);           
            }

            sqldr.Close();
            return OrderList;
        
        }

        private void Alpha_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!(tc == null))
            {
                if (!(tc.OrderBook.Count == 0))
                tc.OrderBookUpdate(MF.conn);
            }               
            this.Dispose();
            
        }


   
    }
}
