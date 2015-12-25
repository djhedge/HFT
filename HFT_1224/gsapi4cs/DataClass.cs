using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDFAPI;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using FIXApi;
using System.Threading;
using System.IO;

namespace HFT
{
    public enum DataMode
    { BackTest, RealTime}
    public class DataClass: TDFDataSource
    {
        public event EventHandler RecvDataReport;
        public event EventHandler WriteLog;
        public event EventHandler EvMarketClose; //收市时触发此事件，由相应绑定的函数处理

        //public event EventHandler AskPriceChanged; 
        public String logStr;
        public TDFOpenSetting openSetting;
        public TDFERRNO nOpenRet;
        public DataMode mode;
        public static String connStr;
        
        public List<string> TickerList;
        public ConcurrentDictionary<string, StockData> HistoricalData;

        public Dictionary<string, SingleStockData> dataDict;
        public int intDate;
        public int intDateIndex;

        public FIXTrade tc;

        public static void SaveShortFileToDB()
        {
              connStr = "server=.;database=HedgeHogDB;integrated security=SSPI";

           
              SqlBulkCopy bulkCopy = new SqlBulkCopy(connStr);
              bulkCopy.DestinationTableName = "ShortTableList";

              bulkCopy.ColumnMappings.Add("Ticker", "Ticker");

              bulkCopy.ColumnMappings.Add("SecurityName", "SecurityName");

              bulkCopy.ColumnMappings.Add("ShortQuantity", "ShortQuantity");

              bulkCopy.ColumnMappings.Add("Margin", "Margin");

              String filePath = "D:\\Data\\ShortTableList";
              String[] FileNameList = Directory.GetFiles(filePath);

              DataTable dtData = new DataTable();
              dtData.Columns.Add("Ticker"); //column 1
              dtData.Columns.Add("SecurityName"); //column 2
              dtData.Columns.Add("ShortQuantity"); //column 3
              dtData.Columns.Add("Margin"); //column 4

              int fileCount = 0;

              foreach (String fileName in FileNameList)
              {
                  fileCount++;
                  StreamReader sr = new StreamReader(fileName, UnicodeEncoding.GetEncoding("GB2312"));
                  sr.ReadLine();
                  int rowCount = 1;
                  while (true)
                  {
                      String strData = sr.ReadLine();
                      rowCount++;
                      if (!String.IsNullOrEmpty(strData))
                      {
                          String[] strValue = strData.Split(',');
                          DataRow dr = dtData.NewRow();

                          dr["Ticker"] = strValue[1].Substring(0, 6);

                          dr["SecurityName"] = strValue[2];
                       
                          dr["ShortQuantity"] = strValue[3];
                          dr["Margin"] = Convert.ToDouble(strValue[5]);
                          if (!(strValue[7] == "融券可用"))
                     
                        
                          {
                              continue;
                          }
                        

                          dtData.Rows.Add(dr);
                      }
                      else
                      {
                          //Console.WriteLine(string.Format("已完成读取文件 {0}， 共计 {1}个", fileName, fileCount));
                          break;
                      }
                  }

                  bulkCopy.WriteToServer(dtData);
              }
        }
        public double GetLatestAsk1(string ticker)
        {
            if (HistoricalData.Keys.Contains(ticker))
                return HistoricalData[ticker].DailyDataList[intDateIndex].GetLastestAskPrice();
            else
                return -1;
        }

        public bool bBackTesting;

        // 非代理方式连接和登陆服务器
        public DataClass(TDFOpenSetting _openSetting)
            : base(_openSetting)
        {
            ShowAllData = false;
            logStr = "";

            TickerList = new List<string>();
            HistoricalData = new ConcurrentDictionary<string, StockData>();

            openSetting = _openSetting;
            intDate = (int)openSetting.Date;

        }

        public void LoadFromTDF()
        {
            nOpenRet = this.Open();
            //nOpenRet = TDFERRNO.TDF_ERR_UNKOWN;
            //WriteLog(this, null);
        }

