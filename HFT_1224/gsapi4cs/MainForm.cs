using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TDFAPI;
using FIXApi;
using System.Threading;
using System.Reflection;
using System.IO;
using IronPython;
using Microsoft.Scripting.Hosting;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Runtime.InteropServices;


namespace HFT
{
    public partial class MainForm : Form
    {
        //只要窗体存在，以下实例就一直存在
        public Dictionary<string, SingleStockData> dataDict;

        public DataClass dc;
        public FIXTrade tc;
        public EngineClass ec;

        public String strDataLog = "";
        public bool bKeepLog;
        TDFOpenSetting openSettings;
        TDFERRNO nOpenRet;
        string subScriptionList;
        string connStr;

        bool bLoggedon;
        string strLogLogon = "";

        private int order_count;
        private int log_count;
        private int hold_count;

        string strCookie1, strCookie2;

        public DataTable dtPosition = new DataTable();
        public Dictionary<string, FIXTrade> AccountDic;
        public SqlConnection conn;

        public TDXTrade tdx;
       
        public MainForm()
        {
            InitializeComponent();           
        }


        private void MainForm_Load(object sender, EventArgs e)
        {

            bLoggedon = false;
            bKeepLog = false;

            order_count = 0;
            log_count = 0;
            hold_count = 0;
            strCookie1 = ""; strCookie2 = "";

            openSettings = new TDFOpenSetting()
            {
                Ip = System.Configuration.ConfigurationManager.AppSettings["IP"],                                    //服务器Ip
                Port = System.Configuration.ConfigurationManager.AppSettings["Port"],                                //服务器端口
                Username = System.Configuration.ConfigurationManager.AppSettings["Username"],                        //服务器用户名
                Password = System.Configuration.ConfigurationManager.AppSettings["Password"],                        //服务器密码
                Subscriptions = System.Configuration.ConfigurationManager.AppSettings["SubScriptions"],              //订阅列表，以 ; 分割的代码列表，例如:if1406.cf;if1403.cf；如果置为空，则全市场订阅
                Markets = System.Configuration.ConfigurationManager.AppSettings["Markets"],                          //市场列表，以 ; 分割，例如: sh;sz;cf;shf;czc;dce
                ReconnectCount = uint.Parse(System.Configuration.ConfigurationManager.AppSettings["ReconnectCount"]),//当连接断开时重连次数，断开重连在TDFDataSource.Connect成功之后才有效
                ReconnectGap = uint.Parse(System.Configuration.ConfigurationManager.AppSettings["ReconnectGap"]),    //重连间隔秒数
                ConnectionID = uint.Parse(System.Configuration.ConfigurationManager.AppSettings["ConnectionID"]),    //连接ID，标识某个Open调用，跟回调消息中TDFMSG结构nConnectionID字段相同
                Date = uint.Parse(System.Configuration.ConfigurationManager.AppSettings["Date"]),                    //请求的日期，格式YYMMDD，为0则请求今天
                Time = (uint)int.Parse(System.Configuration.ConfigurationManager.AppSettings["Time"]),               //请求的时间，格式HHMMSS，为0则请求实时行情，为(uint)-1从头请求
                TypeFlags = (uint)int.Parse(System.Configuration.ConfigurationManager.AppSettings["TypeFlags"])      //unchecked((uint)DataTypeFlag.DATA_TYPE_ALL);   //为0请求所有品种，或者取值为DataTypeFlag中多种类别，比如DATA_TYPE_MARKET | DATA_TYPE_TRANSACTION
            };

            subScriptionList = openSettings.Subscriptions;

            // connStr = "server=192.168.0.100,1433;database=HedgeHogDB;User ID=ct;Password=djdl@1633;Connection Timeout=30";
            connStr = "server=.;database=HedgeHogDB;integrated security=SSPI";

            dataDict = new Dictionary<string, SingleStockData>();

            tdx = new TDXTrade();
        }
       
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
           
