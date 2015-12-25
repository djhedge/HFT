using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FIXApi;
using TDFAPI;
using System.Threading;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
namespace HFT
{
    public partial class StockArbi : Form
    {
        public Dictionary<string, SingleStockData> dataDict;

        public DataClass dc;
        public FIXTrade tc;
        public EngineClass ec;
        public MainForm MF;
        public TDXTrade tdx;

        public DataTable dtPosition ;
      //  public SqlConnection conn;
        public StockArbi()
        {
            
            InitializeComponent();
            dtPosition =new DataTable();
            // TODO:  这行代码将数据加载到表“hedgeHogDBDataSet.v_Strategy”中。您可以根据需要移动或删除它。
            this.v_StrategyTableAdapter.Fill(this.hedgeHogDBDataSet.v_Strategy);
           
        }

        private void comboBoxIfArb_SelectedIndexChanged(object sender, EventArgs e)
        {
            string account = "";
            //单一账户对应的position，也可以写成将多个账户汇总
            if (checkBoxGuosenHedge1.Checked)
                account = "dj1";
            else if (checkBoxCiccSeed1.Checked)
                account = "zj1";
            else if (checkBoxCiccSeed2.Checked)
                account = "zj2";
            else if (checkBoxHY.Checked)
                account = "hy1";
            else
            { //MessageBox.Show("请选择账户!");
                return;
            }


            dtPosition = new DataTable();
            if ((!radioButtonSell.Checked) && (!radioButtonBuy.Checked))
            { 
                //MessageBox.Show("请选择买或者卖");
                return;
            }



            string Strategy = Convert.ToString(comboBoxIfArb.Text);
            if (string.IsNullOrEmpty(Strategy))
            { 
                //MessageBox.Show("Please Select Index!"); 
                return;
            }

            //买时从basketlist读取
            if (radioButtonBuy.Checked)
            {
                string sql = "select * from v_arbitrage where Strategy='" + Strategy + "'";
                SqlDataAdapter SDA = new SqlDataAdapter("select * from v_arbitrage where Strategy='" + Strategy + "'", MF.conn);
                SDA.Fill(dtPosition);
                //dtPosition中会包含数量为0的股票，下单注意剔除
                //第一列为Strategy，第二列为Ticker，第三列为Position,
                dtPosition.Columns.Add("Account");
                for (int i = 0; i < dtPosition.Rows.Count; i++)
                {
                    dtPosition.Rows[i]["Account"] = account;
                }


                double capital = 0;
                string[] strvalue = Strategy.Split('_');
                string sql1 = "select SUM(Position*Price_Close) from (select A.Ticker,Position,TotalAHist.Price_Close from (select * from BasketList where SubStrategy1='" + strvalue[1] + "' and SubStrategy2='" + strvalue[2] + "') A " +
                "left join TotalAHist on A.Ticker=TotalAHist.Ticker and A.HistDate=TotalAHist.HistDate) C";

                SqlCommand cmd = new SqlCommand(sql1, MF.conn);
                SqlDataReader sdr = cmd.ExecuteReader();
                while (sdr.Read())
                {

                    if (sdr.IsDBNull(0))
                        capital = 0;
                    else
                        capital = sdr.GetDouble(0);
                }
                sdr.Close();
                textBoxCiccLog2.Text = "篮子总市值为:" + capital;
            }
            //卖时从Inventory读取
            else
            {

                //每次卖时需要根据当日成交回报更新Inventory,
                //tc.OrderBookUpdate(conn);
                //PositionUpdate();

                int weight = GetWeight(MF.conn, account, Strategy);

                //每篮子对应个股持仓数
                string sql = "select Strategy + '_' + SubStrategy1 + '_' + SubStrategy2 as Strategy,Ticker,(PrePosition+TodayBuy-TodaySell) as Position,Account from Inventory where Strategy + '_' + SubStrategy1 + '_' + SubStrategy2='" + Strategy + "' and account='" + account + "'";
                SqlDataAdapter SDA = new SqlDataAdapter(sql, MF.conn);
                SDA.Fill(dtPosition);
                for (int i = 0; i < dtPosition.Rows.Count; i++)
                {
                    DataRow dr = dtPosition.Rows[i];
                    if (weight > 0)
                        dr["Position"] = Math.Round(Convert.ToDouble(dr["Position"]) / weight / 100) * 100;


                }

                //计算篮子市值
                double capital = 0;
                string[] strvalue = Strategy.Split('_');
                string subsql = "";

                if (weight == 0)
                    subsql = "select SUM(Position*Price_Close)";
                else
                    subsql = "select SUM(floor(Position/(" + weight + "*100))*100*Price_Close)";
                string sql1 = subsql + " from (select A.Position,B.Price_Close from (select Ticker,PrePosition as Position,Account from Inventory where Strategy + '_' + SubStrategy1 + '_' + SubStrategy2='" + Strategy + "' and account='" + account + "') A " +
               " left join" +
                "(select * from TotalAHist where HistDate= (select MAX(HistDate) from TotalAHist)) B on A.Ticker=B.Ticker) C";

                SqlCommand cmd = new SqlCommand(sql1, MF.conn);
                SqlDataReader sdr = cmd.ExecuteReader();
                while (sdr.Read())
                {
                    if (sdr.IsDBNull(0))
                        capital = 0;
                    else
                    {
                        capital = sdr.GetDouble(0);
                    }
                }
                sdr.Close();
                textBoxCiccLog2.Text = "篮子总市值为:" + capital;



            }

            //订阅行情
            string TickerList = "";
            string ticker = "", tickerWithMarket = "", tickerListStr = "";

            for (int i = 0; i < dtPosition.Rows.Count; i++)
            {
                DataRow dr = dtPosition.Rows[i];
                ticker = Convert.ToString(dr["Ticker"]);
                ticker = ticker.Trim();

                if (ticker.StartsWith("60") || ticker.StartsWith("51"))
                {
                    tickerWithMarket = ticker + ".sh";   // 上海
                }
                else if (ticker.StartsWith("00") || ticker.StartsWith("30") || ticker.StartsWith("15"))
                {
                    tickerWithMarket = ticker + ".sz";    // 深圳
                }

                if (!TickerList.Contains(ticker))
                {

                    tickerListStr = tickerListStr + ";" + tickerWithMarket;
                }

                SingleStockData sd = new SingleStockData(ticker);
                sd.StatusTrending = StrategyStatus.New;
                sd.StatusReverse = StrategyStatus.New;

                if (!dataDict.ContainsKey(ticker))
                    dataDict.Add(ticker, sd);
            }
            dc.SetSubscription(tickerListStr, SubscriptionType.SUBSCRIPTION_ADD);
            //将行情输入dtPosition
            dtPosition.Columns.Add("OrderPrice");
            //刷新datagridview
            System.Timers.Timer t = new System.Timers.Timer(5000);   //实例化Timer类，设置间隔时间为5000毫秒；   
            t.Elapsed += new System.Timers.ElapsedEventHandler(theout); //到达时间的时候执行事件；   
            t.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；   
            t.Enabled = true;

            G_position.DataSource = dtPosition;
        }