        public  void LoadFromDatabase()
        {
            int iRow = 0;

            try
            {
                SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                //string strTicker = "select distinct ticker from v_HistPrices07  where intDate=20140731 and ticker='000762'  order by ticker";
                //string strData = "select * from v_HistPrices07 where intDate=20140731 and ticker='000762' order by ticker, intDate, intTime"; 
                string strTicker = "select distinct ticker from v_HistPrices07 order by ticker";
                string strData = "select * from v_HistPrices07 order by ticker, intDate, intTime"; 
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = strTicker;
                cmd.CommandTimeout = 0;

                SqlDataReader dr = cmd.ExecuteReader();		//执行SQL，返回一个“流”
                while (dr.Read())
                {
                    TickerList.Add(dr["Ticker"].ToString());
                }
                dr.Close();
                cmd.CommandText = strData;

                string prevTk = "", currTk = "";
                int prevDate = 0, currDate = 0;

                int drTime;
                double drPrice;

                StockData sd = null;
                DailyData dd = null;

                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    currTk = dr["Ticker"].ToString();
                    currDate = Convert.ToInt32(dr["intDate"].ToString());
                    drTime = Convert.ToInt32(dr["intTime"].ToString());
                    drPrice = Convert.ToDouble(dr["LastPrice"].ToString());

                    if (iRow == 0)
                    {
                        prevTk = currTk;
                        prevDate = currDate;

                        sd = new StockData(currTk);
                        dd = new DailyData(currDate);
                        dd.AddToEnd(drTime, drPrice);
                        iRow++;
                        continue;
                    }

                    if ((currTk == prevTk) && (currDate == prevDate)) //同一股票，同一日期
                    {
                        dd.AddToEnd(drTime, drPrice);
                    }
                    else if ((currTk == prevTk) && (currDate != prevDate)) //同一股票，不同日期
                    {

                        sd.AddToEnd(dd);
                        dd = new DailyData(currDate);
                        dd.AddToEnd(drTime, drPrice);
                        prevDate = currDate;
                    }
                    else //不同股票，可以是同一日期，也可以不同
                    {
                        sd.AddToEnd(dd);
                        HistoricalData.TryAdd(sd.Ticker, sd);
                        sd = new StockData(currTk);
                        dd = new DailyData(currDate);
                        dd.AddToEnd(drTime, drPrice);
                        prevTk = currTk;
                        prevDate = currDate;
                    }
                    iRow++;
                }

                //最后一行未处理
                sd.AddToEnd(dd);
                HistoricalData.TryAdd(sd.Ticker, sd);

                logStr=logStr + "Total Records:" + iRow.ToString() + "\r\n";
                dr.Close();
                conn.Close();             // 关闭数据库连接
                conn.Dispose();           // 释放数据库连接对象

            }
            catch (Exception ex)
            {
                logStr = logStr + "Total Records:" + ex.Message + "\r\n";
                return;
            }

            WriteLog(this, null);

            return;


            //DataAdapter的效率远不及DataReader
            /*
            SqlDataAdapter da = new SqlDataAdapter(sqlStr, conn);
            DataSet ds = new DataSet();
            string dsTable = "PriceHist";
            da.Fill(ds, dsTable);

            DataTable dt = ds.Tables[dsTable];
            foreach (DataRow dr in dt.Rows)
            {
                foreach (DataColumn dc in dt.Columns)
                {
                    Console.WriteLine(dr[dc]);	//遍历表中的每个单元格
                }
            }

            ds.Dispose();        // 释放DataSet对象
            da.Dispose();    // 释放SqlDataAdapter对象
            */
        }

        /*
        public void LoadTickDataFromDatabase()
        {
            int iRow = 0;

            try
            {
                SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                //string strData = "select * from v_HistPricesSeqTest_Details where ticker='600518' order by Ticker, intDate, intTime, OrderSeq";
                string strData = "select * from v_HistPricesSeqTest_Details order by Ticker, intDate, intTime, OrderSeq";
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;

                cmd.CommandText = strData;

                string prevTk = "", currTk = "";
                int prevDate = 0, currDate = 0;
                int drDirection, drVol;

                int drTime;
                double drPrice;

                SingleStockData sd = null;
                
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    currTk = dr["Ticker"].ToString();
                    currDate = Convert.ToInt32(dr["intDate"].ToString());
                    drTime = Convert.ToInt32(dr["intTime"].ToString());
                    drPrice = Convert.ToDouble(dr["LastPrice"].ToString());
                    drDirection=Convert.ToInt32(dr["Direction"].ToString());
                    drVol = Convert.ToInt32(dr["vol"].ToString());

                    if (iRow == 0)
                    {
                        prevTk = currTk;
                        prevDate = currDate;


                        sd = new SingleStockData(currTk);
                        sd.StatusTrending = StrategyStatus.New;
                        sd.DateStamp = currDate;

                        dataDict.Add(currTk, sd);
                        TickerList.Add(currTk);

                        ZBTickData zbdata = new ZBTickData();
                        zbdata.timeStamp = drTime;
                        zbdata.price = drPrice;
                        zbdata.direction = drDirection;
                        zbdata.volume = drVol;
                        dataDict[currTk].zbDataList.Add(zbdata);

                        iRow++;
                        continue;
                    }

                    if (currTk == prevTk)  //同一股票
                    {
                        ZBTickData zbdata = new ZBTickData();
                        zbdata.timeStamp = drTime;
                        zbdata.price = drPrice;
                        zbdata.direction = drDirection;
                        zbdata.volume = drVol;

                        dataDict[currTk].zbDataList.Add(zbdata);
                    }
                    else //不同股票
                    {
                        sd = new SingleStockData(currTk);
                        sd.StatusTrending = StrategyStatus.New;
                        sd.DateStamp = currDate;

                        dataDict.Add(currTk, sd);
                        TickerList.Add(currTk);

                        ZBTickData zbdata = new ZBTickData();
                        zbdata.timeStamp = drTime;
                        zbdata.price = drPrice;
                        zbdata.direction = drDirection;
                        zbdata.volume = drVol;
                        dataDict[currTk].zbDataList.Add(zbdata);

                        prevTk = currTk;
                    }
                    iRow++;
                }

                dr.Close();

                cmd.CommandText = "select Ticker, prevClose, Volume from v_Ticker_Trending";
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    string tk = dr["Ticker"].ToString();
                    int prevVol= Convert.ToInt32(dr["Volume"].ToString());
                    foreach (string str in TickerList)
                    {
                        if (str==tk)
                        {
                            dataDict[str].PrevVolume = prevVol;
                        }
                    }
                }

                logStr = logStr + "Total Records:" + iRow.ToString() + "\r\n";
                dr.Close();




                conn.Close();             // 关闭数据库连接
                conn.Dispose();           // 释放数据库连接对象

            }
            catch (Exception ex)
            {
                logStr = logStr + "Total Records:" + ex.Message + "\r\n";
                return;
            }

            WriteLog(this, null);

            return;

        }
        */

