using System;
using System.Text;
using System.Data;
using QuickFix;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Data.SqlClient;
namespace FIXApi
{
    public enum OrderType
    {
        CashBuy, //普通账户买入
        CashSell, //普通账户卖出
        CreditBuy, //信用账户买入
        CreditSell, //信用账户卖出
        CreditMarginBuy, //融资买入
        CreditMarginSell, //融券卖出
        CreditBuyToCover, //买券还券
        CreditSellToCover //卖券还款
        //现金还款
        //现券还券
    }

    public enum OrderAction {Buy, CancelAndBuy, Sell, CancelAndSell_CF, Lock, ShortSell, PrevLock, CancelLock}

    public enum OrderStatus
    {
        New,
        Listed,
        Filled,
        PartialFilled,
        Cancelled,
        ToCancel,
        Rejected,
        RejectedAndResent,
        Unknown
    }

    public enum TradeMode
    {
        Debug,
        Production,
        Backtest
    }

    public enum TradingStrategies
    {
        StockTrending, StockFishing, CBFishing, CBSpread, alpha_arbitrage_HS300, alpha_arbitrage_ZZ500, alpha_arbitrage_SZ50, alpha_mamount_ZZ500,alpha_mamount_TotalA,other_other_other
    }
    
    public class OrderBookEntry:ICloneable
    {
        //账户
        public string Account;
        
        
        
        public string orderId;
        public string origOrderId;
        public string newOrderId;

        public bool bAskPriceChanged;

        public bool bAuto;
        public OrderStatus status;
        public OrderType type;
        public OrderAction action;
        public TradingStrategies strategies;

        public string ticker;
        public int quantityListed;
        public double priceListed;
        public bool bMarket;
        public double lockPrice;
        public int orderTime;
        public int orderDate;

        public int quantityExecuted;
        public double priceExecuted;

        public string strLog;

        static public double ShortSaleDecreaseFactor = 0.7;
        static public int MinimunShortSaleSize = 100;

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        
        public OrderBookEntry()
        {
            bAuto = true;
            status = OrderStatus.New;
            bMarket = false; 
        }

        public void Clear()
        {
            orderId="";
            ticker="";
            quantityListed=0;
            priceListed=0;
        }

    }


    public class FIXTrade : QuickFix.MessageCracker, QuickFix.Application
    {
        [DllImport("gsencrypt.dll",CallingConvention = CallingConvention.Cdecl)]
        static extern int gsEncrypt(int pi_iMode, string pi_pszDataRaw, int pi_iDataRawSize, string pi_pszKey, StringBuilder po_pszDataEncrypt, int pi_iDataEncryptSize);

        [DllImport("kernel32")]
        static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        // FIX初始化变量
        //private string order_cfg = "initiator.cfg";
        private string order_cfg;
        private QuickFix.SocketInitiator socketInitiator;
        private QuickFix.FileStoreFactory messageStoreFactory;
        private QuickFix.SessionSettings settings;
        private QuickFix.FileLogFactory logFactory;
        private QuickFix42.MessageFactory messageFactory;

        // 账号密码
        //private string fundid;                       //资金账号
        //private string creditFundid;                 //信用账号
        //private string pwd;
        //private int encrypt_type;
        //private bool resetseqnum;


        public string account;
        public string fundid;                       //资金账号
        public string creditFundid;                 //信用账号
        public string pwd;
        public int encrypt_type;
        public bool resetseqnum;

        private string strSHCreditAcc;
        private string strSZCreditAcc;

        // FIX状态信息
        private QuickFix.SessionID ssnid;           // 连接session
        public string msg;                          // FIX返回信息
        public bool isLoggedOn;                     // FIX连接状态
        private int fClOrdID;                       // 唯一序列号

        // 界面交互
        public DataTable dt_order;                  // 委托表
        public DataTable dt_log;                    // 日志表
        public DataTable dt_fund;                   // 资产表
        public DataTable dt_hold;                   // 股份表
        public bool b_lastrpt;                      // 是否最后一条股份查询
        public event EventHandler ExcuteReport;     // 委托执行（查询委托）返回事件
        public event EventHandler QueryHoldReport;  // 查询资金返回事件
        public event EventHandler QueryFundReport;  // 查询股份返回事件

        //自定义
        public bool bKeepLog = false;
        public List<string> strListLog;
        public event EventHandler WriteLog; //日志记录事件
        public Dictionary<string, OrderBookEntry> OrderBook;
        public TradeMode mode;
        public DataTable dt_PnL;
        public String tradeDate;
        public Dictionary<string, SingleStockData> dataDict;


        public DataTable dt_orderbook;

        public FIXTrade(string cfgFile)
        {
            msg = "正在初始化变量...";

            dt_order = new DataTable();
            dt_order.Columns.Add("指令编号");
            dt_order.Columns.Add("指令时间");
            dt_order.Columns.Add("证券代码");
            dt_order.Columns.Add("方向");
            dt_order.Columns.Add("委托数量");
            dt_order.Columns.Add("成交数量");
            dt_order.Columns.Add("委托价格");
            dt_order.Columns.Add("成交均价");
            dt_order.Columns.Add("状态");

            dt_log = new DataTable();
            dt_log.Columns.Add("时间");
            dt_log.Columns.Add("方向");
            dt_log.Columns.Add("信息");

            dt_fund = new DataTable();
            dt_fund.Columns.Add("资金余额");
            dt_fund.Columns.Add("资金可用余额");
            dt_fund.Columns.Add("资产总值");
            dt_fund.Columns.Add("资金资产");
            dt_fund.Columns.Add("市值");
            dt_fund.Columns.Add("资金买入冻结");
            dt_fund.Rows.Add(dt_fund.NewRow());

            dt_hold = new DataTable();
           
            dt_hold.Columns.Add("股东账号");
            dt_hold.Columns.Add("市场");
            dt_hold.Columns.Add("股票代码");
            dt_hold.Columns.Add("股份余额");
            dt_hold.Columns.Add("股份可用余额");
            dt_hold.Columns.Add("当前拥股数");
            dt_hold.Columns.Add("昨日余额");
            dt_hold.Columns.Add("卖出冻结数");
            dt_hold.Columns.Add("人工冻结数");
            dt_hold.Columns.Add("今日买入数量");
            dt_hold.Columns.Add("当前成本");
            dt_hold.Columns.Add("个股市值");
            dt_hold.Columns.Add("盈亏");
            dt_hold.Columns.Add("持仓成本");
            dt_hold.Columns.Add("买入盈亏");

            dt_hold.Columns.Add("账户");

            dt_orderbook = new DataTable();
            dt_orderbook.Columns.Add("Account");
            dt_orderbook.Columns.Add("OrderId");
            dt_orderbook.Columns.Add("OrderTime");
            dt_orderbook.Columns.Add("OrderDate");
            dt_orderbook.Columns.Add("Ticker");
            dt_orderbook.Columns.Add("Type");       
            dt_orderbook.Columns.Add("Status");
            dt_orderbook.Columns.Add("Strategy");
            dt_orderbook.Columns.Add("QuantityListed");
            dt_orderbook.Columns.Add("PriceListed");
            dt_orderbook.Columns.Add("QuantityExecuted");
            dt_orderbook.Columns.Add("PriceExecuted");

            dt_orderbook.PrimaryKey =new DataColumn[] {dt_orderbook.Columns["OrderId"]};


            isLoggedOn = false;
            b_lastrpt = true;
          
            if (cfgFile != "")
            {
                order_cfg = cfgFile;

                settings = new QuickFix.SessionSettings(order_cfg);
                messageStoreFactory = new QuickFix.FileStoreFactory(settings);
                logFactory = new QuickFix.FileLogFactory(settings);
                messageFactory = new QuickFix42.MessageFactory();
                socketInitiator = new QuickFix.SocketInitiator(this, messageStoreFactory, settings, logFactory, messageFactory);
                //string order="C:\\Users\\wuguang\\Desktop\\高频程序\\HighFreqTrading(多账户)\\HighFreqTrading\\Run\\initiator_debug.cfg";
                string order = System.Environment.CurrentDirectory + "\\" + order_cfg;

                StringBuilder temp = new StringBuilder(255);
                GetPrivateProfileString("INFO", "Fundid", "", temp, 255, order);
                fundid = temp.ToString();
                GetPrivateProfileString("INFO", "CreditFundid", "", temp, 255, order);
                creditFundid = temp.ToString();
                GetPrivateProfileString("INFO", "Pwd", "", temp, 255, order);
                pwd = temp.ToString();

                GetPrivateProfileString("INFO", "encrypt_type", "", temp, 255, order);
                encrypt_type = Convert.ToInt16(temp.ToString());
                GetPrivateProfileString("INFO", "mode", "", temp, 255, order);

                if (temp.ToString() == "Debug")
                    mode = TradeMode.Debug;
                else if (temp.ToString() == "Production")
                    mode = TradeMode.Production;

                resetseqnum = true;
            }

            strListLog = new List<string>();
            OrderBook = new Dictionary<string, OrderBookEntry>();
            dt_PnL = new DataTable();

            dt_PnL.Columns.Add("TradeDate");
            dt_PnL.Columns.Add("Ticker");
            dt_PnL.Columns.Add("BuyTime");
            dt_PnL.Columns.Add("BuyPrice");
            dt_PnL.Columns.Add("BuyQuantity");
            dt_PnL.Columns.Add("SellTime");
            dt_PnL.Columns.Add("SellPrice");
            dt_PnL.Columns.Add("SellQuantity");
            dt_PnL.Columns.Add("PnL");
            dt_PnL.Columns.Add("Return");
        }