        public void theout(object source, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer t = (System.Timers.Timer)source;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new System.Timers.ElapsedEventHandler(theout), source, e);
                // this.Invoke(new System.Timers.ElapsedEventHandler(theout),source,e);
            }
            else
            {
                for (int i = 0; i < dtPosition.Rows.Count; i++)
                {
                    string ticker = "";
                    int level = 4;
                    DataRow dr = dtPosition.Rows[i];
                    ticker = Convert.ToString(dr["Ticker"]);
                    ticker = ticker.Trim();
                    if (dataDict.ContainsKey(ticker))
                    {
                        if (radioButtonBuy.Checked)
                            dr["OrderPrice"] = Convert.ToDouble(dataDict[ticker].AskPrices[level]);
                        else if (radioButtonSell.Checked)
                            dr["OrderPrice"] = Convert.ToDouble(dataDict[ticker].BidPrices[level]);
                    }
                    else
                        dr["OrderPrice"] = 0;

                }
              G_position.Refresh();
            }
        }

        public int GetWeight(SqlConnection conn, string account, string Strategy)
        {
            int weight = 0;
            string sql = "select TargetNumIF from OpenBasket where Account='" + account + "' and Strategy='" + Strategy + "'";
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader sdr = cmd.ExecuteReader();
            while (sdr.Read())
            {
                weight = sdr.GetInt32(sdr.GetOrdinal("TargetNumIF"));

            }
            sdr.Close();
            return weight;

        }
   

        private void buttonIFArbRun_Click(object sender, EventArgs e)
        {
            if ((radioButtonBuy.Checked) && (radioButtonSell.Checked))
            {
                MessageBox.Show("组合选择错误");
                return;
            }
            OrderType _orderType;
            if (radioButtonBuy.Checked)

                _orderType = OrderType.CashBuy;
            else if (radioButtonSell.Checked)
                _orderType = OrderType.CashSell;
            else
            {
                MessageBox.Show("未选择指令方式");
                return;
            }

            if (radioButtonMarketOrder.Checked && radioButtonLimitOrder.Checked)
            {
                MessageBox.Show("下单方式选择错误");
                return;
            }



            int quantity = 0; string ticker = ""; double price = 0;
            int vol = 0;
            vol = Convert.ToInt32(textBoxBasketNumber.Text);
            bool bMarketOrder = true;
            int orderCount = 0;
            double orderAmt = 0;

            int level = 4;
            if (checkBoxGuosenHedge1.Checked)
            {
                foreach (DataRow row in dtPosition.Rows)
                {
                    quantity = vol * Convert.ToInt32(row["Position"]);
                    if (quantity == 0)
                        continue;
                    ticker = Convert.ToString(row["Ticker"]);
                    ticker = ticker.Substring(0, 6);
                    //  price = Convert.ToDouble(row["Price"]); //暂不读取价格信息，全部采用市价单

                    FIXApi.OrderBookEntry entry = new OrderBookEntry();

                    entry.type = _orderType;
                    entry.ticker = ticker;
                    entry.quantityListed = quantity;
                    entry.priceListed = price;
                    entry.bMarket = bMarketOrder;

                    orderCount++;
                    orderAmt += (quantity * price);

                    if (tc != null)
                    {

                        entry.orderTime = Convert.ToInt32(DateTime.Now.ToString("Hmmss"));
                        entry.orderDate = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd"));
                        entry.Account = Convert.ToString(row["Account"]);

                        //模拟交易时使用现金买
                        if (Convert.ToString(entry.type).Contains("Buy"))
                        {
                            entry.type = OrderType.CashBuy;
                            entry.action = (OrderAction)Enum.Parse(typeof(OrderAction), "Buy");
                        }
                        else
                        {
                            entry.type = OrderType.CashSell;
                            entry.action = (OrderAction)Enum.Parse(typeof(OrderAction), "Sell");
                        }
                        //价格
                        if (radioButtonMarketOrder.Checked)
                        {
                            entry.bMarket = true;
                            entry.priceListed = 0;
                        }
                        else if (radioButtonLimitOrder.Checked)
                        {
                            entry.bMarket = false;
                            if (dataDict.ContainsKey(ticker) && radioButtonBuy.Checked)
                                entry.priceListed = Convert.ToDouble(dataDict[ticker].AskPrices[level]);
                            else if (dataDict.ContainsKey(ticker) && radioButtonSell.Checked)
                                entry.priceListed = Convert.ToDouble(dataDict[ticker].BidPrices[level]);
                            else
                                entry.priceListed = 0;

                        }

                        entry.strategies = (TradingStrategies)Enum.Parse(typeof(TradingStrategies), Convert.ToString(row["Strategy"]));

                        // Thread orderThread = new Thread(new ParameterizedThreadStart(tc.SendOrder));
                        Thread orderThread = new Thread(new ParameterizedThreadStart(ec.OrderRouter));
                        orderThread.IsBackground = true;
                        orderThread.Start(entry);
                        Thread.Sleep(100);

                    }
                }

            }
            else if (checkBoxHY.Checked)
            {
                //每次批量下单股票数量
                int groupCount = 2;
                //批量下单次数
                int groupnum = dtPosition.Rows.Count / groupCount;
                //剩余数量
                int subCount = dtPosition.Rows.Count - groupnum * groupCount;


                for (int i = 0; i < groupnum + 1; i++)
                {
                    int count = 0;
                    //剩余下单数量大于等于
                    if (dtPosition.Rows.Count - orderCount >= groupCount)
                        count = groupCount;
                    else
                        count = dtPosition.Rows.Count - orderCount;

                    if (count == 0)
                        continue;

                    int[] CategoryList = new int[count];
                    int[] PriceTypeList = new int[count];
                    string[] GddmList = new string[count];
                    string[] ZqdmList = new string[count];
                    float[] PriceList = new float[count];
                    int[] QuantityList = new int[count];


                    string[] Results = new string[ZqdmList.Length];
                    string[] ErrInfos = new string[ZqdmList.Length];
                    IntPtr[] ErrInfoPtr = new IntPtr[ZqdmList.Length];
                    IntPtr[] ResultPtr = new IntPtr[ZqdmList.Length];

                    for (int j = 0; j < ZqdmList.Length; j++)
                    {
                        ResultPtr[j] = Marshal.AllocHGlobal(1024 * 1024);
                        ErrInfoPtr[j] = Marshal.AllocHGlobal(256);
                    }


                    for (int j = 0; j < count; j++)
                    {

                        DataRow row = dtPosition.Rows[i * groupCount + j];
                        quantity = vol * Convert.ToInt32(row["Position"]);
                        ticker = Convert.ToString(row["Ticker"]);
                        ticker = ticker.Substring(0, 6);
                        if (radioButtonBuy.Checked)
                            CategoryList[j] = 0;//第i个单的类别,0为买,1为卖
                        else if (radioButtonSell.Checked)
                            CategoryList[j] = 1;

                        PriceTypeList[j] = 0;//第i个单的报价方式

                        if (ticker.Substring(0, 1) == "0" || ticker.Substring(0, 1) == "3")
                            GddmList[i] = "0899094024";//第i个单的股东代码，深圳
                        else
                            GddmList[j] = "B880368732";//第i个单的股东代码，上海

                        ZqdmList[j] = ticker;//第i个单的证券代码
                        //还得输入价格
                        //PriceList[j] = 98.0f;//第i个单的价格
                        if (dataDict.ContainsKey(ticker) && radioButtonBuy.Checked)
                            PriceList[j] = (float)Convert.ToDouble(dataDict[ticker].AskPrices[level]);
                        else if (dataDict.ContainsKey(ticker) && radioButtonSell.Checked)
                            PriceList[j] = (float)Convert.ToDouble(dataDict[ticker].BidPrices[level]);
                        else
                            PriceList[j] = 0;
                        QuantityList[j] = quantity;//第i个单的数量


                        orderCount++;
                        orderAmt += (quantity * price);
                    }
                    TDXTrade.SendOrders(tdx.ClientID, CategoryList, PriceTypeList, GddmList, ZqdmList, PriceList, QuantityList, count, ResultPtr, ErrInfoPtr);
                    for (int j = 0; j < ZqdmList.Length; j++)
                    {
                        Results[j] = Marshal.PtrToStringAnsi(ResultPtr[j]);
                        ErrInfos[j] = Marshal.PtrToStringAnsi(ErrInfoPtr[j]);
                        Marshal.FreeHGlobal(ResultPtr[j]);
                        Marshal.FreeHGlobal(ErrInfoPtr[j]);
                    }
                }
            }

            orderAmt *= vol;

            textBoxCiccLog1.Text = "下单完成。 篮子数：" + vol.ToString() + " 总笔数：" + orderCount.ToString() + " 总金额： " + orderAmt.ToString();

            //下单篮子数写入数据库
            //string Strategy = Convert.ToString(comboBoxIfArb.Text);
            //Int32 num = Convert.ToInt32(textBoxBasketNumber.Text);
            //string direction;
            //if (radioButtonBuy.Checked)
            //    direction = "Buy";
            //else
            //    direction = "Sell";


            //List<string> accountList = new List<string>();
            //for (int i = 0; i < dtPosition.Rows.Count; i++)
            //{
            //    string account = Convert.ToString(dtPosition.Rows[i]["Account"]);
            //    if (accountList.IndexOf(account) < 0)
            //    {
            //        accountList.Add(account);
            //        //修改篮子数，此次几个帐号开仓就得调用几次此函数
            //        OpenBasektUpdate(account, Strategy, direction, num);
            //    }
            //}
        }

  
        private void StockArbi_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!(tc == null))
            {
                if (!(tc.OrderBook.Count==0))
                tc.OrderBookUpdate(MF.conn);
            }
          
           // this.Close();          
           this.Dispose();
       

           
            
           
        }
        public void OpenBasektUpdate(string account, string Strategy, string direction, Int32 num)
        {

            DataTable dt = new DataTable();
            SqlDataAdapter SDA = new SqlDataAdapter(" select * from OpenBasket", MF.conn);
            //从数据库读取数据
            SDA.Fill(dt);
            dt.PrimaryKey = new DataColumn[] { dt.Columns["Account"], dt.Columns["Strategy"] };

            if (dt.Rows.Contains(new object[] { Convert.ToString(account), Convert.ToString(Strategy) }))
            {
                DataRow dr = dt.Rows.Find(new object[] { account, Strategy });
                if (direction == "Buy")
                    dr["TargetNumIF"] = Convert.ToInt32(dr["TargetNumIF"]) + num;
                else
                    dr["TargetNumIF"] = Convert.ToInt32(dr["TargetNumIF"]) - num;
            }
            else
            {
                DataRow dr = dt.NewRow();
                dr["Account"] = account;
                dr["Strategy"] = Strategy;
                dr["TotalMarketValue"] = 0;
                dr["BasketValue"] = 0;
                dr["TargetNumIF"] = num;
                dr["OpenNumIF"] = 0;
                dt.Rows.Add(dr);
            }


            SqlCommandBuilder scb = new SqlCommandBuilder(SDA);
            SDA.Update(dt);
            dt.AcceptChanges();

        }

  

     
    }
}