        /*
        public void LoadTickDataAggFromDatabase()
        {
            int iRow = 0;

            try
            {
                SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                //string strData = "select * from v_HistPricesSeqTest_Agg where ticker='600470' order by Ticker, intDate, intTime, mOrderSeq";
                //string strData = "select * from v_HistPricesSeqTest_Agg order by Ticker, intDate, intTime, mOrderSeq";
                string strData = "select * from v_HistPricesSeqTest_Agg_20150511 order by Ticker, intDate, intTime, mOrderSeq";



                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;

                cmd.CommandText = strData;

                string prevTk = "", currTk = "";
                int prevDate = 0, currDate = 0;
                int drDirection, drVol;

                int drTime;
                double drPrice;

                SingleStockData sd = null;

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    currTk = dr["Ticker"].ToString();
                    currDate = Convert.ToInt32(dr["intDate"].ToString());
                    drTime = Convert.ToInt32(dr["intTime"].ToString());
                    drPrice = Convert.ToDouble(dr["AvgPrice"].ToString());
                    drDirection = Convert.ToInt32(dr["Direction"].ToString());
                    drVol = Convert.ToInt32(dr["vol1"].ToString());

                    if (iRow == 0)
                    {
                        prevTk = currTk;
                        prevDate = currDate;


                        sd = new SingleStockData(currTk);
                        sd.StatusTrending = StrategyStatus.New;
                        sd.DateStamp = currDate;

                        dataDict.Add(currTk, sd);
                        TickerList.Add(currTk);

                        ZBTickData zbdata = new ZBTickData();
                        zbdata.timeStamp = drTime;
                        zbdata.price = drPrice;
                        zbdata.direction = drDirection;
                        zbdata.volume = drVol;
                        dataDict[currTk].zbDataList.Add(zbdata);

                        iRow++;
                        continue;
                    }

                    if (currTk == prevTk)  //同一股票
                    {
                        ZBTickData zbdata = new ZBTickData();
                        zbdata.timeStamp = drTime;
                        zbdata.price = drPrice;
                        zbdata.direction = drDirection;
                        zbdata.volume = drVol;

                        dataDict[currTk].zbDataList.Add(zbdata);
                    }
                    else //不同股票
                    {
                        sd = new SingleStockData(currTk);
                        sd.StatusTrending = StrategyStatus.New;
                        sd.DateStamp = currDate;

                        dataDict.Add(currTk, sd);
                        TickerList.Add(currTk);

                        ZBTickData zbdata = new ZBTickData();
                        zbdata.timeStamp = drTime;
                        zbdata.price = drPrice;
                        zbdata.direction = drDirection;
                        zbdata.volume = drVol;
                        dataDict[currTk].zbDataList.Add(zbdata);

                        prevTk = currTk;
                    }
                    iRow++;
                }

                dr.Close();

                cmd.CommandText = "select Ticker, prevClose, Volume from v_Ticker_Trending";
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    string tk = dr["Ticker"].ToString();
                    int prevVol = Convert.ToInt32(dr["Volume"].ToString());
                    foreach (string str in TickerList)
                    {
                        if (str == tk)
                        {
                            dataDict[str].PrevVolume = prevVol;
                        }
                    }
                }

                logStr = logStr + "Total Records:" + iRow.ToString() + "\r\n";
                dr.Close();




                conn.Close();             // 关闭数据库连接
                conn.Dispose();           // 释放数据库连接对象

            }
            catch (Exception ex)
            {
                logStr = logStr + "Total Records:" + ex.Message + "\r\n";
                return;
            }

            WriteLog(this, null);

            return;

        }
        */

        public static string LoadTickersFromDB(string _connStr)
        {
            return "";
        }