        /// <summary>
        /// 登录系统
        /// </summary>
        /// <param name="_fundid">资金账号</param>
        /// <param name="_creditFundid">信用账号</param>
        /// <param name="_pwd">交易密码</param>
        /// <param name="_encrypt_type">加密方式</param>
        public void Logon(string _fundid,string _creditFundid, string _pwd, int _encrypt_type, bool _resetseqnum)
        {
            try
            {
                fundid = _fundid;
                creditFundid = _creditFundid;
                pwd = _pwd;
                encrypt_type = _encrypt_type;
                resetseqnum = _resetseqnum;

                msg = "正在登录...";
                socketInitiator.start();
                Session session = Session.lookupSession(ssnid);
                if ((session != null) && !session.isLoggedOn())
                {
                    session.logon();
                    isLoggedOn = session.isLoggedOn();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }
        }



        public void Logon(object test)
        {
            FIXTrade tmp = (FIXTrade)test;
            try
            {
                fundid = tmp.fundid;
                creditFundid = tmp.creditFundid;
                pwd = tmp.pwd;
                encrypt_type = tmp.encrypt_type;
                resetseqnum = tmp.resetseqnum;

                msg = "正在登录...";
                socketInitiator.start();
                Session session = Session.lookupSession(ssnid);
                if ((session != null) && !session.isLoggedOn())
                {
                    session.logon();
                    isLoggedOn = session.isLoggedOn();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }
        }


        /// <summary>
        /// 登出系统
        /// </summary>
        public void Logout()
        {
            try
            {
                QuickFix42.Message message = new QuickFix42.Message(new MsgType("5"));
                SendToServer(message);
                Session session = Session.lookupSession(ssnid);
                if ((session != null) && session.isLoggedOn())
                {
                    session.logout();
                }
                socketInitiator.stop();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 单笔委托
        /// </summary>
        /// <param name="_side">方向</param>
        /// <param name="_stkcode">股票代码</param>
        /// <param name="_amount">委托数量</param>
        /// <param name="_price">委托金额</param>
        /// <param name="_market">是否市价委托</param>
        public void Order(char _side, string _stkcode, string _amount, string _price, char _market, out string _idBuffer)
        {
            msg = "发送委托请求 @" + DateTime.Now.ToString();
            _idBuffer = GetNextID();
            QuickFix.ClOrdID clordid = new ClOrdID(_idBuffer);

            QuickFix.HandlInst inst = new QuickFix.HandlInst('1');
            QuickFix.Side side = new QuickFix.Side(_side);
            QuickFix.OrdType ordtype = new QuickFix.OrdType((_market));
            QuickFix.Symbol symbol = new QuickFix.Symbol(_stkcode);
            QuickFix.TransactTime time = new QuickFix.TransactTime();
            QuickFix42.NewOrderSingle message = new QuickFix42.NewOrderSingle(clordid, inst, symbol, side, time, ordtype);

            message.setString(38, _amount);
            if (_market == '2')
                message.setString(44, _price);
            message.setString(15, "CNY");

            if (_stkcode.StartsWith("60") || _stkcode.StartsWith("51"))
            {
                message.setString(207, "XSHG");   // 上海
            }
            else if (_stkcode.StartsWith("00") || _stkcode.StartsWith("30") || _stkcode.StartsWith("15"))
            {
                message.setString(207, "XSHE");   // 深圳
            }
            lock (this.dt_order)
            {
                dt_order.Rows.Add(new object[] { clordid, time, _stkcode, _side, _amount, 0, _price, 0, "新单" });
            }
            SendToServer(message);
        }

        /// <summary>
        /// 批量委托
        /// </summary>
        /// <param name="_side">委托表</param>
        public void Order(DataTable order)
        {
            foreach (DataRow dr in order.Rows)
            {
                string idBuffer;
                Order(char.Parse(dr["side"].ToString()), dr["stkcode"].ToString(), 
                    dr["amount"].ToString(), dr["price"].ToString(), char.Parse(dr["market"].ToString()), out idBuffer);
            }
        }

        /// <summary>
        /// 单笔撤单
        /// </summary>
        /// <param name="_id">待撤委托唯一标识号</param>
        /// <param name="_stkcode">股票代码</param>
        /// <param name="_side">方向</param>
        public void Cancel(string _id, string _stkcode, char _side, out string _idBuffer)
        {
            msg = "发送撤单请求 @" + DateTime.Now.ToString();
            OrigClOrdID orig = new OrigClOrdID(_id);
            _idBuffer = GetNextID();
            ClOrdID clordid = new ClOrdID(_idBuffer);
            Symbol symbol = new Symbol(_stkcode);
            Side side = new Side(_side);
            TransactTime time = new TransactTime();
            QuickFix42.OrderCancelRequest message = new QuickFix42.OrderCancelRequest(orig, clordid, symbol, side, time);
            message.setInt(38, 0);
            SendToServer(message);
        }

        /// <summary>
        /// 查询资产
        /// </summary>
        public void QueryFund()
        {
            QuickFix42.Message message = new QuickFix42.Message(new MsgType("UAN"));
            message.setString(710, GetNextID());    // 请求ID
            message.setInt(724, 9);                 // 请求类别
            message.setString(1, fundid);
            message.setInt(581, 300);
            SendToServer(message);
        }

        /// <summary>
        /// 查询股份
        /// </summary>
        public void QueryHold()
        {
            dt_hold.Rows.Clear();
            b_lastrpt = false;
            QuickFix42.Message message = new QuickFix42.Message(new MsgType("UAN"));
            message.setString(710, GetNextID());    // 请求ID
            message.setInt(724, 0);                 // 请求类别
            message.setString(1, fundid);
            message.setInt(581, 300);
            SendToServer(message);
        }



        /// <summary>
        /// 查询单笔委托流水
        /// </summary>
        /// <param name="_id">待查委托唯一标识号</param>
        /// <param name="_stkcode">股票代码</param>
        /// <param name="_side">方向</param>
        public void QuerySerial(string _id, string _stkcode, char _side)
        {
            ClOrdID clordid = new ClOrdID(_id);
            Symbol symbol = new Symbol(_stkcode);
            Side side = new Side(_side);
            QuickFix42.OrderStatusRequest message = new QuickFix42.OrderStatusRequest(clordid, symbol, side);
            SendToServer(message);
        }
       

        /// <summary>
        /// 获取唯一标识符
        /// </summary>
        /// <returns>唯一标识符</returns>
        private string GetNextID()
        {
            string str = DateTime.Now.ToString("yyMMddHHmmss-") + fClOrdID;
            Interlocked.Increment(ref fClOrdID);
            return str;
        }

        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <param name="message">消息</param>
        private void SendToServer(QuickFix42.Message message)
        {
            //可在此处添加逻辑，向OrderBookEntry中的OrderId赋值

            if (ssnid != null && Session.lookupSession(ssnid).isLoggedOn())
                Session.sendToTarget(message, ssnid);
        }

        #region --融资融券相关--

        /// <summary>
        /// 信用委托
        /// </summary>
        /// <param name="_side">方向</param>
        /// <param name="_stkcode">股票代码</param>
        /// <param name="_amount">委托数量</param>
        /// <param name="_price">委托金额</param>
        /// <param name="_market">是否市价委托</param>
        public void CreditOrder(char _side, string _stkcode, string _amount, string _price, char _market, out string _idBuffer)
        {
            msg = "发送委托请求 @" + DateTime.Now.ToString();
            _idBuffer = GetNextID();
            QuickFix.ClOrdID clordid = new ClOrdID(_idBuffer);
            
            QuickFix.HandlInst inst = new QuickFix.HandlInst('1');
            QuickFix.Side side = new QuickFix.Side(_side);
            QuickFix.OrdType ordtype = new QuickFix.OrdType((_market));
            QuickFix.Symbol symbol = new QuickFix.Symbol(_stkcode);
            QuickFix.TransactTime time = new QuickFix.TransactTime();
            QuickFix42.NewOrderSingle message = new QuickFix42.NewOrderSingle(clordid, inst, symbol, side, time, ordtype);

            message.setString(38, _amount);
            if (_market == '2')
                message.setString(44, _price);
            message.setString(15, "CNY");

            if (_stkcode.StartsWith("60") || _stkcode.StartsWith("51"))
            {
                message.setString(207, "XSHG");   // 上海
                strSHCreditAcc = XMLHelp.GetInnerValueStr("accout.xml", "Credit_Acc", "SH_Credit_Acc");
                message.setString(1, strSHCreditAcc);
            }
            else if (_stkcode.StartsWith("00") || _stkcode.StartsWith("30") || _stkcode.StartsWith("15"))
            {
                message.setString(207, "XSHE");   // 深圳
                strSZCreditAcc = XMLHelp.GetInnerValueStr("accout.xml", "Credit_Acc", "SZ_Credit_Acc");
                message.setString(1, strSZCreditAcc);
            }
            lock (this.dt_order)
            { 
                dt_order.Rows.Add(new object[] { clordid, time, _stkcode, _side, _amount, 0, _price, 0, "新单" });
            }
            SendToServer(message);
        }

        /// <summary>
        /// 查询信用资产
        /// </summary>
        public void QueryCreditFund()
        {
            QuickFix42.Message message = new QuickFix42.Message(new MsgType("UAN"));
            message.setString(710, GetNextID());    // 请求ID
            message.setInt(724, 9);                 // 请求类别
            message.setString(1, creditFundid);
            message.setInt(581, 300);
            SendToServer(message);
        }

        /// <summary>
        /// 查询信用股份
        /// </summary>
        public void QueryCreditHold()
        {
            dt_hold.Rows.Clear();
            b_lastrpt = false;
            QuickFix42.Message message = new QuickFix42.Message(new MsgType("UAN"));
            message.setString(710, GetNextID());    // 请求ID
            message.setInt(724, 0);                 // 请求类别
            message.setString(1, creditFundid);
            message.setInt(581, 300);
            SendToServer(message);
        }

        /// <summary>
        ///  融资融券
        /// </summary>
        /// <param name="_Side"></param>
        /// <param name="_stkcode"></param>
        /// <param name="_amount"></param>
        /// <param name="_price"></param>
        /// <param name="_mktFlag"></param>
        /// <param name="_CashMargin"></param>
        public void RZRQ_Order(char _side, string _stkcode, string _amount, string _price, char _mktFlag, char _CashMargin, out string _idBuffer)
        {
            msg = "发送融资融券委托请求 @" + DateTime.Now.ToString();
            _idBuffer = GetNextID();
            QuickFix.ClOrdID clordid = new ClOrdID(_idBuffer);

            QuickFix.HandlInst inst = new QuickFix.HandlInst('1');

            QuickFix.Side side = new QuickFix.Side(_side);
            QuickFix.OrdType ordtype = new QuickFix.OrdType((_mktFlag));
            QuickFix.Symbol symbol = new QuickFix.Symbol(_stkcode);
            QuickFix.TransactTime time = new QuickFix.TransactTime();
            QuickFix42.NewOrderSingle message = new QuickFix42.NewOrderSingle(clordid, inst, symbol, side, time, ordtype);

            message.setChar(544, Convert.ToSByte(_CashMargin));
            message.setString(38, _amount);

            if (_mktFlag == '2')
            {
                message.setString(44, _price);
            }

            message.setString(15, "CNY");

            if (_stkcode.StartsWith("60") || _stkcode.StartsWith("51"))
            {
                message.setString(207, "XSHG");   // 上海
            }
            else if (_stkcode.StartsWith("00") || _stkcode.StartsWith("30") || _stkcode.StartsWith("15"))
            {
                message.setString(207, "XSHE");   // 深圳
            }
            lock (this.dt_order)
            {
                dt_order.Rows.Add(new object[] { clordid, time, _stkcode, _side, _amount, 0, _price, 0, "新单" });
            }

            SendToServer(message);
        }


        #endregion


        #region FIX函数

        /// <summary>
        /// 执行回报
        /// </summary>
        /// <param name="report">回报结果</param>
        /// <param name="sessionID">session号</param>
        public override void onMessage(QuickFix42.ExecutionReport report, SessionID sessionID)
        {
            for (int i = dt_order.Rows.Count - 1; i >= 0; i--)
            {
                //只有撤单拒绝和撤单回报有41字段
                string ClOrdID = report.isSetField(41) ? report.getOrigClOrdID().getValue() : report.getClOrdID().getValue();

                if (dt_order.Rows[i][0].ToString().Equals(ClOrdID))
                {
                    try { object o = report.getInt(39); }
                    catch (Exception ex)
                    {
                        //有时会返回39=A，抛出DataFormatError，疑是委托确认
                        lock (strListLog)
                        { 
                            strListLog.Add(DateTime.Now.ToString() + " onMessage " + "截获异常" + ex.Message);
                        }
                        if (bKeepLog)   WriteLog(this, null);
                        return;
                    }
                    
                    //14：成交数量， 6：成交均价
                    //0: 委托确认 8：委托拒绝 1：全成 2：部成 6：待撤
                    lock (this.dt_order)
                    {
                        if (report.getInt(39) != 8)
                            dt_order.Rows[i]["成交数量"] = report.getInt(14);
                        if (report.getInt(39) == 1 || report.getInt(39) == 2)
                            dt_order.Rows[i]["成交均价"] = report.getDouble(6);
                        switch (report.getInt(39))
                        {
                            case 0:
                                dt_order.Rows[i]["状态"] = "已报"; break;
                            case 1:
                                dt_order.Rows[i]["状态"] = "部成"; break;
                            case 2:
                                dt_order.Rows[i]["状态"] = "成交"; break;
                            case 4:
                                dt_order.Rows[i]["状态"] = "已撤"; break;
                            case 6:
                                dt_order.Rows[i]["状态"] = "待撤"; break;
                            case 8:
                                dt_order.Rows[i]["状态"] = "拒绝"; break;
                            default:
                                dt_order.Rows[i]["状态"] = "未知"; break;
                        }
                    }

                    lock(this.strListLog)
                    {
                        strListLog.Add(DateTime.Now.ToString() + " onMessage " + "收到消息回报" + " 成交数量：" + report.getInt(14));
                    }

                    if (OrderBook.ContainsKey(ClOrdID))
                    {
                        switch (report.getInt(39))
                        {
                            #region case0: 已报
                            case 0: //"已报"，为券商端返回，只是确认经过券商端审核，报送至交易所，无需后续操作
                                OrderBook[ClOrdID].quantityExecuted = report.getInt(38); //委托数量，非成交数量
                                OrderBook[ClOrdID].status = OrderStatus.Listed;

                                //通过该OrderAction来区分如何处理
                                lock (this.dataDict)
                                {
                                    if (OrderBook[ClOrdID].strategies == TradingStrategies.CBFishing)
                                    {
                                        string ticker = OrderBook[ClOrdID].ticker;
                                        if ((OrderBook[ClOrdID].action == OrderAction.Buy) || (OrderBook[ClOrdID].action == OrderAction.CancelAndBuy))
                                            dataDict[ticker].StatusCBFishing = StrategyStatus.LongListedOnly;
                                        else if ((OrderBook[ClOrdID].action == OrderAction.Sell) || (OrderBook[ClOrdID].action == OrderAction.CancelAndSell_CF))
                                            dataDict[ticker].StatusCBFishing = StrategyStatus.ShortListedOnly;
                                    }
                                    else if (OrderBook[ClOrdID].strategies == TradingStrategies.StockTrending)
                                    {
                                        string ticker = OrderBook[ClOrdID].ticker;
                                        if ((OrderBook[ClOrdID].action == OrderAction.Lock)
                                            || (OrderBook[ClOrdID].action == OrderAction.ShortSell)) //融券模式下，只有三种空单：锁券、撤锁挂单、改价重挂
                                            dataDict[ticker].StatusTrending = StrategyStatus.ShortListedOnly;
                                        else if (OrderBook[ClOrdID].action == OrderAction.Buy) //现券模式下，挂买单
                                            dataDict[ticker].StatusTrending = StrategyStatus.LongListedOnly;
                                        else if (OrderBook[ClOrdID].action == OrderAction.Sell) //现券模式下，挂卖单
                                            dataDict[ticker].StatusTrending = StrategyStatus.SellListedOnly;
                                    }
                                }

                                break;
                            #endregion
                            #region case1: 部成
                            case 1: //"部成" //注意部成可能会多次返回， 产生不同的数量和均价，最好用数组来存储每次成交的数量和均价

                                OrderBook[ClOrdID].quantityExecuted = report.getInt(14); //成交数量
                                OrderBook[ClOrdID].priceExecuted = report.getDouble(6); //成交均价
                                OrderBook[ClOrdID].status = OrderStatus.PartialFilled;

                                if (OrderBook[ClOrdID].strategies == TradingStrategies.StockTrending)
                                {
                                    //现券模式下，只有两种单子：Buy, Sell
                                    string ticker = OrderBook[ClOrdID].ticker;
                                    //对于买入而言，部成也当成交处理，开始挂空单，后面剩余多单由于限价较高，都会成交掉，影响不大。
                                    //如果以后做精细，可以考虑等到全部累计成交完后，再根据成交数量挂空单
                                    if (OrderBook[ClOrdID].action == OrderAction.Buy)
                                    {
                                        lock (this.dataDict)
                                        { dataDict[ticker].StatusTrending = StrategyStatus.LongExecuted; } //买单成交后即进入等待，等着5分钟后挂空单
                                        lock (this.dt_PnL)
                                        {
                                            for (int j = dt_PnL.Rows.Count - 1; j >= 0; j--)
                                            {
                                                if ((dt_PnL.Rows[j]["Ticker"].ToString().Equals(ticker)))
                                                {
                                                    dt_PnL.Rows[j]["BuyTime"] = OrderBook[ClOrdID].orderTime.ToString();

                                                    double currPrice = Convert.ToDouble(dt_PnL.Rows[j]["BuyPrice"]);
                                                    double currQuantity = Convert.ToDouble(dt_PnL.Rows[j]["BuyQuantity"]);
                                                    double avgPrice = 0;
                                                    double totalQuantity = 0;

                                                    if (currPrice == 0)
                                                        dt_PnL.Rows[j]["BuyPrice"] = OrderBook[ClOrdID].priceExecuted.ToString();
                                                    else
                                                    {
                                                        avgPrice = (currPrice * currQuantity + OrderBook[ClOrdID].priceExecuted * OrderBook[ClOrdID].quantityExecuted) / (currQuantity + OrderBook[ClOrdID].quantityExecuted);
                                                        dt_PnL.Rows[j]["BuyPrice"] = avgPrice.ToString();
                                                    }

                                                    if (currQuantity == 0)
                                                        dt_PnL.Rows[j]["BuyQuantity"] = OrderBook[ClOrdID].quantityExecuted.ToString();
                                                    else
                                                    {
                                                        totalQuantity = currQuantity + OrderBook[ClOrdID].quantityExecuted;
                                                        dt_PnL.Rows[j]["BuyQuantity"] = totalQuantity.ToString();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else if ((OrderBook[ClOrdID].action == OrderAction.ShortSell) || (OrderBook[ClOrdID].action == OrderAction.Sell))
                                    {
                                        //dataDict[ticker].StatusTrending = StrategyStatus.None;   //空单部成不用修改状态
                                        //减掉已成部分，下次改价撤单重挂时可以用更新后的数量
                                        OrderBook[ClOrdID].quantityListed = OrderBook[ClOrdID].quantityListed - OrderBook[ClOrdID].quantityExecuted;

                                        lock (this.dt_PnL)
                                        {
                                            for (int j = dt_PnL.Rows.Count - 1; j >= 0; j--)
                                            {
                                                if ((dt_PnL.Rows[j]["Ticker"].ToString().Equals(ticker)))
                                                {
                                                    dt_PnL.Rows[j]["SellTime"] = OrderBook[ClOrdID].orderTime.ToString();

                                                    double currPrice = Convert.ToDouble(dt_PnL.Rows[j]["SellPrice"]);
                                                    double currQuantity = Convert.ToDouble(dt_PnL.Rows[j]["SellQuantity"]);
                                                    double avgPrice = 0;
                                                    double totalQuantity = 0;

                                                    if (currPrice == 0)
                                                        dt_PnL.Rows[j]["SellPrice"] = OrderBook[ClOrdID].priceExecuted.ToString();
                                                    else
                                                    {
                                                        avgPrice = (currPrice * currQuantity + OrderBook[ClOrdID].priceExecuted * OrderBook[ClOrdID].quantityExecuted) / (currQuantity + OrderBook[ClOrdID].quantityExecuted);
                                                        dt_PnL.Rows[j]["SellPrice"] = avgPrice.ToString();
                                                    }

                                                    if (currQuantity == 0)
                                                        dt_PnL.Rows[j]["SellQuantity"] = OrderBook[ClOrdID].quantityExecuted.ToString();
                                                    else
                                                    {
                                                        totalQuantity = currQuantity + OrderBook[ClOrdID].quantityExecuted;
                                                        dt_PnL.Rows[j]["SellQuantity"] = totalQuantity.ToString();
                                                    }
                                                }
                                            }
                                        }

                                    }

                                }
# region 钓鱼策略暂不使用
                                /*
                                //先和成交一样处理，反手下单。区别一是反手下单数量有变，把剩余未成单撤掉
                                else if (OrderBook[ClOrdID].strategies == TradingStrategies.CBFishing)
                                {
                                    string ticker = OrderBook[ClOrdID].ticker;
                                    if ((OrderBook[ClOrdID].action == OrderAction.Buy) || (OrderBook[ClOrdID].action == OrderAction.CancelAndBuy))  //买单成交回报(首次挂单或第二轮重挂，或改价重挂)，此时成交后立刻反手挂卖单
                                    {
                                        FIXApi.OrderBookEntry entry = new OrderBookEntry();
                                        entry.strategies = TradingStrategies.CBFishing;
                                        entry.action = OrderAction.Sell;
                                        entry.type = FIXApi.OrderType.CreditSell;
                                        entry.ticker = ticker;
                                        entry.quantityListed = OrderBook[ClOrdID].quantityExecuted;//按成交数量下反手单
                                        lock (this.dataDict)
                                        {
                                            entry.priceListed = dataDict[ticker].AskPrices[0] - dataDict[ticker].CBSellPriceOffset;
                                            entry.strategies = TradingStrategies.CBFishing;
                                            dataDict[ticker].StatusCBFishing = StrategyStatus.Pending;
                                        }

                                        string bufferNotUsed;
                                        Cancel(ClOrdID, OrderBook[ClOrdID].ticker, '1', out bufferNotUsed);

                                        Thread orderThread = new Thread(new ParameterizedThreadStart(OrderSender_CBFishing));
                                        orderThread.IsBackground = true;
                                        orderThread.Start(entry);

                                    }
                                    else if ((OrderBook[ClOrdID].action == OrderAction.Sell) || (OrderBook[ClOrdID].action == OrderAction.CancelAndSell_CF))  //卖单成交回报，此时成交后立刻反手再新开买单
                                    {
                                        //卖单部成其实不应该撤，暂时如此处理
                                        string bufferNotUsed;
                                        Cancel(ClOrdID, OrderBook[ClOrdID].ticker, '2', out bufferNotUsed);
                                        lock (this.dataDict)
                                        {
                                            dataDict[ticker].StatusCBFishing = StrategyStatus.New; //只需将状态调回New，下一次DataClass有数据更新时即会自动开买单
                                        }
                                    }
                                }
                                */
# endregion
                                break;
                            #endregion
                            #region case2: 成交
                            case 2: // "成交"
                                OrderBook[ClOrdID].quantityExecuted = report.getInt(14); //成交数量
                                OrderBook[ClOrdID].priceExecuted = report.getDouble(6); //成交均价
                                OrderBook[ClOrdID].status = OrderStatus.Filled;

                                if (OrderBook[ClOrdID].strategies == TradingStrategies.StockTrending)
                                {
                                    //现券模式下，只有两种单子：Buy, Sell
                                    string ticker = OrderBook[ClOrdID].ticker;
                                    if (OrderBook[ClOrdID].action == OrderAction.Buy)
                                    {
                                        lock(this.dataDict)
                                        {dataDict[ticker].StatusTrending = StrategyStatus.LongExecuted;} //买单成交后即进入等待，等着5分钟后挂空单或卖出
                                        lock(this.dt_PnL)
                                        {
                                            for (int j = dt_PnL.Rows.Count - 1; j >= 0; j--)
                                            {
                                                if ((dt_PnL.Rows[j]["Ticker"].ToString().Equals(ticker)))
                                                {
                                                    dt_PnL.Rows[j]["BuyTime"] = OrderBook[ClOrdID].orderTime.ToString();

                                                    double currPrice = Convert.ToDouble(dt_PnL.Rows[j]["BuyPrice"]);
                                                    double currQuantity = Convert.ToDouble(dt_PnL.Rows[j]["BuyQuantity"]);
                                                    double avgPrice = 0;
                                                    double totalQuantity = 0;

                                                    if (currPrice == 0)
                                                        dt_PnL.Rows[j]["BuyPrice"] = OrderBook[ClOrdID].priceExecuted.ToString();
                                                    else
                                                    {
                                                        avgPrice=(currPrice*currQuantity + OrderBook[ClOrdID].priceExecuted*OrderBook[ClOrdID].quantityExecuted)/(currQuantity+OrderBook[ClOrdID].quantityExecuted);
                                                        dt_PnL.Rows[j]["BuyPrice"] = avgPrice.ToString();
                                                    }

                                                    if (currQuantity == 0)
                                                        dt_PnL.Rows[j]["BuyQuantity"] = OrderBook[ClOrdID].quantityExecuted.ToString();
                                                    else
                                                    {
                                                        totalQuantity = currQuantity + OrderBook[ClOrdID].quantityExecuted;
                                                        dt_PnL.Rows[j]["BuyQuantity"] = totalQuantity.ToString();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else if ((OrderBook[ClOrdID].action == OrderAction.ShortSell) || (OrderBook[ClOrdID].action == OrderAction.Sell))
                                    {
                                        lock(this.dataDict)
                                        { dataDict[ticker].StatusTrending = StrategyStatus.None; }//空单成交后即结束，每只股票每天只做一次. 
                                        lock (this.dt_PnL)
                                        {
                                            for (int j = dt_PnL.Rows.Count - 1; j >= 0; j--)
                                            {
                                                if ((dt_PnL.Rows[j]["Ticker"].ToString().Equals(ticker)))
                                                {
                                                    dt_PnL.Rows[j]["SellTime"] = OrderBook[ClOrdID].orderTime.ToString();

                                                    double currPrice = Convert.ToDouble(dt_PnL.Rows[j]["SellPrice"]);
                                                    double currQuantity = Convert.ToDouble(dt_PnL.Rows[j]["SellQuantity"]);
                                                    double avgPrice = 0;
                                                    double totalQuantity = 0;

                                                    if (currPrice == 0)
                                                        dt_PnL.Rows[j]["SellPrice"] = OrderBook[ClOrdID].priceExecuted.ToString();
                                                    else
                                                    {
                                                        avgPrice = (currPrice * currQuantity + OrderBook[ClOrdID].priceExecuted * OrderBook[ClOrdID].quantityExecuted) / (currQuantity + OrderBook[ClOrdID].quantityExecuted);
                                                        dt_PnL.Rows[j]["SellPrice"] = avgPrice.ToString();
                                                    }

                                                    if (currQuantity == 0)
                                                        dt_PnL.Rows[j]["SellQuantity"] = OrderBook[ClOrdID].quantityExecuted.ToString();
                                                    else
                                                    {
                                                        totalQuantity = currQuantity + OrderBook[ClOrdID].quantityExecuted;
                                                        dt_PnL.Rows[j]["SellQuantity"] = totalQuantity.ToString();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                # region 钓鱼策略暂不使用
                                /*
                                else if (OrderBook[ClOrdID].strategies == TradingStrategies.CBFishing)
                                {
                                    string ticker = OrderBook[ClOrdID].ticker;
                                    if ((OrderBook[ClOrdID].action == OrderAction.Buy) || (OrderBook[ClOrdID].action == OrderAction.CancelAndBuy))  //买单成交回报(首次挂单或第二轮重挂，或改价重挂)，此时成交后立刻反手挂卖单
                                    {

                                        FIXApi.OrderBookEntry entry = new OrderBookEntry();
                                        entry.strategies = TradingStrategies.CBFishing;
                                        entry.action = OrderAction.Sell;
                                        entry.type = FIXApi.OrderType.CreditSell;
                                        entry.ticker = ticker;
                                        lock (this.dataDict)
                                        {
                                            entry.quantityListed = dataDict[ticker].Quantity;
                                            entry.priceListed = dataDict[ticker].AskPrices[0] - dataDict[ticker].CBSellPriceOffset;
                                            entry.strategies = TradingStrategies.CBFishing;
                                            dataDict[ticker].StatusCBFishing = StrategyStatus.Pending;
                                        }

                                        Thread orderThread = new Thread(new ParameterizedThreadStart(OrderSender_CBFishing));
                                        orderThread.IsBackground = true;
                                        orderThread.Start(entry);

                                    }
                                    else if ((OrderBook[ClOrdID].action == OrderAction.Sell) || (OrderBook[ClOrdID].action == OrderAction.CancelAndSell_CF))  //卖单成交回报，此时成交后立刻反手再新开买单
                                    {
                                        lock (this.dataDict)
                                        {
                                            dataDict[ticker].StatusCBFishing = StrategyStatus.New; //只需将状态调回New，下一次DataClass有数据更新时即会自动开买单
                                        }
                                    }
                                }
                                */
                                #endregion
                                break;
                            #endregion
                            #region case4: 已撤
                            case 4: // "已撤"，撤单指令回报
                                OrderBook[ClOrdID].quantityExecuted = report.getInt(14); //成交数量
                                OrderBook[ClOrdID].status = OrderStatus.Cancelled;
                                break;
                            #endregion
                            #region case6: 待撤
                            case 6: // "待撤"，不知何种情况
                                OrderBook[ClOrdID].quantityExecuted = report.getInt(14); //成交数量
                                OrderBook[ClOrdID].status = OrderStatus.ToCancel;
                                break;
                            #endregion
                            #region case8: 拒绝
                            case 8:  //"拒绝";

                                string rejectReason = report.getString(58);
                                if (rejectReason.Contains("融券总额不足尚差")) //券商端因为可融券数量不够而拒绝，只收到一次回报
                                {
                                    int startIndex = rejectReason.LastIndexOf('-');
                                    string strQuantity = rejectReason.Substring(startIndex, rejectReason.Length - startIndex);
                                    int negativeQuantity = 0;
                                    if (Int32.TryParse(strQuantity, out negativeQuantity))
                                    {
                                        int newQuantity = OrderBook[ClOrdID].quantityListed + negativeQuantity; //e.g.: 1000 + (-900) = 100
                                        if (newQuantity >= OrderBookEntry.MinimunShortSaleSize)
                                        {
                                            OrderBookEntry newEntry = (OrderBookEntry)OrderBook[ClOrdID].Clone();
                                            newEntry.status = OrderStatus.New;
                                            newEntry.quantityListed = newQuantity;
                                            string bufferUsed;
                                            SendOrder(newEntry, out bufferUsed); //再次发送
                                            OrderBook[ClOrdID].newOrderId = bufferUsed;
                                            OrderBook[ClOrdID].status = OrderStatus.RejectedAndResent;
                                            newEntry.orderId = bufferUsed;
                                            OrderBook.Add(newEntry.orderId, newEntry);
                                        }
                                        else
                                        {
                                            OrderBook[ClOrdID].status = OrderStatus.Rejected;
                                            OrderBook[ClOrdID].strLog = "无券可融";
                                        }
                                    }
                                }
                                //else if (rejectReason.Contains("exchange return order rejected! ErrorCode: 282"))  //报单低于卖一拒绝，两次回复，券商端通过，收到成交回报，交易所端拒绝
                                else if (rejectReason.Contains("exchange return order rejected"))  //ErrorCode不仅仅只有282
                                {
                                    OrderBookEntry newEntry = (OrderBookEntry)OrderBook[ClOrdID].Clone();
                                    newEntry.status = OrderStatus.New;

                                    lock (this.dataDict)
                                    {
                                        //此处是唯一给ShortPriceListed赋值的地方，其它情况下均为0
                                        dataDict[newEntry.ticker].ShortPriceListed = dataDict[newEntry.ticker].AskPrices[0];
                                        newEntry.priceListed = dataDict[newEntry.ticker].ShortPriceListed;
                                    }

                                    string bufferUsed;
                                    SendOrder(newEntry, out bufferUsed); //再次发送
                                    OrderBook[ClOrdID].newOrderId = bufferUsed;
                                    OrderBook[ClOrdID].status = OrderStatus.RejectedAndResent;
                                    newEntry.orderId = bufferUsed;
                                    OrderBook.Add(newEntry.orderId, newEntry);
                                }
                                else
                                    OrderBook[ClOrdID].status = OrderStatus.Rejected;//未知原因拒绝;

                                break;
                            #endregion
                            default: // "未知"
                                OrderBook[ClOrdID].status = OrderStatus.Unknown; break;
                        }
                    }

                }
            }

            if (bKeepLog) WriteLog(this, null);

            //ExcuteReport(this, null); //出现过异常，暂停使用
        }

        void Application.fromAdmin(Message message, SessionID sessionID)
        {
            lock (this.dt_log)
            {
                dt_log.Rows.Add(new object[] { DateTime.Now.ToString(), "fromAdmin", message.ToString() });
            }

            isLoggedOn = Session.lookupSession(sessionID).isLoggedOn();
            if ((message is QuickFix42.Logout || message is QuickFix42.Reject) && message.isSetField(58))
            {
                msg = message.getString(58) + " @" + DateTime.Now.ToString();
            }

            lock (this.strListLog)
            {
                strListLog.Add(DateTime.Now.ToString() + " fromAdmin " + "收到回复");
            }
            if (bKeepLog) WriteLog(this, null);
        }

        void Application.fromApp(Message message, SessionID sessionID)
        {
            lock (this.dt_log)
            {
                dt_log.Rows.Add(new object[] { DateTime.Now.ToString(), "fromApp", message.ToString() });
            }

            // 查询股份和资产不是FIX4.2的标准功能，需要特殊处理
            if (message.getHeader().getString(35) == "UAP")
            {
                //查询持仓新增
                int stockNum = Convert.ToInt16(message.getString(727));
                
                
                PosType posType = new PosType();
                LongQty longQty = new LongQty();
                ShortQty shortQty = new ShortQty();
                NoPositions noPositions = new NoPositions();
                Group group = new Group(noPositions.getField(), posType.getField());

                PosAmtType posAmtType = new PosAmtType();
                PosAmt posAmt = new PosAmt();
                NoPosAmt noPosAmt = new NoPosAmt();
                Group group_amt = new Group(noPosAmt.getField(), posAmtType.getField());

                if (message.getField(724) == "0")       // 股份
                {
                    // 如果没有股份则直接返回
                    if (!message.isSetField(noPositions.getField()))
                        return;

                    dt_hold.Rows.Add(dt_hold.NewRow());
                    dt_hold.Rows[dt_hold.Rows.Count - 1][0] = message.getString(1);
                    dt_hold.Rows[dt_hold.Rows.Count - 1][1] = message.getString(207);
                    dt_hold.Rows[dt_hold.Rows.Count - 1][2] = message.getString(55);

                    //账户，后续多账户应改为自动读取
                    dt_hold.Rows[dt_hold.Rows.Count - 1][15] = account;
                    if (message.getString(912).Equals("Y"))
                        b_lastrpt = true;

                    //NoPositions
                    uint i;
                    for (i = 1; i <= int.Parse(message.getField(noPositions.getField())); i++)
                    {
                        message.getGroup(i, group);
                        switch (group.getField(posType.getField()))
                        {
                            case "SB": dt_hold.Rows[dt_hold.Rows.Count - 1][3] = Convert.ToInt32(group.isSetField(longQty.getField()) ? group.getField(longQty.getField()) : "0"); break;
                            case "SAV": dt_hold.Rows[dt_hold.Rows.Count - 1][4] = Convert.ToInt32(group.isSetField(longQty.getField()) ? group.getField(longQty.getField()) : "0"); break;
                            case "SQ": dt_hold.Rows[dt_hold.Rows.Count - 1][5] = Convert.ToInt32(group.isSetField(longQty.getField()) ? group.getField(longQty.getField()) : "0"); break;
                            case "LB": dt_hold.Rows[dt_hold.Rows.Count - 1][6] = Convert.ToInt32(group.isSetField(longQty.getField()) ? group.getField(longQty.getField()) : "0"); break;
                            case "SS": dt_hold.Rows[dt_hold.Rows.Count - 1][7] = Convert.ToInt32(group.isSetField(longQty.getField()) ? group.getField(longQty.getField()) : "0"); break;
                            case "SF": dt_hold.Rows[dt_hold.Rows.Count - 1][8] = Convert.ToInt32(group.isSetField(longQty.getField()) ? group.getField(longQty.getField()) : "0"); break;
                            case "SBQ": dt_hold.Rows[dt_hold.Rows.Count - 1][9] = Convert.ToInt32(group.isSetField(longQty.getField()) ? group.getField(longQty.getField()) : "0"); break;

                        }
                    }

                    //NoPosAmt
                    for (i = 1; i <= int.Parse(message.getField(noPosAmt.getField())); i++)
                    {
                        message.getGroup(i, group_amt);
                        switch (group_amt.getField(posAmtType.getField()))
                        {
                            case "BC": dt_hold.Rows[dt_hold.Rows.Count - 1][10] =Convert.ToDouble(group_amt.isSetField(posAmt.getField()) ? group_amt.getField(posAmt.getField()) : "0"); break;
                            case "SMV": dt_hold.Rows[dt_hold.Rows.Count - 1][11] = Convert.ToDouble(group_amt.isSetField(posAmt.getField()) ? group_amt.getField(posAmt.getField()) : "0"); break;
                            case "IC": dt_hold.Rows[dt_hold.Rows.Count - 1][12] = Convert.ToDouble(group_amt.isSetField(posAmt.getField()) ? group_amt.getField(posAmt.getField()) : "0"); break;
                            case "PC": dt_hold.Rows[dt_hold.Rows.Count - 1][13] = Convert.ToDouble(group_amt.isSetField(posAmt.getField()) ? group_amt.getField(posAmt.getField()) : "0"); break;
                            case "BPL": dt_hold.Rows[dt_hold.Rows.Count - 1][14] = Convert.ToDouble(group_amt.isSetField(posAmt.getField()) ? group_amt.getField(posAmt.getField()) : "0"); break;
                        }
                    }
                }
                else if (message.getField(724) == "9")      // 资金
                {
                    //NoPosAmt
                    for (uint i = 1; i <= int.Parse(message.getField(noPosAmt.getField())); i++)
                    {
                        message.getGroup(i, group_amt);
                        switch (group_amt.getField(posAmtType.getField()))
                        {
                            case "FB": dt_fund.Rows[0][0] = group_amt.getField(posAmt.getField()); break;
                            case "FAV": dt_fund.Rows[0][1] = group_amt.getField(posAmt.getField()); break;
                            case "MV": dt_fund.Rows[0][2] = group_amt.getField(posAmt.getField()); break;
                            case "F": dt_fund.Rows[0][3] = group_amt.getField(posAmt.getField()); break;
                            case "SV": dt_fund.Rows[0][4] = group_amt.getField(posAmt.getField()); break;
                            case "FBF": dt_fund.Rows[0][5] = group_amt.getField(posAmt.getField()); break;
                        }
                    }
                }

                //查询函数新增
                if (dt_hold.Rows.Count == stockNum)
                {
                    //写入数据库
                    WriteToServer(dt_hold);

                }

                if (message.getField(724) == "0")       // 股份
                {
                    //QueryHoldReport(this, null);
                }
                else if (message.getField(724) == "9")  // 资金
                {
                    //QueryFundReport(this, null);
                }
            }
            else
            {
                try
                {
                    base.crack(message, sessionID); // 调用默认处理方法
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                }
            }

            lock (this.strListLog)
            {
                strListLog.Add(DateTime.Now.ToString() + " fromApp "  + message.ToString());
            }

            if (bKeepLog) WriteLog(this, null);

            message.Dispose();
        }


        //查询函数新增
       public void WriteToServer(DataTable dt)
        {
            //string connStr = "server=.;database=HedgeHogDB;Trusted_Connection=SSPI";
             
            string connStr = "server=192.168.0.169,1433;database=HedgeHogDB;User ID=wg;Password=Pass@word;Connection Timeout=30";
           
             
            SqlConnection conn = new SqlConnection(connStr);
            SqlCommand qcmd = conn.CreateCommand();
            conn.Open();

            List<string> shareholdlist = new List<string>();
            foreach (DataRow dr in dt.Rows)
            {
                string sharehold = Convert.ToString(dr[0]);
                if (shareholdlist.IndexOf(sharehold) < 0)
                    shareholdlist.Add(sharehold);
            }
       

           //删除该账户持仓
           string sql="";
           if (shareholdlist.Count == 0)
               sql = "delete from StockHold where 账户='" + account+ "' ";
           else if (shareholdlist.Count == 1)
               sql = "delete from StockHold where 账户='" +account+ "' and 股东账号= '" + Convert.ToString(shareholdlist[0]) + "'";
           else if (shareholdlist.Count == 2)
               sql = "delete from StockHold where 账户='" + account + "' and (股东账号= '" + Convert.ToString(shareholdlist[0]) + "' or 股东账号='" + Convert.ToString(shareholdlist[1]) + "')";
           
           
           


            qcmd.CommandText = sql;
            qcmd.ExecuteNonQuery();



            SqlBulkCopy bulkCopy = new SqlBulkCopy(connStr);
            bulkCopy.DestinationTableName = "StockHold";
            bulkCopy.ColumnMappings.Add("账户", "账户");
            bulkCopy.ColumnMappings.Add("股东账号", "股东账号");
            bulkCopy.ColumnMappings.Add("市场", "市场");
            bulkCopy.ColumnMappings.Add("股票代码", "股票代码");
            bulkCopy.ColumnMappings.Add("股份余额", "股份余额");
            bulkCopy.ColumnMappings.Add("股份可用余额", "股份可用余额");
            bulkCopy.ColumnMappings.Add("当前拥股数", "当前拥股数");
            bulkCopy.ColumnMappings.Add("昨日余额", "昨日余额");
            bulkCopy.ColumnMappings.Add("卖出冻结数", "卖出冻结数");
            bulkCopy.ColumnMappings.Add("人工冻结数", "人工冻结数");
            bulkCopy.ColumnMappings.Add("今日买入数量", "今日买入数量");
            bulkCopy.ColumnMappings.Add("当前成本", "当前成本");
            bulkCopy.ColumnMappings.Add("个股市值", "个股市值");
            bulkCopy.ColumnMappings.Add("盈亏", "盈亏");
            bulkCopy.ColumnMappings.Add("持仓成本", "持仓成本");
            bulkCopy.ColumnMappings.Add("买入盈亏", "买入盈亏");

            bulkCopy.WriteToServer(dt);
            conn.Close();
        }
       public void OrderBookUpdate(SqlConnection conn)
       {
           OrderBookUpdate("OrderBook", conn);
           OrderBookUpdate("HistOrderBook", conn);
           OrderBook.Clear();

       }
       public void OrderBookUpdate(string tablename, SqlConnection conn)
       {

           dt_orderbook.Clear();
           //string connStr = "server=192.168.0.169,1433;database=HedgeHogDB;User ID=wg;Password=Pass@word;Connection Timeout=30";
           //SqlConnection conn = new SqlConnection(connStr);       
           //conn.Open();


           //当日成交表需删除其他日期成交记录
           if (tablename.Substring(0, 4) != "Hist")
           {
               SqlCommand qcmd = conn.CreateCommand();
               qcmd.CommandText = "delete from " + tablename + " where OrderDate<" + DateTime.Now.ToString("yyyyMMdd");
               qcmd.ExecuteNonQuery();

           }

           //只需绑定今天的成交回报
           SqlDataAdapter SDA = new SqlDataAdapter("select * from " + tablename + " where OrderDate=" + DateTime.Now.ToString("yyyyMMdd"), conn);
           //从数据库读取数据
           SDA.Fill(dt_orderbook);



           //将OrderBook写入dt_orderbook
           foreach (string key in OrderBook.Keys)
           {
               DataRow dr = dt_orderbook.NewRow();
               OrderBookEntry tmpentry = OrderBook[key];
               //账户，最好在entry中包含下单账户
               dr["Account"] = tmpentry.Account;
               dr["OrderId"] = tmpentry.orderId;
               dr["OrderTime"] = tmpentry.orderTime;
               dr["OrderDate"] = tmpentry.orderDate;
               dr["Ticker"] = tmpentry.ticker;
               if (Convert.ToString(tmpentry.type).Contains("Buy"))
                   dr["Type"] = "Buy";
               else
                   dr["Type"] = "Sell";
               dr["Status"] = Convert.ToString(tmpentry.status); ;
               dr["Strategy"] = Convert.ToString(tmpentry.strategies);
               dr["QuantityListed"] = tmpentry.quantityListed;
               dr["PriceListed"] = tmpentry.priceListed;
               dr["QuantityExecuted"] = tmpentry.quantityExecuted;
               dr["PriceExecuted"] = tmpentry.priceExecuted;

               //dt不包含此主键
               if (dt_orderbook.Rows.Contains(tmpentry.orderId))
               {
                   DataRow olddr = dt_orderbook.Rows.Find(tmpentry.orderId);

                   olddr.Delete();

               }
               dt_orderbook.Rows.Add(dr);
           }


           //将数据写入数据库
           SqlCommandBuilder scb = new SqlCommandBuilder(SDA);
           SDA.Update(dt_orderbook);
           dt_orderbook.AcceptChanges();




       }


    

        void Application.toAdmin(Message message, SessionID sessionID)
        {
            lock (this.dt_log)
            {
                dt_log.Rows.Add(new object[] { DateTime.Now.ToString(), "toAdmin", message.ToString() });
            }


            if (message is QuickFix42.Logon)
            {
                StringBuilder encrypt_pwd = new StringBuilder(128);
                if (encrypt_type == 0)
                    encrypt_pwd.Insert(0, pwd);
                else
                {
                    gsEncrypt(encrypt_type, pwd, pwd.Length, "GWGSFIX", encrypt_pwd, 128);
                }

                // 调用加密函数
                message.setBoolean(141, resetseqnum);        //每次登陆重置序号，不建议开启

                //’Z’表示使用普通资金帐号登录,’X’表示信用资金账户登录,’T’,表示普通资金账户与信息资金账户
                /*
                if (encrypt_type == 0)
                    message.setString(96, "T:110000000572,110050000572:135790:");
                else
                    message.setString(96, "T:110002378162,110062378162:" + encrypt_pwd.ToString() + ":");
                //message.setString(96, "Z:" + fundid + ":" + encrypt_pwd.ToString() + ":");
                //message.setString(96, "X:" + fundid + ":" + encrypt_pwd.ToString() + ":");
                //message.setString(96, "T:110000000572,110050000572:135790:");
                //message.setString(96, "Z:110000000572:135790:");
                 */

                if (encrypt_type == 0)
                    message.setString(96, "T:110000000572,110050000572:135790:");
                else
                    //仅资金账户登录
                    if (creditFundid == "")
                        message.setString(96, "Z:" + fundid + ":" + encrypt_pwd.ToString() + ":");
                    else
                        message.setString(96, "T:" + fundid + "," + creditFundid + ":" + encrypt_pwd.ToString() + ":");
                    //期权测试
                    //message.setString(96, "G:110089000010:"+ encrypt_pwd.ToString() + ":");
                    //message.setString(96, "G:110089000010:" + "46d5a479427d55eb" + ":");

                
                message.setInt(98, encrypt_type);

                lock (this.strListLog)
                {
                    strListLog.Add(DateTime.Now.ToString() + " toAdmin " + "发送登录申请");
                }

                if (bKeepLog) WriteLog(this, null);
            }


        }

        void Application.toApp(Message message, SessionID sessionID)
        {
            lock (this.dt_log)
            {
                dt_log.Rows.Add(new object[] { DateTime.Now.ToString(), "toApp", message.ToString() });
            }

            lock (this.strListLog)
            {
                strListLog.Add(DateTime.Now.ToString() + "toApp" + message.ToString());
            }

            if (bKeepLog) WriteLog(this, null);
        }
        void Application.onCreate(SessionID sessionID)
        {
            msg = "正在创建连接...";
            ssnid = sessionID;
        }

        void Application.onLogon(SessionID sessionID)
        {
            isLoggedOn = Session.lookupSession(sessionID).isLoggedOn();
            if (Session.lookupSession(sessionID).isLoggedOn())
                msg = "登录成功！";
        }

        void Application.onLogout(SessionID sessionID)
        {
            isLoggedOn = Session.lookupSession(sessionID).isLoggedOn();
        }


        #endregion

        #region 自定义

        public void SendOrder(OrderType type, string ticker, string quantity, string price, bool bMarket, out string idBuffer)
        {
            //TOUPDATE, 根据品种决定报价保留到几位小数，目前按股票来，只保留两位小数
            switch (type)
            {
                case OrderType.CashBuy: //普通账户买入
                    Order('1', ticker, quantity, price, bMarket ? '1' : '2', out idBuffer);
                    break;
                case OrderType.CashSell: //普通账户卖出
                    Order('2', ticker, quantity, price, bMarket ? '1' : '2', out idBuffer);
                    break;
                case OrderType.CreditBuy: //信用账户买入
                    CreditOrder('1', ticker, quantity, price, bMarket ? '1' : '2', out idBuffer);
                    break;
                case OrderType.CreditSell: //信用账户卖出
                    CreditOrder('2', ticker, quantity, price, bMarket ? '1' : '2', out idBuffer);
                    break;
                case OrderType.CreditMarginBuy:  //融资买入
                    RZRQ_Order('1', ticker, quantity, price, bMarket ? '1' : '2', '2', out idBuffer);
                    break;
                case OrderType.CreditMarginSell: //融券卖出
                    RZRQ_Order('2', ticker, quantity, price, bMarket ? '1' : '2', '2', out idBuffer);
                    break;
                case OrderType.CreditBuyToCover: //买券还券
                    RZRQ_Order('1', ticker, quantity, price, bMarket ? '1' : '2', '3', out idBuffer);
                    break;
                case OrderType.CreditSellToCover: //卖券还款
                    RZRQ_Order('2', ticker, quantity, price, bMarket ? '1' : '2', '3', out idBuffer);
                    break;
                default:
                    throw new Exception("Unknown Order Type");

                //现金还款（尚未实现）
                //现券还券（尚未实现）
            }
            
        }

        public void SendOrder(OrderBookEntry order, out string idBuffer)
        {

            SendOrder(order.type, order.ticker, order.quantityListed.ToString(), order.priceListed.ToString(), order.bMarket, out idBuffer);
        }

        public void SendOrder(object oEntry)
        {
            OrderBookEntry entry = (OrderBookEntry)oEntry;

            string bufferNotUsed = "";
            SendOrder(entry, out bufferNotUsed);
        }

        public void CalcPnL()
        {
            lock (this.dt_PnL)
            {
                foreach (DataRow dr in dt_PnL.Rows)
                {
                    double buyPrice = Convert.ToDouble(dr["BuyPrice"].ToString());
                    double sellPrice = Convert.ToDouble(dr["SellPrice"].ToString());
                    double buyQuantity = Convert.ToDouble(dr["BuyQuantity"].ToString());
                    double sellQuantity = Convert.ToDouble(dr["SellQuantity"].ToString());

                    double pnl = sellPrice * sellQuantity - buyPrice * buyQuantity;

                    if (buyPrice == 0) continue;

                    double transcost = 0.0016;
                    double r = (sellPrice - buyPrice) / buyPrice - transcost;

                    dr["PnL"] = pnl.ToString();

                    dr["Return"] = r.ToString();

                }
            }

        }

        public void OrderSender_CBFishing(object oEntry)
        {
            OrderBookEntry entry = (OrderBookEntry)oEntry;

            if ((entry.action == OrderAction.CancelAndBuy) || (entry.action == OrderAction.CancelAndSell_CF)) 
            {
                //撤回现有挂单
                string orderId = "";
                foreach (string id in OrderBook.Keys)
                {
                    if ((OrderBook[id].ticker == entry.ticker)&&(OrderBook[id].strategies==TradingStrategies.CBFishing))
                    {
                        if (OrderBook[id].status == OrderStatus.Listed)//在OrderBook寻找唯一的记录，Key是对于同一策略，同一ticker，应该只有唯一一条记录状态是Listed，其它都是Canceled
                         { 
                            string bufferNotUsed;
                            if (entry.action == OrderAction.CancelAndBuy) 
                                Cancel(id, OrderBook[id].ticker, '1', out bufferNotUsed);
                            else if (entry.action == OrderAction.CancelAndSell_CF)
                                Cancel(id, OrderBook[id].ticker, '2', out bufferNotUsed);
                            orderId = id;
                            break;
                         }
                    }
                }

                //等待撤单指令完成
                if (orderId != "")
                {
                    while (OrderBook[orderId].status != OrderStatus.Cancelled)
                        Thread.Sleep(1);
                }
            }

            //下达新的买单指令
            if ((entry.action == OrderAction.Buy) || (entry.action == OrderAction.CancelAndBuy))
            //if ((entry.action == OrderAction.Buy))
            {
                string bufferUsed;
                SendOrder(entry, out bufferUsed);
                entry.orderId = bufferUsed;
                OrderBook.Add(entry.orderId, entry);
            }

            ////下达新的卖单指令
            if ((entry.action == OrderAction.Sell) || (entry.action == OrderAction.CancelAndSell_CF))
            {
                string bufferUsed; 
                SendOrder(entry, out bufferUsed);
                entry.orderId = bufferUsed;
                OrderBook.Add(entry.orderId, entry);
            }
        }

        public void OrderSender_StockTrending(object oEntry)
        {
            OrderBookEntry entry = (OrderBookEntry)oEntry;

            if ((mode == TradeMode.Backtest) || (mode == TradeMode.Debug))
            {
                //只计算P&L，不下单
                //模拟环境，不需要锁券，直接计算买入价            
                //("证券代码");
                //("买入时间");
                //("买入价格");
                //("买入数量");
                //("卖出时间");
                //("卖出价格");
                //("卖出数量");
                //("总盈亏");
                //("收益率");
                if (entry.action == OrderAction.Buy)
                {
                    dt_PnL.Rows.Add(new object[] {entry.orderDate, entry.ticker, entry.orderTime.ToString(), 
         entry.priceListed.ToString(), entry.quantityListed.ToString(),"0","0","0","0","0"});
                }
                else if (entry.action == OrderAction.Sell)
                {
                    for (int i = dt_PnL.Rows.Count - 1; i >= 0; i--)
                    {
                        if ((dt_PnL.Rows[i]["Ticker"].ToString().Equals(entry.ticker)))
                        {
                            if ((dt_PnL.Rows[i]["SellTime"].ToString().Equals("0"))) //针对每日允许多次信号的情况
                            {
                                dt_PnL.Rows[i]["SellTime"] = entry.orderTime.ToString();
                                dt_PnL.Rows[i]["SellPrice"] = entry.priceListed.ToString();
                                dt_PnL.Rows[i]["SellQuantity"] = entry.quantityListed.ToString();
                            }
                        }
                    }
                }

            }
            else //以下为生产环境
            {
                if (entry.action == OrderAction.PrevLock) //先挂单锁券
                {
                    string bufferUsed;
                    SendOrder(entry, out bufferUsed);
                    entry.orderId = bufferUsed;
                    if (!OrderBook.ContainsKey(entry.orderId))
                        OrderBook.Add(entry.orderId, entry);
                    else
                        return;

                    //不太可能出现不成交情况，但需要对异常情况做处理，例如锁券失败等等
                    while (entry.status != OrderStatus.Listed)
                    {
                        if (entry.status == OrderStatus.Rejected)
                        {
                            lock (this.dataDict)
                            {
                                dataDict[entry.ticker].StatusTrending = StrategyStatus.None;
                            }
                            return; //无法锁券，不再下买单
                        }
                        Thread.Sleep(1000);
                    }

                    lock (this.dataDict)
                    {
                        dataDict[entry.ticker].PrevLockQuantity = entry.quantityExecuted;
                    }

                }
                else if (entry.action == OrderAction.Buy) 
                {
                    //下达买单指令
                    //成交价格和成交量由成交回报来填写
                    string bufferUsed = "";
                    lock (this.dt_PnL)
                    {
                        dt_PnL.Rows.Add(new object[] { entry.orderDate, entry.ticker, entry.orderTime.ToString(), "0", "0", "0", "0", "0", "0", "0" });
                    }

                    SendOrder(entry, out bufferUsed);
                    entry.orderId = bufferUsed;
                    OrderBook.Add(entry.orderId, entry);
                    /*
                    //构建锁券指令
                    FIXApi.OrderBookEntry lockEntry = new OrderBookEntry();
                    lockEntry.strategies = TradingStrategies.StockTrending;
                    lockEntry.action = OrderAction.Lock;
                    lockEntry.type = FIXApi.OrderType.CreditMarginSell;
                    lockEntry.ticker = entry.ticker;
                    lockEntry.quantityListed = entry.quantityListed;
                    lockEntry.priceListed = entry.lockPrice;// 挂在接近涨停价
                    lockEntry.orderTime = entry.orderTime;
                    lockEntry.orderDate = entry.orderDate;


                    SendOrder(lockEntry, out bufferUsed); //存在隐患，如被拒绝，则可能还没执行到OrderBook.Add(lockEntry.orderId, lockEntry)，OnMessage函数已经试图开始修改OrderBook同一entry
                    lockEntry.orderId = bufferUsed;
                    OrderBook.Add(lockEntry.orderId, lockEntry);


                    //不太可能出现不成交情况，但需要对异常情况做处理，例如锁券失败等等
                    while (lockEntry.status != OrderStatus.Listed)
                    {
                        if (lockEntry.status == OrderStatus.Rejected)
                        {
                            lock (this.dataDict)
                            {
                                dataDict[lockEntry.ticker].StatusTrending = StrategyStatus.None;
                            }
                            return; //无法锁券，不再下买单
                        }
                        Thread.Sleep(100);
                    }

                    //根据成功锁券数量再决定下多少买单
                    if (lockEntry.quantityExecuted == 0)
                    {
                        lock (this.dataDict)
                        {
                            dataDict[lockEntry.ticker].StatusTrending = StrategyStatus.None;
                        }
                        return;
                    }

                    lock (this.dataDict)
                    {
                        entry.quantityListed = lockEntry.quantityExecuted + dataDict[entry.ticker].PrevLockQuantity;
                    }
                    */
                }
                else if (entry.action == OrderAction.Sell) //正常卖
                {
                    string bufferUsed;
                    SendOrder(entry, out bufferUsed);
                    entry.orderId = bufferUsed;
                    OrderBook.Add(entry.orderId, entry);

                    //卖单成交，该轮交易结束，计算P&L
                    CalcPnL();
                }
                /* 融券卖空，暂不使用
                else if ((entry.action == OrderAction.ShortSell) || (entry.action == OrderAction.CancelLock))
                {
                    //撤回预先锁券挂单
                    string bufferUsed, bufferNotUsed;
                    int QuantityLocked = 0;
                    string orderId = "";

                    int totalCount = OrderBook.Count;
                    string[] idList = new string[totalCount];
                    OrderBook.Keys.CopyTo(idList, 0);

                    for (int j = 0; j < totalCount; j++)
                    {
                        string id = idList[j];
                        //在OrderBook寻找唯一的记录，Key是对于同一策略，同一ticker，应该至多两条，Lock和PrevLock其它都是Canceled
                        if ((OrderBook[id].ticker == entry.ticker)
                            && (OrderBook[id].strategies == TradingStrategies.StockTrending)
                            && (OrderBook[id].type == OrderType.CreditMarginSell)
                            && (OrderBook[id].status == OrderStatus.Listed
                            && ((OrderBook[id].action == OrderAction.Lock) || (OrderBook[id].action == OrderAction.PrevLock))))//只应该有一条，此处不太可能出现部成
                        {
                            QuantityLocked = QuantityLocked + OrderBook[id].quantityExecuted;
                            Cancel(id, OrderBook[id].ticker, '2', out bufferNotUsed);
                            orderId = id; //可能会被赋值两次，非空即可
                            //等待撤单指令完成
                            if (orderId != "")
                            {
                                while (OrderBook[orderId].status != OrderStatus.Cancelled)
                                    Thread.Sleep(10);
                            }

                            orderId = "";
                            //break;
                        }
                    }

                    if (entry.action == OrderAction.CancelLock)
                    {
                        lock (this.dataDict)
                        {
                            dataDict[entry.ticker].StatusTrending = StrategyStatus.None;
                        }
                        return;
                    }

                    if (entry.action == OrderAction.ShortSell)
                    {

                        //下达新的卖空指令

                        if (QuantityLocked != 0) //仅做卖空逻辑不需要撤锁这一步骤
                            entry.quantityListed = QuantityLocked;

                        lock (this.dataDict)
                        {
                            //ShortPriceListed是dataDict层面的成员，负责控制重新挂单
                            dataDict[entry.ticker].ShortPriceListed = entry.priceListed;
                        }

                        SendOrder(entry, out bufferUsed);
                        entry.orderId = bufferUsed;
                        OrderBook.Add(entry.orderId, entry);

                        string _orderId = entry.orderId;
                        //等待挂单完成
                        while (true)
                        {
                            if (OrderBook[_orderId].status == OrderStatus.RejectedAndResent)
                            {
                                string oldOrderId = _orderId;
                                _orderId = OrderBook[oldOrderId].newOrderId;
                                OrderBook.Remove(oldOrderId);
                            }

                            if (OrderBook[_orderId].status == OrderStatus.Listed)
                                break;

                            if (OrderBook[_orderId].status == OrderStatus.Filled) //直接成交，虽然罕见但有可能
                            {
                                lock (this.dataDict)
                                {
                                    dataDict[entry.ticker].StatusTrending = StrategyStatus.None;
                                }
                                CalcPnL();
                                return; //Over
                            }


                            if (OrderBook[_orderId].status == OrderStatus.Rejected)
                            {
                                lock (this.dataDict)
                                {
                                    dataDict[entry.ticker].StatusTrending = StrategyStatus.None;
                                }
                                return; //Over
                            }
                            Thread.Sleep(100);
                        }


                        string currOrderId = _orderId; //前面有可能修改过原entry.orderId
                        bool bAskChanged_local = false;

                        while (OrderBook[currOrderId].status != OrderStatus.Filled)
                        {
                            lock (this.dataDict)
                            {
                                bAskChanged_local = dataDict[entry.ticker].bAskPriceChanged;
                            }
                            string orderToCancelId = "";
                            int QuantityRemaining = 0;
                            if (bAskChanged_local) //改价重挂
                            {
                                bool bFound = false;

                                totalCount = OrderBook.Count;
                                idList = new string[totalCount];
                                OrderBook.Keys.CopyTo(idList, 0);

                                for (int j = 0; j < totalCount; j++)
                                {
                                    string id = idList[j];
                                    if (!(OrderBook.ContainsKey(id))) continue;
                                    if ((OrderBook[id].ticker == entry.ticker)
                                        && (OrderBook[id].strategies == TradingStrategies.StockTrending)
                                        && (OrderBook[id].type == OrderType.CreditMarginSell)) //可能会有多条
                                    {
                                        if ((OrderBook[id].status == OrderStatus.Listed) || (OrderBook[id].status == OrderStatus.PartialFilled)) //部成也需对剩余部分撤单重挂
                                        {
                                            bFound = true;
                                            QuantityRemaining = OrderBook[id].quantityListed;
                                            Cancel(id, OrderBook[id].ticker, '2', out bufferNotUsed);
                                            orderToCancelId = id;

                                            if (orderToCancelId != "")
                                            {
                                                while (OrderBook[orderToCancelId].status != OrderStatus.Cancelled)
                                                    Thread.Sleep(100);
                                            }

                                            orderToCancelId = "";

                                            //break;
                                        }
                                    }
                                }

                                if (!bFound)
                                {
                                    lock (this.dataDict)
                                    {
                                        dataDict[entry.ticker].StatusTrending = StrategyStatus.None;
                                    }
                                    return; //未找到记录，只可能是下新空单指令时出了问题，例如无券可融。只能终止，持有买单到收盘
                                }

                                lock (this.dataDict)
                                {
                                    //此处是唯一给ShortPriceListed赋值的地方，其它情况下均为0
                                    dataDict[entry.ticker].ShortPriceListed = dataDict[entry.ticker].AskPrices[0]; //测试，按卖五挂单
                                }


                                FIXApi.OrderBookEntry newEntry = new OrderBookEntry();
                                newEntry.strategies = TradingStrategies.StockTrending;
                                newEntry.action = OrderAction.ShortSell;
                                newEntry.type = FIXApi.OrderType.CreditMarginSell;
                                newEntry.ticker = entry.ticker;
                                newEntry.quantityListed = QuantityRemaining;
                                lock (this.dataDict)
                                {
                                    newEntry.priceListed = dataDict[entry.ticker].ShortPriceListed;
                                }
                                newEntry.orderTime = entry.orderTime;
                                newEntry.orderDate = entry.orderDate;

                                SendOrder(newEntry, out bufferUsed);
                                newEntry.orderId = bufferUsed;
                                OrderBook.Add(newEntry.orderId, newEntry);

                                currOrderId = newEntry.orderId;

                                lock (this.dataDict)
                                {
                                    dataDict[entry.ticker].bAskPriceChanged = false;
                                }
                            }
                        }
                        Thread.Sleep(500);

                        //卖单成交，该轮交易结束，计算P&L
                        CalcPnL();
                    }
                }
                */
            }
        }

        public void ListOrder(DataTable dt, double vol, OrderType _orderType, char _market) //dt包含Ticker,Weight,Price
        {

            QuickFix42.NewOrderList message = new QuickFix42.NewOrderList();
            message.setString(66, GetNextID());
            message.setInt(394, 3);
            message.setInt(68, 1);

            char _side='0';
            switch (_orderType)
            {
                case OrderType.CreditBuy: //信用账户买入
                    _side = '1';
                    //CreditOrder('1', ticker, quantity, price, bMarket ? '1' : '2', out idBuffer);
                    break;
                case OrderType.CreditMarginBuy:  //融资买入
                    _side = '1';
                    //RZRQ_Order('1', ticker, quantity, price, bMarket ? '1' : '2', '2', out idBuffer);
                    break;
                case OrderType.CreditSell: //信用账户卖出
                    _side = '2';
                    //CreditOrder('2', ticker, quantity, price, bMarket ? '1' : '2', out idBuffer);
                    break;
                default:
                    throw new Exception("Unknown Order Type");
            }


            int count = 1;
            int rowCount = dt.Rows.Count;
            foreach (DataRow dr in dt.Rows)
            {
                QuickFix.ClOrdID clordid = new ClOrdID(GetNextID());
                QuickFix.ListSeqNo listseqno = new QuickFix.ListSeqNo(count);
                QuickFix.HandlInst inst = new QuickFix.HandlInst('1');

                QuickFix.OrdType ordtype = new QuickFix.OrdType(_market);//限价单
                QuickFix.Side side = new QuickFix.Side(_side);

                string _stkcode = Convert.ToString(dr["Ticker"]).Substring(0, 6);
                QuickFix.Symbol symbol = new QuickFix.Symbol(_stkcode);
                QuickFix.TransactTime time = new QuickFix.TransactTime();

                double _amount = vol* Convert.ToDouble(dr["Weight"]);
                QuickFix.OrderQty orderqty = new QuickFix.OrderQty(_amount);

                double _price = Convert.ToDouble(dr["Price"]);
                QuickFix.Price price = new QuickFix.Price(_price);
                QuickFix.Currency currency = new QuickFix.Currency("CNY");

                string accStr="";

                QuickFix.SecurityExchange sec=null;
                QuickFix.Account acc=null;

                if (_stkcode.StartsWith("60") || _stkcode.StartsWith("51"))
                {
                    sec = new QuickFix.SecurityExchange("XSHG");   // 上海
                    if ((_orderType == OrderType.CreditBuy) || (_orderType == OrderType.CreditSell)) //仅限信用账户买卖需要用到股东账号，普通账户或者融资融券都不需要
                    {
                        if (mode==TradeMode.Debug)
                            strSHCreditAcc = XMLHelp.GetInnerValueStr("accout_Debug.xml", "Credit_Acc", "SH_Credit_Acc");
                        else
                            strSHCreditAcc = XMLHelp.GetInnerValueStr("accout.xml", "Credit_Acc", "SH_Credit_Acc");
                        accStr= strSHCreditAcc;
                    }

                }
                else if (_stkcode.StartsWith("00") || _stkcode.StartsWith("30") || _stkcode.StartsWith("15"))
                {
                    sec = new QuickFix.SecurityExchange("XSHE");   // 深圳
                    if ((_orderType == OrderType.CreditBuy) || (_orderType == OrderType.CreditSell)) //仅限信用账户买卖需要用到股东账号，普通账户或者融资融券都不需要
                    {
                        if (mode == TradeMode.Debug)
                            strSZCreditAcc = XMLHelp.GetInnerValueStr("accout_Debug.xml", "Credit_Acc", "SZ_Credit_Acc");
                        else
                            strSZCreditAcc = XMLHelp.GetInnerValueStr("accout.xml", "Credit_Acc", "SZ_Credit_Acc");
                        accStr=strSZCreditAcc;
                    }

                }
                lock (this.dt_order)
                {
                    dt_order.Rows.Add(new object[] { clordid, time, _stkcode, _side, _amount.ToString(), 0, _price.ToString(), 0, "新单" });
                }

                Group subgroup = new Group(73, 11); //固定常数
                acc=new Account(accStr);
                //subgroup.setField(1, accStr);
                subgroup.setField(clordid);
                subgroup.setField(listseqno);
                subgroup.setField(inst);
                subgroup.setField(ordtype);
                subgroup.setField(side);
                subgroup.setField(symbol);
                subgroup.setField(time);
                subgroup.setField(orderqty);
                subgroup.setField(price);
                subgroup.setField(currency);
                subgroup.setField(sec);

                if ((_orderType == OrderType.CreditBuy) || (_orderType == OrderType.CreditSell)) //仅限信用账户买卖需要用到股东账号，普通账户或者融资融券都不需要
                    subgroup.setField(acc);

                //if (_orderType == OrderType.CreditMarginBuy)
                //    subgroup.setField(544, "2");


                message.addGroup(subgroup);
                count = count + 1;

            }
            SendToServer(message);

        }
        }

        #endregion
    }