            if (!(dc == null))
                dc.Dispose();
            if (!(tc == null))
            {
                if (!(tc.OrderBook.Count == 0))
                tc.OrderBookUpdate(conn);
                // PositionUpdate();
                if (tc.isLoggedOn)
                {
                    tc.Logout();
                    while (tc.isLoggedOn)
                        System.Threading.Thread.Sleep(100);
                }
            }
            Application.Exit();
            this.Dispose();
        }

 
        private void OnExcuteReport(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(OnExcuteReport), null);
            }
            else
            {
                labelTradeException.Text = tc.msg;

                // 根据FIX类的结果，刷新界面数据
                if (G_TrdSerial.Rows.Count > 0 && order_count < G_TrdSerial.Rows.Count)
                {
                    order_count = G_TrdSerial.Rows.Count;
                    G_TrdSerial.CurrentCell = G_TrdSerial.Rows[G_TrdSerial.Rows.Count - 1].Cells[0];
                }
                G_TrdSerial.Invalidate();
                PropertyInfo pi = G_TrdSerial.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                pi.SetValue(G_TrdSerial, true, null);

                if (G_Log.Rows.Count > 0 && log_count < G_Log.Rows.Count)
                {
                    log_count = G_Log.Rows.Count;
                    G_Log.CurrentCell = G_Log.Rows[G_Log.Rows.Count - 1].Cells[0];
                }
                G_Log.Invalidate();
                pi = G_Log.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                pi.SetValue(G_Log, true, null);
            }
        }

        private void OnQueryFundReport(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(OnQueryFundReport), null);
            }
            else
            {
                labelTradeException.Text = tc.msg;

                if (G_Log.Rows.Count > 0 && log_count < G_Log.Rows.Count)
                {
                    log_count = G_Log.Rows.Count;
                    G_Log.CurrentCell = G_Log.Rows[G_Log.Rows.Count - 1].Cells[0];
                }
                G_Log.Invalidate();
                PropertyInfo pi = G_Log.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                pi.SetValue(G_Log, true, null);

                //T_FundBal.Text = tc.dt_fund.Rows[0][0].ToString();
                //T_AvlFundBal.Text = tc.dt_fund.Rows[0][1].ToString();
                //T_TotalFundBal.Text = tc.dt_fund.Rows[0][2].ToString();
                //T_Fundasset.Text = tc.dt_fund.Rows[0][3].ToString();
                //T_MktVal.Text = tc.dt_fund.Rows[0][4].ToString();
                //T_FundBuyFrz.Text = tc.dt_fund.Rows[0][5].ToString();
            }
        }

        private void OnQueryHoldReport(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(OnQueryHoldReport), null);
            }
            else
            {
                labelTradeException.Text = tc.msg;

                if (G_Log.Rows.Count > 0 && log_count < G_Log.Rows.Count)
                {
                    log_count = G_Log.Rows.Count;
                    G_Log.CurrentCell = G_Log.Rows[G_Log.Rows.Count - 1].Cells[0];
                }
                G_Log.Invalidate();
                PropertyInfo pi = G_Log.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                pi.SetValue(G_Log, true, null);

                if (G_HoldSerial.Rows.Count > 0 && hold_count < G_HoldSerial.Rows.Count)
                {
                    hold_count = G_HoldSerial.Rows.Count;
                    G_HoldSerial.CurrentCell = G_HoldSerial.Rows[G_HoldSerial.Rows.Count - 1].Cells[0];
                }
                G_HoldSerial.Invalidate();
                pi = G_HoldSerial.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                pi.SetValue(G_HoldSerial, true, null);

                //B_QueryHold.Enabled = fix.b_lastrpt;
                //*********************************避免重复发送查询指令***********************************************************
                //BTN_QueryCreditHold.Enabled = fix.b_lastrpt;
                //****************************************************************************************************************
            }
        }

        private void btnDataStart_Click(object sender, EventArgs e)
        {
            conn = new SqlConnection(connStr);
            conn.Open();

            //// TODO:  这行代码将数据加载到表“hedgeHogDBDataSet.v_Strategy”中。您可以根据需要移动或删除它。
            this.v_StrategyTableAdapter.Fill(this.hedgeHogDBDataSet.v_Strategy);

            logOnAll();
            

            bool isConnected = tdx.TDXOpen();
            if (isConnected)
                textBoxTDXLog.Text = "通达信已登录";
          
        }

        private void logOnAll()
        {

            #region 初始化DataClass，调用Open，登陆服务器。初始化过程到此结束，对数据的操作，请到TDFSourceImp的两个虚函数里进行

            dc = new DataClass(openSettings);

            if (rbBackTest.Checked)
                dc.mode = DataMode.BackTest;
            else
                dc.mode = DataMode.RealTime;

            dc.WriteLog += dc_WriteLog;
            dc.EvMarketClose += dc_EvMarketClose;

            tbDataLog.Text = DateTime.Now.ToLongTimeString() + "数据读取开始";

            dc.dataDict = dataDict;
            dc.intDate = (int)openSettings.Date;
          

            DataClass.connStr = connStr;

            //Thread thDcDB = new Thread(new ThreadStart(dc.LoadFromDatabase));
            //thDcDB.Start();

            Thread thDc = new Thread(new ThreadStart(dc.LoadFromTDF));
            thDc.Start();

            #endregion

            #region 初始化TradeClass

            string strFundid = "", strCreditFundid = "", strPwd = "";
            int encrypt_type = 0;
            bool bResetSeq = true; //交易日变更后第一次登陆必须设为true，以后必须设为false。生产环境和测试环境都需要执行一次


            //账户合集字典
            Dictionary<string, FIXTrade> AccountDic = new Dictionary<string, FIXTrade>();
            if (rbProd.Checked)
            {
                //tc = new FIXTrade("initiator_prod.cfg");               
                //strFundid = "110099100116";              
                //strPwd = "143279";
                //encrypt_type = 2; //DES_ECB, 101为Blowfish
                //tc.mode = TradeMode.Production;              
                string filePath = System.Environment.CurrentDirectory;
                DirectoryInfo di = new DirectoryInfo(filePath);
                //读取目录下所有的cfg文件
                foreach (FileInfo info in di.GetFiles("*.cfg"))
                {
                    string tmpName = info.ToString();
                    string order_cfg = info.ToString();
                    //剔除非实际账户的cfg文件
                    if (order_cfg.Contains("prod"))
                    {
                        string[] strvalue = order_cfg.Split('_');
                        string[] strname = strvalue[2].Split('.');
                        string tcname = strname[0];
                        FIXTrade tmpTrade = new FIXTrade(order_cfg);
                        tmpTrade.account = tcname;
                        AccountDic.Add(tcname, tmpTrade);
                    }
                }
            }
            else if (rbDebug.Checked)
            {

                //strFundid = "110000000572";
                //strCreditFundid = "110050000572";
                //strPwd = "135790";
                //encrypt_type = 0;
                //tc.mode = TradeMode.Debug;
                string filePath = System.Environment.CurrentDirectory;
                DirectoryInfo di = new DirectoryInfo(filePath);
                //读取目录下所有的cfg文件
                foreach (FileInfo info in di.GetFiles("*.cfg"))
                {
                    string tmpName = info.ToString();
                    string order_cfg = info.ToString();
                    //剔除非实际账户的cfg文件
                    if (order_cfg.Contains("debug"))
                    {
                        string[] strvalue = order_cfg.Split('_');
                        string[] strname = strvalue[2].Split('.');
                        string tcname = strname[0];
                        FIXTrade tmpTrade = new FIXTrade(order_cfg);
                        tmpTrade.account = tcname;
                        AccountDic.Add(tcname, tmpTrade);
                    }
                }

            }
            else if (rbBackTest.Checked)
            {

                tc = new FIXTrade("");
                tc.mode = TradeMode.Backtest;
            }
            else
                throw new Exception("TradeClass 生成错误");

          // foreach (string tmp in AccountDic.Keys)
         //  {
           //由于是多账户，选择其中一个账户登录
            if (rbProd.Checked)
            {
                tc = AccountDic["dj1"];
            }
             else if (rbDebug.Checked)
            {
                tc = AccountDic["mn1"];
             }
            else
            {
                tc = new FIXTrade("");
            }

                if (tc != null)
                {
                    tc.dataDict = dataDict;
                    tc.ExcuteReport += new EventHandler(OnExcuteReport);
                    tc.QueryFundReport += new EventHandler(OnQueryFundReport);
                    tc.QueryHoldReport += new EventHandler(OnQueryHoldReport);
                    
                  

                    G_TrdSerial.DataSource = tc.dt_order;
                    G_Log.DataSource = tc.dt_log;
                   
                    G_PnL.DataSource = tc.dt_PnL;
                    tc.WriteLog += tc_WriteLog;//等效于 tc.WriteLog += new EventHandler(tc_WriteLog);
                    //多线程登录
                    if ((tc.mode == TradeMode.Debug) || (tc.mode == TradeMode.Production))
                    //tc.Logon(strFundid, strCreditFundid, strPwd, encrypt_type, bResetSeq);                     
                    {
                        Thread LogonThread = new Thread(new ParameterizedThreadStart(tc.Logon));
                        LogonThread.Start(tc);
                    }
                }
           // }
            #endregion

            #region 初始化EngineClass
            ec = new EngineClass();
            ec.ConnectToData(dc);
            ec.ConnectToTrade(tc);
            ec.WriteLog += ec_WriteLog;
            ec.dataDict = dataDict;

            #endregion

        }


        private void btTradeSendOrder_Click(object sender, EventArgs e)
        {
            string ticker = textBoxTradeTicker.Text;
            string quantity = textBoxTradeQuantity.Text;
            string price = textBoxTradePrice.Text;
            int typeSelected = comboBoxTradeOrderType.SelectedIndex;
            string bufferNotUsed;

            OrderType orderType = 0;

            switch (typeSelected)
            {
                case 0: orderType = OrderType.CashBuy; break;
                case 1: orderType = OrderType.CashSell; break;
                case 2: orderType = OrderType.CreditBuy; break;
                case 3: orderType = OrderType.CreditSell; break;
                case 4: orderType = OrderType.CreditMarginBuy; break;
                case 5: orderType = OrderType.CreditMarginSell; break;
                case 6: orderType = OrderType.CreditBuyToCover; break;
                case 7: orderType = OrderType.CreditSellToCover; break;
                default: MessageBox.Show("Unknown OrderType"); break;
            }

            if (tc != null)
            //  tc.SendOrder(orderType, ticker, quantity, price, checkBoxTradeMarketOrder.Checked, out bufferNotUsed);
            //
            {
                FIXApi.OrderBookEntry entry = new OrderBookEntry();
                entry.strategies = TradingStrategies.other_other_other;
                entry.type = orderType;
                if (Convert.ToString(entry.type).Contains("Buy"))
                    entry.action = OrderAction.Buy;
                else
                    entry.action = OrderAction.Sell;
                entry.Account = "dj1";
                entry.ticker = ticker;
                entry.quantityListed = Convert.ToInt32(quantity);
                entry.priceListed = Convert.ToDouble (price);
                entry.orderTime = Convert.ToInt32(DateTime.Now.ToString("Hmmss")); //SellTime
                entry.orderDate = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd"));


                ec.OrderRouter(entry);
            }


            //多账户下单，不同的tc属于不同的账户
            //foreach (string  tmp in AccountDic.keys)
            //{
            //    tc = tmp.tc;
            //    if (tmp.strCreditFundid == "")
            //        orderType = OrderType.CashBuy;
            //    else
            //        orderType = OrderType.CreditBuy;
            //    if (tc != null)
            //        tc.SendOrder(orderType, ticker, quantity, price, checkBoxTradeMarketOrder.Checked, out bufferNotUsed);
            //}
           
        }


        # region 行情测试区
        private void btnDataLoadList_Click(object sender, EventArgs e)
        {
            if ((dc != null) && (nOpenRet == TDFERRNO.TDF_ERR_SUCCESS))
            {
                string tickerListStr = dc.LoadDataDict();
                //dc.UpdateHistData(tickerListStr);
                dc.SetSubscription(tickerListStr, SubscriptionType.SUBSCRIPTION_SET);
            }
            /*
            if ((dc != null) && (nOpenRet == TDFERRNO.TDF_ERR_SUCCESS))
            {
                string f = "D:\\Data\\ReadlineData\\LoadTickerList.txt";
                string tickerList = "";

                StreamReader sr = File.OpenText(f);
                string nextLine;
                while ((nextLine = sr.ReadLine()) != null)
                {
                    if (nextLine.StartsWith("60") || nextLine.StartsWith("51"))
                    {
                        nextLine = nextLine+".sh";   // 上海
                    }
                    else if (nextLine.StartsWith("00") || nextLine.StartsWith("30") || nextLine.StartsWith("15"))
                    {
                        nextLine = nextLine + ".sz";    // 深圳
                    }

                    tickerList = tickerList + nextLine + ";";
                }
                sr.Close();

                dc.SetSubscription(tickerList, SubscriptionType.SUBSCRIPTION_SET);
                dc.UpdateHistData(tickerList);
            }
            */
        }

        private void btnDataAdd_Click(object sender, EventArgs e)
        {
            string str = textBoxDataAddTicker.Text;
            if ((dc != null) && (nOpenRet == TDFERRNO.TDF_ERR_SUCCESS))
            {
                dc.SetSubscription(str, SubscriptionType.SUBSCRIPTION_ADD);
            }
        }

        private void btnDataReset_Click(object sender, EventArgs e)
        {
            if ((dc != null) && (nOpenRet == TDFERRNO.TDF_ERR_SUCCESS))
            {
                dc.SetSubscription("", SubscriptionType.SUBSCRIPTION_FULL);
                //dc.HistoricalData = null;
            }
        }

        # endregion

        #region 回调函数区
        void ec_WriteLog(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(ec_WriteLog), null);
            }
            else
            {
                tbEngineLog.Clear();
                lock (ec.strListLog)
                {
                    foreach (string str in ec.strListLog)
                        tbEngineLog.AppendText(str + "\r\n");
                }

            }
        }

        void tc_WriteLog(object sender, EventArgs e)
        {

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(tc_WriteLog), null);
            }
            else
            {
                labelTradeException.Text = tc.msg;
                lock (tc.strListLog)
                {
                    foreach (string str in tc.strListLog)
                        tbTradeLog.AppendText(str + "\r\n");
                }

                textBoxTradeLogon.Text = tc.isLoggedOn ? "已登录" : "断开";

                btnLogOn.Enabled = !tc.isLoggedOn;

            }

        }

        void dc_WriteLog(object sender, EventArgs e)
        {

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(dc_WriteLog), null);
            }
            else
            {
                if (dc.nOpenRet == TDFERRNO.TDF_ERR_SUCCESS)
                {
                    textBoxDataLogon.Text = "已登录";
                    dc.RecvDataReport += new EventHandler(dc_RecvDataReport);
                    strDataLog = "connect success!\n";
                }
                else
                {
                    //这里判断错误代码，进行对应的操作，对于 TDF_ERR_NETWORK_ERROR，用户可以选择重连
                    textBoxDataLogon.Text = "断开";
                    strDataLog = "open returned: " + nOpenRet.ToString();
                    //return;
                }

                tbDataLog.Text = tbDataLog.Text + "\r\n" + DateTime.Now.ToLongTimeString() + "数据读取结束";
            }

        }


        void dc_RecvDataReport(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(dc_RecvDataReport), null);
            }
            else
            {
                lock (dc.HistoricalData)
                {
                    foreach (StockData sd in dc.HistoricalData.Values)
                    {
                        int lastIndex = sd.currentPrices.LastTimeIndex;
                        //if ((lastIndex >= 0)&&(sd.currentPrices.tickData.Count==lastIndex))
                        if (lastIndex >= 0)
                        {
                            TickData td = sd.currentPrices.tickData[lastIndex];

                            tbDataLog.Text = sd.Ticker + " " +
                                DateTime.Now.ToString() + " " +
                                td.TimeStamp.ToString() + " " +
                                td.LastPrice.ToString() + " " +
                                td.Volume.ToString() + " " +
                                td.TotalVolume.ToString() + " " + "\r\n"
                                + tbDataLog.Text;
                        }
                    }
                }
                //tbDataLog.Text = dc.logStr +"\r\n";
            }
        }


        void dc_EvMarketClose(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(dc_EvMarketClose), null);
            }
            else
            {
                tbDataLog.Text = tbDataLog.Text + "\r\n" +
                    DateTime.Now.ToLongTimeString() + " 数据读取完毕，收市" + "\r\n" +
                    "共读取股票只数：" + "\r\n" + dc.HistoricalData.Count.ToString();
            }
        }
        # endregion

        # region 交易界面函数



        # endregion


        private void buttonEngineTest_Click(object sender, EventArgs e)
        {
            //dc.LoadTickDataAggFromDatabase();
            //dc.LoadTickDataFromDatabase();

            if (ec == null) MessageBox.Show("Engine not initialized yet");
            else
            {
                if (ec.bRunning == false)
                {
                    ec.bDebug = false;
                    ec.bRunning = true;

                    Thread th = new Thread(new ThreadStart(ec.Run));
                    th.Start();
                }
                else
                {
                    ec.bRunning = false;
                }
            }
            //fishing strategy设置
            /*
            string strFundid = "", strCreditFundid = "", strPwd = "";
            int encrypt_type = 0;
            bool bResetSeq = true; //交易日变更后第一次登陆必须设为true，以后必须设为false。生产环境和测试环境都需要执行一次

            if (rbProd.Checked)
            {
                tc = new FIXTrade("initiator_prod.cfg");
                strFundid = "110099100054";
                strCreditFundid = "110099160054";
                //strFundid = "110002378162";
                //strCreditFundid = "110062378162";
                strPwd = "190715";
                encrypt_type = 2; //DES_ECB, 101为Blowfish
                tc.mode = TradeMode.Production;
            }
            else if (rbDebug.Checked)
            {
                tc = new FIXTrade("initiator_debug.cfg");
                strFundid = "110000000572";
                strCreditFundid = "110050000572";
                strPwd = "135790";
                encrypt_type = 0;
                tc.mode = TradeMode.Debug;
            }
            else if (rbBackTest.Checked)
            {

                tc = new FIXTrade("");
                tc.mode = TradeMode.Backtest;
            }
            else
                throw new Exception("TradeClass 生成错误");

            if (tc != null)
            {
                tc.dataDict = dataDict;

                tc.ExcuteReport += new EventHandler(OnExcuteReport);
                tc.QueryFundReport += new EventHandler(OnQueryFundReport);
                tc.QueryHoldReport += new EventHandler(OnQueryHoldReport);

                G_TrdSerial.DataSource = tc.dt_order;
                G_Log.DataSource = tc.dt_log;
                G_HoldSerial.DataSource = tc.dt_hold;
                G_PnL.DataSource = tc.dt_PnL;
                tc.WriteLog += tc_WriteLog;//等效于 tc.WriteLog += new EventHandler(tc_WriteLog);

                if ((tc.mode == TradeMode.Debug) || (tc.mode == TradeMode.Production))
                    tc.Logon(strFundid, strCreditFundid, strPwd, encrypt_type, bResetSeq);

            }

            //String connStr = "server=192.168.0.100,1433;database=HedgeHogDB;User ID=ct;Password=djdl@1633;Connection Timeout=30";
            string connStr = "server=.;database=HedgeHogDB;integrated security=SSPI";


            dc = new DataClass(openSettings);
            dc.connStr = connStr;
            dc.dataDict = dataDict;
            string watchStrList = dc.LoadDataDict();
            //dc.UpdateHistData(watchStrList);

            dc.tc = tc;

            if (rbBackTest.Checked)
                dc.mode = DataMode.BackTest;
            else
                dc.mode = DataMode.RealTime;

            dc.WriteLog += dc_WriteLog;
            dc.EvMarketClose += dc_EvMarketClose;

            tbDataLog.Text = DateTime.Now.ToLongTimeString() + "数据读取开始";

            Thread thDc = new Thread(new ThreadStart(dc.LoadFromTDF));
            thDc.Start();




            ec = new EngineClass();
            ec.ConnectToData(dc);
            ec.ConnectToTrade(tc);
            ec.WriteLog += ec_WriteLog;
            ec.dataDict = dataDict;
            //暂时不用EngineClass，让dc和tc直接交互
            //Thread thEc = new Thread(new ThreadStart(ec.RunStrategies));
            //thEc.Start();
            */
        }

        private void buttonLoadRunList_Click(object sender, EventArgs e)
        {
            StockTrend stForm = new StockTrend();
            stForm.dataDict = this.dataDict;
            stForm.tc = this.tc;
            stForm.dc = this.dc;
            stForm.ec = this.ec;
            stForm.MF = this;
            stForm.tdx = this.tdx;
            stForm.Show();
        }

        private void buttonEngineRun_Click(object sender, EventArgs e)
        {
            string tickerListStr = dc.LoadDataDict();
            dc.SetSubscription(tickerListStr, SubscriptionType.SUBSCRIPTION_SET);

            while (dataDict.Count < 1)
                Thread.Sleep(100);

            Thread th = new Thread(new ThreadStart(ec.Run));
            th.Start();
        }

        private void buttonLock_Click(object sender, EventArgs e)
        {

            StockArbi stForm = new StockArbi();
            stForm.dataDict = this.dataDict;
            stForm.tc = this.tc;
            stForm.dc = this.dc;
            stForm.ec = this.ec;
            stForm.MF = this;
            stForm.tdx = this.tdx;
           

            stForm.Show();

        }



        //private void buttonIFArbRun_Click(object sender, EventArgs e)
        //{
        //    textBoxCiccLog1.Text = "";
        //    string indexFileName = "";

        //    //
        //    TradingStrategies strategy = TradingStrategies.alpha_arbitrage_HS300 ;



        //    switch (comboBoxIfArb.SelectedIndex)
        //    {
        //        case 0: indexFileName = "HS300"; strategy = TradingStrategies.alpha_arbitrage_HS300; break;
        //        case 1: indexFileName = "SZ50"; strategy = TradingStrategies.alpha_arbitrage_SZ50; break;
        //        case 2: indexFileName = "ZZ500"; strategy = TradingStrategies.alpha_arbitrage_ZZ500; break;
        //        default: MessageBox.Show("Please select a index"); break;
        //    }

        //    if (((radioButtonBuy.Checked) && (radioButtonCreditSell.Checked))
        //        || ((radioButtonSell.Checked) && (radioButtonCreditBuy.Checked))
        //        || ((radioButtonSell.Checked) && (radioButtonCreditMarginBuy.Checked)))
        //    {
        //        MessageBox.Show("组合选择错误");
        //        return;
        //    }

        //    //读取hs300excel文件,得出个股权重
        //    string Filename = "";
        //    if (radioButtonBuy.Checked)
        //        Filename = "C:\\IFArb\\" + indexFileName + "BuyList.csv";
        //    else if (radioButtonCreditSell.Checked)
        //        Filename = "C:\\IFArb\\" + indexFileName + "SellList.csv";
        //    else
        //    {
        //        MessageBox.Show("未选择买入还是卖出");
        //        return;
        //    }

        //    DataTable HS300Data = new DataTable();
        //    HS300Data.Columns.Add("Ticker"); //column 1
        //    HS300Data.Columns.Add("SecurityName"); //column 2
        //    HS300Data.Columns.Add("Weight"); //column 3
        //    HS300Data.Columns.Add("Price"); //column 4

        //    StreamReader sr = new StreamReader(Filename, UnicodeEncoding.GetEncoding("GB2312"));
        //    sr.ReadLine();
        //    while (true)
        //    {
        //        string strData = sr.ReadLine();
        //        if (!String.IsNullOrEmpty(strData))
        //        {
        //            String[] strValue = strData.Split(',');
        //            DataRow dr = HS300Data.NewRow();
        //            dr["Ticker"] = strValue[0];
        //            dr["SecurityName"] = strValue[1];
        //            dr["Weight"] = strValue[2];
        //         //   dr["Price"] = strValue[3]; //暂不读取价格信息，全部采用市价单

        //            //剔除数量为0的股票
        //            if (Convert.ToDouble(strValue[2]) > 0)
        //            { HS300Data.Rows.Add(dr); }
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }

        //    int quantity = 0; string ticker = ""; double price = 0;
        //    int vol = 0;
        //    vol = Convert.ToInt32(textBoxBasketNumber.Text);

        //    bool bMarketOrder = true;

        //    OrderType _orderType;

        //    if (radioButtonCreditBuy.Checked)
        //        _orderType = OrderType.CreditBuy;
        //    else if (radioButtonCreditMarginBuy.Checked)
        //        _orderType = OrderType.CreditMarginBuy;
        //    else if (radioButtonCreditSell.Checked)
        //        _orderType = OrderType.CreditSell;
        //    else
        //    {
        //        MessageBox.Show("未选择指令方式");
        //        return;
        //    }

        //    int orderCount = 0;
        //    double orderAmt = 0;

        //    foreach (DataRow row in HS300Data.Rows)
        //    {
        //        //if (count++ > 0) break;

        //        quantity = vol * Convert.ToInt32(row["Weight"]);
        //        ticker = Convert.ToString(row["Ticker"]);
        //        ticker = ticker.Substring(0, 6);
        //      //  price = Convert.ToDouble(row["Price"]); //暂不读取价格信息，全部采用市价单

        //        FIXApi.OrderBookEntry entry = new OrderBookEntry();

        //        entry.type = _orderType;

        //        entry.ticker = ticker;
        //        entry.quantityListed = quantity;
        //        entry.priceListed = price;
        //        entry.bMarket = bMarketOrder;

        //        orderCount++;
        //        orderAmt += (quantity * price);

        //        if (tc != null)
        //        {
                   
        //            entry.bMarket = true;
        //            entry.orderTime = Convert.ToInt32(DateTime.Now.ToString("Hmmss"));
        //            entry.orderDate = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd"));
        //            entry.Account = "dj1";

        //            //模拟交易时使用现金买
        //            if (Convert.ToString(entry.type).Contains("Buy"))
        //            {
        //                entry.type = OrderType.CashBuy;
        //                entry.action = (OrderAction)Enum.Parse(typeof(OrderAction), "Buy");
        //            }
        //            else
        //            {
        //                entry.type = OrderType.CashSell;
        //                entry.action = (OrderAction)Enum.Parse(typeof(OrderAction), "Sell");
                    
        //            }

        //            entry.bMarket = true;
                  
                
        //            entry.strategies = strategy;              
        //            entry.priceListed = 0;
                
                   
                    
                    
                                                 
        //           // Thread orderThread = new Thread(new ParameterizedThreadStart(tc.SendOrder));
        //            Thread orderThread = new Thread(new ParameterizedThreadStart(ec.OrderRouter));     
        //            orderThread.IsBackground = true;
        //            orderThread.Start(entry);
        //            Thread.Sleep(100);
                
        //        }
        //    }

        //    orderAmt *= vol;

        //    textBoxCiccLog1.Text = "下单完成。 篮子数："+vol.ToString()+" 总笔数：" + orderCount.ToString() + " 总金额： " + orderAmt.ToString();

        //}

        //private void btnBatchOrder_Click(object sender, EventArgs e)
        //{

        //    textBoxCiccLog1.Text = "";
        //    textBoxCiccLog2.Text = "";

        //    string indexFileName = "";
        //    switch (comboBoxIfArb.SelectedIndex)
        //    {
        //        case 0: indexFileName = "HS300"; break;
        //        case 1: indexFileName = "SZ50"; break;
        //        case 2: indexFileName = "ZZ500"; break;
        //        default: MessageBox.Show("Please select a index"); break;
        //    }


        //    string pythonDirection = "";

        //    if (radioButtonBuy.Checked) pythonDirection = "B";
        //    else if (radioButtonSell.Checked) pythonDirection = "S";
        //    else return;

        //    //读取hs300excel文件,得出个股权重
        //    string Filename = "";
        //    if (radioButtonBuy.Checked)
        //        Filename = "C:\\IFArb\\" + indexFileName + "BuyList.csv";
        //    else if (radioButtonCreditSell.Checked)
        //        Filename = "C:\\IFArb\\" + indexFileName + "SellList.csv";
        //    else
        //    {
        //        MessageBox.Show("未选择买入还是卖出");
        //        return;
        //    }

        //    DataTable HS300Data = new DataTable();
        //    HS300Data.Columns.Add("Ticker"); //column 1
        //    HS300Data.Columns.Add("SecurityName"); //column 2
        //    HS300Data.Columns.Add("Weight"); //column 3
        //    HS300Data.Columns.Add("Price"); //column 4

        //    StreamReader sr = new StreamReader(Filename, UnicodeEncoding.GetEncoding("GB2312"));
        //    sr.ReadLine();
        //    while (true)
        //    {
        //        string strData = sr.ReadLine();
        //        if (!String.IsNullOrEmpty(strData))
        //        {
        //            String[] strValue = strData.Split(',');
        //            DataRow dr = HS300Data.NewRow();
        //            dr["Ticker"] = strValue[0];
        //            dr["SecurityName"] = strValue[1];
        //            dr["Weight"] = strValue[2];
        //            dr["Price"] = strValue[3]; //暂不读取价格信息，全部采用市价单

        //            //剔除数量为0的股票
        //            if (Convert.ToDouble(strValue[2]) > 0)
        //            { HS300Data.Rows.Add(dr); }
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }

        //    int quantity = 0; string ticker = ""; double price = 0;
        //    int vol = 0;
        //    vol = Convert.ToInt32(textBoxBasketNumber.Text);

        //    bool bMarketOrder = true;

        //    int count1 = 0, count2 = 0;
        //    double amtSum1 = 0, amtSum2 = 0;

        //    foreach (DataRow row in HS300Data.Rows)
        //    {
        //        //if (count++ > 0) break;

        //        quantity = vol * Convert.ToInt32(row["Weight"]);
        //        ticker = Convert.ToString(row["Ticker"]);
        //        ticker = ticker.Substring(0, 6);
        //        price = Convert.ToDouble(row["Price"]); //暂不读取价格信息，全部采用市价单

        //        FIXApi.OrderBookEntry entry = new OrderBookEntry();

        //        entry.ticker = ticker;
        //        entry.quantityListed = quantity;
        //        entry.priceListed = price;
        //        entry.bMarket = bMarketOrder;


        //        if ((strCookie1 == "") && (strCookie2 == ""))
        //        {
        //            MessageBox.Show("No Cookies");
        //            return;
        //        }

        //        //下单程序必须放在D盘根目录，D\\SendOrder.py
        //        if (checkBoxCiccSeed1.Checked && strCookie1 != "")
        //        {

        //            PythonWrapper.PutOrder(strCookie1, "B880303376", "0899090767", entry.ticker, pythonDirection, entry.priceListed, entry.quantityListed);
        //            count1++;
        //            amtSum1 += (entry.priceListed * entry.quantityListed);

        //        }

        //        if (checkBoxCiccSeed2.Checked && strCookie2 != "")
        //        {

        //            PythonWrapper.PutOrder(strCookie2, "B880374335", "0899094261", entry.ticker, pythonDirection, entry.priceListed, entry.quantityListed);
        //            count2++;
        //            amtSum2 += (entry.priceListed * entry.quantityListed);
        //        }

        //        textBoxCiccLog1.Text = "种子基金1号下单完成。 总笔数："+count1.ToString()+" 总金额： "+amtSum1.ToString();
        //        textBoxCiccLog2.Text = "种子基金2号下单完成。 总笔数：" + count2.ToString() + " 总金额： " + amtSum2.ToString();

        //    }
        //}

        /*
        private void btnBatchOrder_Click(object sender, EventArgs e)
        {

            if (((radioButtonBuy.Checked) && (radioButtonCreditSell.Checked))
    || ((radioButtonSell.Checked) && (radioButtonCreditBuy.Checked))
    || ((radioButtonSell.Checked) && (radioButtonCreditMarginBuy.Checked)))
            {
                MessageBox.Show("组合选择错误");
                return;
            }

            if ((radioButtonBuy.Checked) && (radioButtonCreditMarginBuy.Checked))
            {
                MessageBox.Show("暂不支持融资融券的批量下单");
                return;
            }

            //读取hs300excel文件,得出个股权重
            string Filename = "";
            if (radioButtonBuy.Checked)
                Filename = "D:\\Data\\IFArb\\BuyList.csv";
            else if (radioButtonCreditSell.Checked)
                Filename = "D:\\Data\\IFArb\\SellList.csv";
            else
            {
                MessageBox.Show("未选择买入还是卖出");
                return;
            }

            Filename = "D:\\test.csv";

            DataTable HS300Data = new DataTable();
            HS300Data.Columns.Add("Ticker"); //column 1
            HS300Data.Columns.Add("SecurityName"); //column 2
            HS300Data.Columns.Add("Weight"); //column 3
            HS300Data.Columns.Add("Price"); //column 4

            StreamReader sr = new StreamReader(Filename, UnicodeEncoding.GetEncoding("GB2312"));
            sr.ReadLine();
            while (true)
            {
                string strData = sr.ReadLine();
                if (!String.IsNullOrEmpty(strData))
                {
                    String[] strValue = strData.Split(',');
                    DataRow dr = HS300Data.NewRow();
                    dr["Ticker"] = strValue[0];
                    dr["SecurityName"] = strValue[1];
                    dr["Weight"] = strValue[2];
                    dr["Price"] = strValue[3];

                    //剔除数量为0的股票
                    if (Convert.ToDouble(strValue[2]) > 0)
                    { HS300Data.Rows.Add(dr); }
                }
                else
                {
                    break;
                }
            }

            int vol = 0;
            vol = Convert.ToInt32(textBoxBasketNumber.Text);

            bool bMarketOrder = false;

            OrderType _orderType;

            if (radioButtonCreditBuy.Checked)
                _orderType = OrderType.CreditBuy;
            else if (radioButtonCreditMarginBuy.Checked)
                _orderType = OrderType.CreditMarginBuy;
            else if (radioButtonCreditSell.Checked)
                _orderType = OrderType.CreditSell;
            else
            {
                MessageBox.Show("未选择指令方式");
                return;
            }

            tc.ListOrder(HS300Data, Convert.ToDouble(vol), _orderType, bMarketOrder ? '1' : '2');//只下市价单
        }
        */
        private void btnStructFund_Click(object sender, EventArgs e)
        {
            StructFundForm stForm = new StructFundForm();
            stForm.dataDict = this.dataDict;
            stForm.tc = this.tc;
            stForm.dc = this.dc;
            stForm.ec = this.ec;

            stForm.Show();
        }

        //private void buttonCookieUpdate_Click(object sender, EventArgs e)
        //{
        //    strCookie1 = textBoxCookie1.Text;
        //    strCookie2 = textBoxCookie2.Text;

        //    if (ec == null) return;

        //    if (checkBoxCiccSeed1.Checked && strCookie1 != "")
        //    {
        //        ec.bCicc1 = true;
        //        ec.strCookie1 = strCookie1;
        //    }
        //    else 
        //    {
        //        ec.bCicc1 = false;
        //        ec.strCookie1 = "";

        //    }

        //    if (checkBoxCiccSeed2.Checked && strCookie2 != "")
        //    {
        //        ec.bCicc2 = true;
        //        ec.strCookie2 = strCookie2;
        //    }
        //    else
        //    {
        //        ec.bCicc2 = false;
        //        ec.strCookie2 = "";

        //    }


        //}



       //private void buttonCiccTest_Click(object sender, EventArgs e)
       // {
       //     if ((strCookie1 == "") && (strCookie2 == ""))
       //     {
       //         MessageBox.Show("No Cookies");
       //         return;
       //     }

       //     //下单程序必须放在D盘根目录，D\\SendOrder.py
       //     if (checkBoxCiccSeed1.Checked && strCookie1 != "")
       //     {

       //         PythonWrapper.PutOrder(strCookie1, "B880303376", "0899090767", "511990", "B", 101, 100);

       //     }

       //     if (checkBoxCiccSeed2.Checked && strCookie2 != "")
       //     {

       //         PythonWrapper.PutOrder(strCookie2, "B880374335", "0899094261", "511990", "B", 101, 100);

       //     }
       // }
        #region 查询持仓
        private void button1_Click(object sender, EventArgs e)
        {
            tc.QueryCreditHold();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            tc.QueryHold();
        }
        #endregion





 

       
      


      


        private void button3_Click_1(object sender, EventArgs e)
        {
            tc.OrderBookUpdate(conn);
            
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            PositionUpdate();
           
        }
        public void PositionUpdate()
        {

            DataTable dt = new DataTable();

            SqlDataAdapter SDA = new SqlDataAdapter("select * from Inventory", conn);
            //从数据库读取数据
            SDA.Fill(dt);


            //只需绑定今天的成交回报,利用成交回报修改持仓
            string sql = "select * from OrderBook";
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader sdr = cmd.ExecuteReader();
            while (sdr.Read())
            {
                string Account = sdr.GetString(sdr.GetOrdinal("Account"));
                string ticker = sdr.GetString(sdr.GetOrdinal("Ticker"));
                string Strategy = sdr.GetString(sdr.GetOrdinal("Strategy"));
                int num = sdr.GetInt32(sdr.GetOrdinal("QuantityExecuted"));
                string type = sdr.GetString(sdr.GetOrdinal("Type"));
                double price = sdr.GetDouble(sdr.GetOrdinal("PriceExecuted"));

                //成交量为0就跳过
                if (num == 0 || price == 0)
                    continue;
                //是否找到该策略该股票
                bool Hasrecord = false;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow dr = dt.Rows[i];
                    //在现有持仓中找到该策略该股票，修正股票数量
                    if (Account == Convert.ToString(dr["Account"]) && (Strategy == Convert.ToString(dr["Strategy"]) || (Strategy == Convert.ToString(dr["Strategy"]) + "_" + Convert.ToString(dr["SubStrategy1"]) + "_" + Convert.ToString(dr["SubStrategy2"]))) && ticker == Convert.ToString(dr["Ticker"]))
                    {
                        Hasrecord = true;
                        if (type == "Buy")
                        {
                            // dr["PrePosition"] = Convert.ToInt32(dr["PrePosition"]) + num;
                            dr["TodayBuy"] = num + Convert.ToInt32(dr["TodayBuy"]);

                        }
                        else if (type == "Sell")
                        {
                            //dr["PrePosition"] = Convert.ToInt32(dr["PrePosition"]) - num;
                            //dr["TodaySell"] = Math.Min(Convert.ToInt32(dr["PrePosition"]) - Convert.ToInt32(dr["TodaySell"]), num) + Convert.ToInt32(dr["TodaySell"]);
                            //num = num - Math.Min(Convert.ToInt32(dr["PrePosition"]) - Convert.ToInt32(dr["TodaySell"]), num);
                            dr["TodaySell"] = num + Convert.ToInt32(dr["TodaySell"]);
                        }
                        //if (num==0)
                        //break;
                    }
                }

                //在现有持仓中没有找到对应策略的股票，则新建一条记录
                if (!Hasrecord)
                {
                    DataRow newdr = dt.NewRow();
                    newdr["Account"] = Account;
                    newdr["Ticker"] = ticker;
                    newdr["PreClose"] = 0;
                    if (type == "Buy")
                    {
                        newdr["PrePosition"] = 0;
                        newdr["TodayBuy"] = num;
                        newdr["TodaySell"] = 0;
                    }
                    else
                    {
                        newdr["PrePosition"] = 0;
                        newdr["TodaySell"] = num;
                        newdr["TodayBuy"] = 0;
                    }

                    //alpha策略
                    if (Strategy.Contains("_"))
                    {
                        string[] strValue = Strategy.Split('_');
                        newdr["Strategy"] = strValue[0];
                        newdr["SubStrategy1"] = strValue[1];
                        newdr["SubStrategy2"] = strValue[2];
                    }
                    else
                    {
                        newdr["Strategy"] = Strategy;
                        newdr["SubStrategy1"] = Strategy;
                        newdr["SubStrategy2"] = Strategy;
                    }
                    dt.Rows.Add(newdr);
                }
            }
            sdr.Close();



            //将数据写入数据库
            SqlCommandBuilder scb = new SqlCommandBuilder(SDA);
            SDA.Update(dt);
            dt.AcceptChanges();

            //清空当日成交表
            string sql1 = "delete from OrderBook ";
            SqlCommand cmd1 = new SqlCommand(sql1, conn);
            cmd1.ExecuteNonQuery();
        }

        public void OpenBasektUpdate(string account, string Strategy, string direction, Int32 num)
        {

            DataTable dt = new DataTable();
            SqlDataAdapter SDA = new SqlDataAdapter(" select * from OpenBasket", conn);
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
                dr["TargetNumIF"] =num;
                dr["OpenNumIF"] = 0;
                dt.Rows.Add(dr);
            }


            SqlCommandBuilder scb = new SqlCommandBuilder(SDA);
            SDA.Update(dt);
            dt.AcceptChanges();

        }


        private void button4_Click_1(object sender, EventArgs e)
        {
            Alpha stForm = new Alpha();
            stForm.dataDict = this.dataDict;
            stForm.tc = this.tc;
            stForm.dc = this.dc;
            stForm.ec = this.ec;
            stForm.MF = this;
           
            stForm.Show();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            tc.QueryHold();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            tc.QueryCreditHold();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //读取hs300excel文件,得出个股权重
            string Filename = "";
            Filename = "D:\\下单.csv";

            DataTable HS300Data = new DataTable();
            HS300Data.Columns.Add("Strategy"); //column 2
            HS300Data.Columns.Add("Ticker"); //column 1
            HS300Data.Columns.Add("Position"); //column 3
            HS300Data.Columns.Add("Account"); //column 4

            StreamReader sr = new StreamReader(Filename, UnicodeEncoding.GetEncoding("GB2312"));
           
            while (true)
            {
                string strData = sr.ReadLine();
                if (!String.IsNullOrEmpty(strData))
                {
                    String[] strValue = strData.Split(',');
                    DataRow dr = HS300Data.NewRow();
                    dr["Strategy"] = strValue[0];
                    dr["Ticker"] = strValue[1];
                    dr["Position"] = strValue[2];
                    dr["Account"] = strValue[3];

                    //剔除数量为0的股票
                    if (Convert.ToDouble(strValue[2]) != 0)
                    { HS300Data.Rows.Add(dr); }
                }
                else
                {
                    break;
                }
            }
        

            int quantity = 0;
            string ticker = "";
            foreach (DataRow row in HS300Data.Rows)
            {


                quantity = Convert.ToInt32(row["Position"]);

                if (quantity == 0)
                    continue;

                ticker = Convert.ToString(row["Ticker"]);
                ticker = ticker.Substring(0, 6);
                //  price = Convert.ToDouble(row["Price"]); //暂不读取价格信息，全部采用市价单

                FIXApi.OrderBookEntry entry = new OrderBookEntry();
                if (quantity > 0)
                    entry.type = OrderType.CashBuy;
                else
                {
                    entry.type = OrderType.CashSell;
                    quantity = -quantity;
                }



                entry.ticker = ticker;
                entry.quantityListed = quantity;
                entry.priceListed = 0;




                if (tc != null)
                {

                    entry.bMarket = true;
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

                    entry.bMarket = true;
                    entry.priceListed = 0;



                    entry.strategies = (TradingStrategies)Enum.Parse(typeof(TradingStrategies), Convert.ToString(row["Strategy"]));

                    //Thread orderThread = new Thread(new ParameterizedThreadStart(tc.SendOrder));
                    Thread orderThread = new Thread(new ParameterizedThreadStart(ec.OrderRouter));
                    orderThread.IsBackground = true;
                    orderThread.Start(entry);
                    Thread.Sleep(100);


                }
            }
        }


        //TDX


        private void buttonTDXQueryCash_Click(object sender, EventArgs e)
        {
            textBoxTDXOutPut.Text = tdx.QueryCash();
        }

        private void buttonTDXLogon_Click(object sender, EventArgs e)
        {
            bool isConnected=tdx.TDXOpen();
            if (isConnected)
                textBoxTDXLog.Text="通达信已登录";
        }

        private void buttonTDXLogoff_Click(object sender, EventArgs e)
        {
            tdx.TDXClose();
            textBoxTDXLog.Text = "通达信已断开";
        }

        private void buttonTDXOrderTest_Click(object sender, EventArgs e)
        {
            tdx.TDXTest();
        }

        private void button7_Click(object sender, EventArgs e)
        {

            Alpha stForm = new Alpha();
            stForm.dataDict = this.dataDict;
            stForm.tc = this.tc;
            stForm.dc = this.dc;
            stForm.ec = this.ec;
            stForm.MF = this;

            stForm.Show();
        }

  

    }
}