        //此版本为高频策略使用
        /*
        public string LoadDataDict()
        {
            string ticker = "", tickerWithMarket = "", tickerListStr = "";

            SqlConnection conn = new SqlConnection(connStr);

            //string sql = "select Ticker, ShortQuantity, prevClose, Volume from v_Ticker_Trending where prevClose>0 and volume>0 and shortquantity*prevClose>20000 Order by ticker";
            //string sql = "select Ticker, ShortQuantity, prevClose, Volume from v_Ticker_Trending where prevClose>0 and volume>0 and shortquantity*prevClose>2000 Order by ticker";//DEBUG
            //string sql = "select Ticker, QuantityHolding, prevClose, PrevVolume from v_Ticker_Trending where ticker='600316' Order by ticker"; //测试用
            string sql = "select Ticker, Quantity, prevClose, PrevVolume from TickerList_HighFreq Order by ticker";

            conn.Open();
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader sqldr = cmd.ExecuteReader();

            int count = 0;
            while (sqldr.Read())
            {
                ticker = sqldr.GetString(0);
                ticker = ticker.Trim();

                if (ticker.StartsWith("60") || ticker.StartsWith("51"))
                {
                    tickerWithMarket = ticker + ".sh";   // 上海
                }
                else if (ticker.StartsWith("00") || ticker.StartsWith("30") || ticker.StartsWith("15"))
                {
                    tickerWithMarket = ticker + ".sz";    // 深圳
                }


                TickerList.Add(ticker);
                tickerListStr = tickerListStr + ";" + tickerWithMarket;

                SingleStockData sd = new SingleStockData(ticker);

                sd.MaxQuantity = Convert.ToInt32(sqldr.GetInt32(1));

                sd.PrevClose = Convert.ToDouble(sqldr.GetDouble(2));
                sd.PrevVolume = Convert.ToInt64(sqldr.GetInt64(3));

                sd.Quantity = 100; //DEBUG

                sd.StatusTrending = StrategyStatus.New;

                dataDict.Add(ticker, sd);
            }

            return tickerListStr;
        }
        */

        //此版本为分级基金套利使用

        public string LoadDataDict()
        {
            string ticker = "", tickerWithMarket = "", tickerListStr = "";
            connStr = "server=192.168.0.169,1433;database=HedgeHogDB;User ID=wg;Password=Pass@word;Connection Timeout=30";
            SqlConnection conn = new SqlConnection(connStr);
           if (intDate==0)
                intDate =Convert.ToInt32(DateTime.Today.ToString("yyyyMMdd"));
            string sql = "select Ticker, Quantity, prevClose, PrevVolume from v_HFT_PrevCloseData  where NowDate ="+intDate.ToString()+" and prevvolume>0 Order by ticker";//融券卖空用
           // string sql = "select top 100 Ticker, Quantity, prevClose, PrevVolume from v_HFT_PrevCloseData  where NowDate =" + intDate.ToString() + "  and ticker='000933' and prevvolume>0 Order by ticker";//融券卖空用

         
            conn.Open();
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader sqldr = cmd.ExecuteReader();

            while (sqldr.Read())
            {
                ticker = sqldr.GetString(0);
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
                    TickerList.Add(ticker);
                    tickerListStr = tickerListStr + ";" + tickerWithMarket;
                }

                SingleStockData sd = new SingleStockData(ticker);

                sd.MaxQuantity = Convert.ToInt32(sqldr.GetInt32(1));

                sd.PrevClose = Convert.ToDouble(sqldr.GetDouble(2));
                sd.PrevVolume = Convert.ToInt64(sqldr.GetInt64(3));

                sd.Quantity = sd.MaxQuantity;
                //sd.Quantity = 100;

                sd.StatusTrending = StrategyStatus.New;
                sd.StatusReverse = StrategyStatus.New;

                sd.DateStamp = intDate;

                if (!dataDict.ContainsKey(ticker))
                    dataDict.Add(ticker, sd);
            }

