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
               
                
                entry.Account = "dj1";
                //买卖方向
                entry.action = (OrderAction)Enum.Parse(typeof(OrderAction),oclass.Direction);    
                entry.orderTime =Convert.ToInt32(DateTime.Now.ToString("Hmmss"));
                entry.orderDate = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd"));
                entry.ticker = oclass.Ticker;
                entry.strategies = (TradingStrategies)Enum.Parse(typeof(TradingStrategies), oclass.Strategy);
                entry.quantityListed = oclass.Num;
              
                //限价
                //entry.bMarket = false;
                //entry.priceListed = dataDict[oclass.Ticker].AskPrices[4];
                //市价
                entry.bMarket = true;
                entry.priceListed =0;

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
           
            
            //各个策略调仓股票之和
            OrderClassList = new List<OrderClass>();
           

            //string constr = DataClass.connStr;
            string constr = "server=192.168.0.169,1433;database=HedgeHogDB;User ID=wg;Password=Pass@word;Connection Timeout=30";
            SqlConnection conn = new SqlConnection(constr);
            conn.Open();

            //主键的关键字，保障唯一

            //下单账户
            string account="dj1";
            string Strategy;       
            string SubStrategy1 ;
            string SubStrategy2;
            int weight;

            //一共9个策略，目前只使用3个
            //ZZ500 capflow
            if (Convert.ToInt16(textBox1.Text) > 0)
            {
              
                Strategy="alpha";
                SubStrategy1 = "capflow";
                SubStrategy2 = "ZZ500";
                weight = Convert.ToInt16(textBox1.Text);
                //该策略调仓股票
                List<OrderClass> tmpList=GetDifOrder(conn,account,Strategy,SubStrategy1,SubStrategy2,weight);
                //加入
                if (tmpList.Count > 0)
                     OrderClassList.AddRange(tmpList);
            }
            //ZZ500 mamount
            if (Convert.ToInt16(textBox2.Text) >= 0)
            {

                Strategy = "alpha";
                SubStrategy1 = "mamount";
                SubStrategy2 = "ZZ500";
                weight = Convert.ToInt16(textBox2.Text);
                List<OrderClass> tmpList = GetDifOrder(conn, account, Strategy, SubStrategy1, SubStrategy2, weight);

                if (tmpList.Count > 0)
                    OrderClassList.AddRange(tmpList);
            }
            //ZZ500 mv
            if (Convert.ToInt16(textBox3.Text) >0)
            {

                Strategy = "alpha";
                SubStrategy1 = "mv";
                SubStrategy2 = "ZZ500";
                weight = Convert.ToInt16(textBox3.Text);
                List<OrderClass> tmpList = GetDifOrder(conn, account, Strategy, SubStrategy1, SubStrategy2, weight);

                if (tmpList.Count > 0)
                    OrderClassList.AddRange(tmpList);
            }
            //HS300 capflow
            if (Convert.ToInt16(textBox4.Text) > 0)
            {

                Strategy = "alpha";
                SubStrategy1 = "capflow";
                SubStrategy2 = "HS300";
                weight = Convert.ToInt16(textBox4.Text);
                List<OrderClass> tmpList = GetDifOrder(conn, account, Strategy, SubStrategy1, SubStrategy2, weight);

                if (tmpList.Count > 0)
                    OrderClassList.AddRange(tmpList);
            }
            //HS300 capflow
            if (Convert.ToInt16(textBox5.Text) > 0)
            {

                Strategy = "alpha";
                SubStrategy1 = "mamount";
                SubStrategy2 = "HS300";
                weight = Convert.ToInt16(textBox5.Text);
                List<OrderClass> tmpList = GetDifOrder(conn, account, Strategy, SubStrategy1, SubStrategy2, weight);

                if (tmpList.Count > 0)
                    OrderClassList.AddRange(tmpList);
            }
            //HS300 mv
            if (Convert.ToInt16(textBox6.Text) > 0)
            {

                Strategy = "alpha";
                SubStrategy1 = "mv";
                SubStrategy2 = "HS300";
                weight = Convert.ToInt16(textBox6.Text);
                List<OrderClass> tmpList = GetDifOrder(conn, account, Strategy, SubStrategy1, SubStrategy2, weight);

                if (tmpList.Count > 0)
                    OrderClassList.AddRange(tmpList);
            }
            //TotalA capflow
            if (Convert.ToInt16(textBox7.Text) > 0)
            {

                Strategy = "alpha";
                SubStrategy1 = "capflow";
                SubStrategy2 = "TotalA";
                weight = Convert.ToInt16(textBox7.Text);
                List<OrderClass> tmpList = GetDifOrder(conn, account, Strategy, SubStrategy1, SubStrategy2, weight);

                if (tmpList.Count > 0)
                    OrderClassList.AddRange(tmpList);
            }
            // TotalA mamount
            if (Convert.ToInt16(textBox8.Text) >= 0)
            {

                Strategy = "alpha";
                SubStrategy1 = "mamount";
                SubStrategy2 = "TotalA";
                weight = Convert.ToInt16(textBox8.Text);
                List<OrderClass> tmpList = GetDifOrder(conn, account, Strategy, SubStrategy1, SubStrategy2, weight);

                if (tmpList.Count > 0)
                    OrderClassList.AddRange(tmpList);
            }
            //TotalA mv
            if (Convert.ToInt16(textBox9.Text) > 0)
            {

                Strategy = "alpha";
                SubStrategy1 = "mv";
                SubStrategy2 = "TotalA";
                weight = Convert.ToInt16(textBox9.Text);
                List<OrderClass> tmpList = GetDifOrder(conn, account, Strategy, SubStrategy1, SubStrategy2, weight);

                if (tmpList.Count > 0)
                    OrderClassList.AddRange(tmpList);
            }

         
            conn.Close();


           dataGridView1.DataSource = OrderClassList;
         //   dataGridView1.Columns[0].HeaderText = "Ticker";
          //  dataGridView1.Columns[0].HeaderText = "Num";
            dataGridView1.Refresh();
          
        }




        private List<OrderClass> GetDifOrder(SqlConnection conn,string account,string Strategy,string SubStrategy1,string SubStrategy2,int weight)
        {
        
            List<OrderClass> OrderList=new List<OrderClass>();
            //Inventory表
            string sql = "select A.Ticker,A.PrePosition,A.TodayBuy,A.TodaySell,B.Ticker as BTicker,B.Position as BPosition from" +
                "(select Ticker,PrePosition,TodayBuy,TodaySell from Inventory where Account='" + account + "' and Strategy='" + Strategy + "' and SubStrategy1='" + SubStrategy1 + "' and SubStrategy2='" + SubStrategy2 + "') A left join" +
               "(select Ticker,Position from BasketList where Account='" + account + "' and Strategy='" + Strategy + "' and SubStrategy1='" + SubStrategy1 + "' and SubStrategy2='" + SubStrategy2 + "' ) B on A.Ticker=B.Ticker" +

               " union " +

              "select A.Ticker,A.PrePosition,A.TodayBuy,A.TodaySell,B.Ticker as BTicker,B.Position as BPosition from" +
                "(select Ticker,PrePosition,TodayBuy,TodaySell from Inventory where Account='" + account + "' and Strategy='" + Strategy + "' and SubStrategy1='" + SubStrategy1 + "' and SubStrategy2='" + SubStrategy2 + "') A right join" +
               "(select Ticker,Position from BasketList where Account='" + account + "' and Strategy='" + Strategy + "' and SubStrategy1='" + SubStrategy1 + "'and SubStrategy2='" + SubStrategy2 + "' ) B on A.Ticker=B.Ticker" +


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
                   TmpClass.Ticker = sqldr.GetString(sqldr.GetOrdinal("Ticker"));
                   //需要调整的数量（正为买，负为卖）
                   int tmpnum = sqldr.GetInt32(sqldr.GetOrdinal("BPosition")) * weight - (sqldr.GetInt32(sqldr.GetOrdinal("PrePosition")) - sqldr.GetInt32(sqldr.GetOrdinal("TodaySell")) + sqldr.GetInt32(sqldr.GetOrdinal("TodayBuy")));
                   //可卖数量
                   int cansellnum = sqldr.GetInt32(sqldr.GetOrdinal("PrePosition")) - sqldr.GetInt32(sqldr.GetOrdinal("TodaySell"));
                   if (tmpnum > 0)
                   {
                       TmpClass.Num = tmpnum;
                       TmpClass.Direction = "Buy";
                   }
                   else if (tmpnum < 0)
                   {
                       TmpClass.Num = Math.Min(-tmpnum, cansellnum);
                       TmpClass.Direction = "Sell";
                   }
               }


               if (TmpClass.Num == 0)
                   continue;
               else
                   OrderList.Add(TmpClass);



            
            
            
            }

            sqldr.Close();
            return OrderList;
        
        }

   
    }
}