            return tickerListStr;
        }
        

        public bool WriteToDatabase(DataTable dt)
        {

            SqlConnection conn = new SqlConnection(connStr);
            conn.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;
            cmd.CommandText = "delete from PnLResults where tradedate="+intDate.ToString();
            cmd.ExecuteNonQuery();

            cmd.Dispose();


            SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);
            bulkCopy.BulkCopyTimeout = 0;

            bulkCopy.DestinationTableName = "PnLResults";

            bulkCopy.ColumnMappings.Add("TradeDate", "TradeDate");

            bulkCopy.ColumnMappings.Add("Ticker", "Ticker");

            bulkCopy.ColumnMappings.Add("BuyTime", "BuyTime");

            bulkCopy.ColumnMappings.Add("BuyPrice", "BuyPrice");

            bulkCopy.ColumnMappings.Add("BuyQuantity", "BuyQuantity");

            bulkCopy.ColumnMappings.Add("SellTime", "SellTime");
            bulkCopy.ColumnMappings.Add("SellPrice", "SellPrice");
            bulkCopy.ColumnMappings.Add("SellQuantity", "SellQuantity");
            bulkCopy.ColumnMappings.Add("PnL", "PnL");
            bulkCopy.ColumnMappings.Add("Return", "Return");


            bulkCopy.WriteToServer(dt);

            conn.Close();

            return true;

        }

        public bool ShowAllData { get; set; }

        //重载 OnRecvSysMsg 方法，接收系统消息通知
        // 请注意：
        //  1. 不要在这个函数里做耗时操作
        //  2. 只在这个函数里做数据获取工作 -- 将数据复制到其它数据缓存区，由其它线程做业务逻辑处理
        public override void OnRecvSysMsg(TDFMSG msg)
        {
            if (msg.MsgID == TDFMSGID.MSG_SYS_CONNECT_RESULT)
            {
                //连接结果
                TDFConnectResult connectResult = msg.Data as TDFConnectResult;
                string strPrefix = connectResult.ConnResult ? "连接成功" : "连接失败";
                ;//Console.WriteLine("{0}！server:{1}:{2},{3},{4}, connect id:{5}", strPrefix, connectResult.Ip, connectResult.Port, connectResult.Username, connectResult.Password, connectResult.ConnectID);
            }
            else if (msg.MsgID == TDFMSGID.MSG_SYS_LOGIN_RESULT)
            {
                TDFLoginResult loginResult = msg.Data as TDFLoginResult;
                if (loginResult.LoginResult)
                {
                    //登陆结果
                    ;//Console.WriteLine("登陆成功，市场个数:{0}:", loginResult.Markets.Length);
                    for (int i = 0; i < loginResult.Markets.Length; i++)
                    {
                        ;//Console.WriteLine("market:{0}, dyn-date:{1}", loginResult.Markets[i], loginResult.DynDate[i]);
                    }
                }
                else
                {
                    ;//Console.WriteLine("登陆失败！info:{0}", loginResult.Info);
                }
            }
            else if (msg.MsgID == TDFMSGID.MSG_SYS_CODETABLE_RESULT)
            {
                //接收代码表结果
                TDFCodeResult codeResult = msg.Data as TDFCodeResult;
                ;//Console.WriteLine("获取到代码表, info:{0}，市场个数:{1}", codeResult.Info, codeResult.Markets.Length);
                for (int i = 0; i < codeResult.Markets.Length; i++)
                {
                    ;//Console.WriteLine("market:{0}, date:{1}, code count:{2}", codeResult.Markets[i], codeResult.CodeDate[i], codeResult.CodeCount[i]);
                }

                //客户端请自行保存代码表，本处演示怎么获取代码表内容
                TDFCode[] codeArr;
                GetCodeTable("", out codeArr);
                ;//Console.WriteLine("接收到{0}项代码!, 输出前100项", codeArr.Length);
                for (int i = 0; i < 100 && i < codeArr.Length; i++)
                {
                    if (codeArr[i].Type >= 0x90 && codeArr[i].Type <= 0x95)
                    {
                        // 期权数据
                        TDFOptionCode code = new TDFOptionCode();
                        var ret = GetOptionCodeInfo(codeArr[i].WindCode, ref code);
                        ;//PrintHelper.PrintObject(code);
                    }
                    else
                    {
                        ;//PrintHelper.PrintObject(codeArr[i]);
                    }
                }
            }
            else if (msg.MsgID == TDFMSGID.MSG_SYS_QUOTATIONDATE_CHANGE)
            {
                //行情日期变更。
                TDFQuotationDateChange quotationChange = msg.Data as TDFQuotationDateChange;
                ;//Console.WriteLine("接收到行情日期变更通知消息，market:{0}, old date:{1}, new date:{2}", quotationChange.Market, quotationChange.OldDate, quotationChange.NewDate);
            }
            else if (msg.MsgID == TDFMSGID.MSG_SYS_MARKET_CLOSE)
            {
                //闭市消息
                TDFMarketClose marketClose = msg.Data as TDFMarketClose;
                ;//Console.WriteLine("接收到闭市消息, 交易所:{0}, 时间:{1}, 信息:{2}", marketClose.Market, marketClose.Time, marketClose.Info);

                if (!(marketClose.Market=="CF"))
                    EvMarketClose(this, null);
            }
            else if (msg.MsgID == TDFMSGID.MSG_SYS_HEART_BEAT)
            {
                //心跳消息
                ;//Console.WriteLine("接收到心跳消息!");
            }
        }
        
        //重载OnRecvDataMsg方法，接收行情数据
        // 请注意：
        //  1. 不要在这个函数里做耗时操作
        //  2. 只在这个函数里做数据获取工作 -- 将数据复制到其它数据缓存区，由其它线程做业务逻辑处理
        public override void OnRecvDataMsg(TDFMSG msg)
        {
            try
            {
                if (msg.MsgID == TDFMSGID.MSG_DATA_MARKET)
                {
                    //行情消息
                    TDFMarketData[] marketDataArr = msg.Data as TDFMarketData[];

                        foreach (TDFMarketData data in marketDataArr)
                        {
                            //if (data.Code.Contains("10000015"))
                            //    continue;
                            lock (this.dataDict)
                            {
                                if (dataDict.Keys.Contains(data.Code))
                                {
                                    SingleStockData sd = dataDict[data.Code];

                                    //数据获取部分
                                    if (data.Match != 0)
                                    {
                                        sd.LastPrice = (double)data.Match / 10000; //最新成交价
                                    }
                                    sd.PrevClose = (double)data.PreClose / 10000;
                                    sd.AskPrices[0] = (double)data.AskPrice[0] / 10000;
                                    sd.BidPrices[0] = (double)data.BidPrice[0] / 10000;

                                    for (int i = 0; i < 10; i++)
                                    {
                                        sd.BidPrices[i] = (double)data.BidPrice[i] / 10000;
                                        sd.AskPrices[i] = (double)data.AskPrice[i] / 10000;
                                        sd.BidVol[i] = data.BidVol[i];
                                        sd.AskVol[i] = data.AskVol[i];
                                    }

                                    if ((sd.ShortPriceListed != sd.AskPrices[0])&&(sd.StatusTrending==StrategyStatus.ShortListedOnly)) //最新卖一已变动，需改价重挂
                                        sd.bAskPriceChanged = true; //测试，按卖五挂单
                                    # region Fishing Strategies
                                    //业务逻辑部分，只有对FishingStrategy这样时效要求极高的放在此处，其余均放入EngineClass
                                    if (sd.StatusCBFishing != StrategyStatus.None) //此ticker需要跑CBFishing Strategy
                                    {
                                        if (sd.StatusCBFishing == StrategyStatus.New) //首次挂买单，或上一轮结束后重新挂单
                                        {

                                            //sd.LongPriceListed = sd.BidPrices[0] - sd.CBBuyPriceOffset; //挂在卖五，后续可以参数化
                                            sd.LongPriceListed = sd.BidPrices[4]; //挂在卖五，后续可以参数化
                                            sd.StatusCBFishing = StrategyStatus.LongListedOnly;

                                            //下一步考虑在EngineClass或TradeClass中生成Order，只需有strategy和ticker，即可在dataDict中取到生成order所需数据
                                            FIXApi.OrderBookEntry entry = new OrderBookEntry();
                                            entry.strategies = TradingStrategies.CBFishing;
                                            entry.action = OrderAction.Buy;
                                            entry.type = FIXApi.OrderType.CreditBuy;
                                            entry.ticker = data.Code;
                                            entry.quantityListed = sd.Quantity;
                                            entry.priceListed = sd.LongPriceListed;

                                            sd.StatusCBFishing = StrategyStatus.Pending; //获得成交回报后再改变状态，避免重复执行
                                            Thread orderThread = new Thread(new ParameterizedThreadStart(tc.OrderSender_CBFishing));
                                            orderThread.IsBackground = true;
                                            orderThread.Start(entry);
                                        }
                                        else if (sd.StatusCBFishing == StrategyStatus.LongListedOnly)   //买单已挂，根据最新价格检查是否需要撤单重挂
                                        {
                                            if ((sd.LongPriceListed >= sd.BidPrices[3]) || (sd.LongPriceListed <= sd.BidPrices[5])) //大于卖三或小于卖七时重挂
                                            {
                                                sd.LongPriceListed = sd.BidPrices[0] - sd.CBBuyPriceOffset;
                                                FIXApi.OrderBookEntry entry = new OrderBookEntry();
                                                entry.strategies = TradingStrategies.CBFishing;
                                                entry.action = OrderAction.CancelAndBuy;
                                                entry.type = FIXApi.OrderType.CreditBuy;
                                                entry.ticker = data.Code;
                                                entry.quantityListed = sd.Quantity;
                                                entry.priceListed = sd.LongPriceListed;

                                                sd.StatusCBFishing = StrategyStatus.Pending; //获得成交回报后再改变状态，避免重复执行
                                                Thread orderThread = new Thread(new ParameterizedThreadStart(tc.OrderSender_CBFishing));
                                                orderThread.IsBackground = true;
                                                orderThread.Start(entry);
                                            }
                                            //if ((sd.ShortPriceListed<data.AskPrice[4]) || (sd.ShortPriceListed>data.AskPrice[6]) ) //低于卖五或者高于卖七时撤单重挂，避免撤单过于频繁
                                            //    sd.ShortPriceListed = sd.AskPrices[4];
                                            ////cancel and resend
                                        }
                                        else if (sd.StatusCBFishing == StrategyStatus.ShortListedOnly)   //卖单已挂，根据最新价格检查是否需要撤单重挂
                                        {
                                            if (sd.ShortPriceListed > sd.AskPrices[0])
                                            {
                                                sd.ShortPriceListed = sd.AskPrices[0] - sd.CBSellPriceOffset;  //一直挂在比卖一低0.01的位置
                                                FIXApi.OrderBookEntry entry = new OrderBookEntry();
                                                entry.strategies = TradingStrategies.CBFishing;
                                                entry.action = OrderAction.CancelAndSell_CF;
                                                entry.type = FIXApi.OrderType.CreditSell;
                                                entry.ticker = data.Code;
                                                entry.quantityListed = sd.Quantity;
                                                entry.priceListed = sd.ShortPriceListed;
                                                entry.strategies = TradingStrategies.CBFishing;
                                                sd.StatusCBFishing = StrategyStatus.Pending; //获得成交回报后再改变状态，避免重复执行
                                                Thread orderThread = new Thread(new ParameterizedThreadStart(tc.OrderSender_CBFishing));
                                                orderThread.IsBackground = true;
                                                orderThread.Start(entry);
                                            }
                                            //if ((sd.ShortPriceListed<data.AskPrice[4]) || (sd.ShortPriceListed>data.AskPrice[6]) ) //低于卖五或者高于卖七时撤单重挂，避免撤单过于频繁
                                            //    sd.ShortPriceListed = sd.AskPrices[4];
                                            ////cancel and resend
                                        }
                                        else if (sd.StatusStockFishing != StrategyStatus.None) //此ticker需要跑StockFishing Strategy
                                        {


                                        }
                                    }
                                    # endregion
                                }
                            }
                        }
                    //极其耗时，轻易不要打开
                    //RecvDataReport(this, null);
                }
                else if (msg.MsgID == TDFMSGID.MSG_DATA_FUTURE)
                {
                    //期货行情消息
                    TDFFutureData[] futureDataArr = msg.Data as TDFFutureData[];
                    foreach (TDFFutureData data in futureDataArr)
                    {
                        ;
                    }
                        return;
                }
                else if (msg.MsgID == TDFMSGID.MSG_DATA_INDEX)
                {
                    //指数消息
                    TDFIndexData[] indexDataArr = msg.Data as TDFIndexData[];
                    foreach (TDFIndexData data in indexDataArr)
                    {
                        return; 
                    }

                }
                else if (msg.MsgID == TDFMSGID.MSG_DATA_TRANSACTION)
                {
                    //逐笔成交
                    TDFTransaction[] transactionDataArr = msg.Data as TDFTransaction[];
                    foreach (TDFTransaction data in transactionDataArr)
                    {
                        //每一data变量包含最底层的一笔逐笔成交数据
                        lock (this.dataDict)
                        {
                            if (dataDict.Keys.Contains(data.Code))
                            {
                                //数据获取部分
                                int timeStamp = Convert.ToInt32((double)data.Time / 1000 -0.5); //去掉毫秒部分;

                                if (timeStamp == 93524) timeStamp = 93524;

                                int direction = 0;
                                if (data.BSFlag == 66)
                                    direction = 1;
                                else if (data.BSFlag == 83)
                                    direction = -1;
                                int volume = data.Volume;
                                double price = (double)data.Price / 10000;

                                if (dataDict[data.Code].zbDataList.Count == 0) //第一次添加
                                {
                                    ZBTickData zbdata = new ZBTickData();
                                    zbdata.timeStamp = timeStamp;

                                    if (direction==1)
                                    {
                                        zbdata.volumeB = volume;
                                        zbdata.priceTotalB = price;
                                        zbdata.countB = 1;
                                        zbdata.priceB = zbdata.priceTotalB / (double)zbdata.countB;//计算该秒、买方向的平均价格
                                    }
                                    else if (direction==-1)
                                    {
                                        zbdata.volumeS = volume;
                                        zbdata.priceTotalS = price;
                                        zbdata.countS = 1;
                                        zbdata.priceS = zbdata.priceTotalS / (double)zbdata.countS;//计算该秒、买方向的平均价格
                                    }

                                    if ((zbdata.priceB>0)&&(zbdata.priceS>0))
                                        zbdata.price = (zbdata.priceB + zbdata.priceS) / 2;
                                    else 
                                        zbdata.price = (zbdata.priceB + zbdata.priceS) ;
                                    zbdata.volume = zbdata.volumeB + zbdata.volumeS;

                                    if ((zbdata.volume == 0) || (zbdata.price == 0)) continue;//空记录
                                    dataDict[data.Code].zbDataList.Add(zbdata);

                                    dataDict[data.Code].ZBFirstIndex = 0;
                                    dataDict[data.Code].ZBLastIndex = 0;
                                }
                                else
                                {
                                    int currLastIndex = dataDict[data.Code].ZBLastIndex;

                                    if (dataDict[data.Code].zbDataList[currLastIndex].timeStamp == timeStamp)  //同一时间戳存在，只需更新价格与成交量，不需插入新纪录
                                    {

                                        ZBTickData zbdata = dataDict[data.Code].zbDataList[currLastIndex];//取出字典中的元素并进行修改，由于是引用，对zbdata的修改会自动在字典中更新

                                        if (direction == 1)
                                        {
                                            zbdata.priceTotalB = zbdata.priceTotalB + price;
                                            zbdata.countB = zbdata.countB + 1;
                                            zbdata.priceB = zbdata.priceTotalB / (double)zbdata.countB;//计算该秒、该方向的平均价格
                                            zbdata.volumeB = zbdata.volumeB + volume;

                                        }
                                        else if (direction == -1)
                                        {
                                            zbdata.priceTotalS = zbdata.priceTotalS + price;
                                            zbdata.countS = zbdata.countS + 1;
                                            zbdata.priceS = zbdata.priceTotalS / (double)zbdata.countS;//计算该秒、该方向的平均价格
                                            zbdata.volumeS = zbdata.volumeS + volume;
                                        }

                                        if ((zbdata.priceB > 0) && (zbdata.priceS > 0))
                                            zbdata.price = (zbdata.priceB + zbdata.priceS) / 2;
                                        else
                                            zbdata.price = (zbdata.priceB + zbdata.priceS);
                                        zbdata.volume = zbdata.volumeB + zbdata.volumeS;

                                        dataDict[data.Code].zbDataList.RemoveAt(currLastIndex);
                                        dataDict[data.Code].zbDataList.Add(zbdata);

                                    }
                                    else //不存在此时间戳，需添加新纪录
                                    {
                                        ZBTickData zbdata = new ZBTickData();
                                        zbdata.timeStamp = timeStamp;

                                        if (direction == 1)
                                        {
                                            zbdata.volumeB = volume;
                                            zbdata.priceTotalB = price;
                                            zbdata.countB = 1;
                                            zbdata.priceB = zbdata.priceTotalB / (double)zbdata.countB;//计算该秒、买方向的平均价格
                                        }
                                        else if (direction == -1)
                                        {
                                            zbdata.volumeS = volume;
                                            zbdata.priceTotalS = price;
                                            zbdata.countS = 1;
                                            zbdata.priceS = zbdata.priceTotalS / (double)zbdata.countS;//计算该秒、买方向的平均价格
                                        }

                                        if ((zbdata.priceB > 0) && (zbdata.priceS > 0))
                                            zbdata.price = (zbdata.priceB + zbdata.priceS) / 2;
                                        else
                                            zbdata.price = (zbdata.priceB + zbdata.priceS);
                                        zbdata.volume = zbdata.volumeB + zbdata.volumeS;

                                        //dataDict[data.Code].totalVolTillNow += zbdata.volume;
                                        if ((zbdata.volume == 0) || (zbdata.price == 0)) continue;//空记录
                                        dataDict[data.Code].zbDataList.Add(zbdata);

                                        dataDict[data.Code].ZBLastIndex = dataDict[data.Code].ZBLastIndex + 1;
                                    }
                                }

                                /*
                                //数据获取部分
                                ZBTickData zbdata = new ZBTickData();
                                zbdata.timeStamp = Convert.ToInt32((double)data.Time / 1000); //去掉毫秒部分

                                //if (zbdata.timeStamp > 93850) { int i = 0; i++; }

                                zbdata.price = (double)data.Price / 10000;
                                if (data.BSFlag == 66)
                                    zbdata.direction = 1;
                                else if (data.BSFlag == 83)
                                    zbdata.direction = -1;
                                else
                                    zbdata.direction = 0;
                                zbdata.volume = data.Volume;
                                dataDict[data.Code].totalVolTillNow += zbdata.volume;

                                dataDict[data.Code].zbDataList.Add(zbdata);
                                
                                if (dataDict[data.Code].zbDataList.Count == 1) //第一次添加
                                {
                                    dataDict[data.Code].ZBFirstIndex = 0;
                                    dataDict[data.Code].ZBLastIndex = 0;
                                }
                                else
                                {
                                    dataDict[data.Code].ZBLastIndex = dataDict[data.Code].ZBLastIndex + 1;
                                }
                                */

                            }
                        }
                        //极其耗时，轻易不要打开
                        //RecvDataReport(this, null);
                    }
                }
                else if (msg.MsgID == TDFMSGID.MSG_DATA_ORDER)
                {
                    //逐笔委托
                    TDFOrder[] orderDataArr = msg.Data as TDFOrder[];
                    foreach (TDFOrder data in orderDataArr)
                    {

                        return; 
                    }
                }
                else if (msg.MsgID == TDFMSGID.MSG_DATA_ORDERQUEUE)
                {
                    //委托队列
                    TDFOrderQueue[] orderQueueArr = msg.Data as TDFOrderQueue[];
                    foreach (TDFOrderQueue data in orderQueueArr)
                    {
                        return; 
                    }
                }
            }
            finally
            {
                //RecvDataReport(this, null);

            }

        }

    }
}
