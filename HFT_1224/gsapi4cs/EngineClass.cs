using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FIXApi;
using System.Threading;
using System.Collections.Concurrent;

namespace HFT
{
    public class EngineClass
    {
        DataClass dc;
        FIXTrade tc;
        public bool bDebug;
        public bool bRunning;
        public bool bTestEngine;

        public List<string> TickerList;
        public Dictionary<string, int> OrderList;
        //public ConcurrentDictionary<string, StockData> HistoricalData;

        public List<string> strListLog;
        public event EventHandler WriteLog; //日志记录事件

        public Dictionary<string, SingleStockData> dataDict;

        //中金下单使用，临时
        public bool bCicc1 = false, bCicc2 = false;
        public string strCookie1 = "", strCookie2 = "";

        public EngineClass()
        {
            strListLog = new List<string>();
            TickerList = new List<string>();
            
            bRunning = false;

        }

        public void ConnectToTrade(FIXTrade _tc)
        {
            tc = _tc;
        }

        public void ConnectToData(DataClass _dc)
        {
            dc = _dc;
        }
        
        public void PrevLockAll()
        {
            foreach (string ticker in dc.TickerList)
            {
                SingleStockData sd = dataDict[ticker];

                FIXApi.OrderBookEntry entry = new OrderBookEntry();
                entry.strategies = TradingStrategies.StockTrending;
                entry.action = OrderAction.PrevLock;
                entry.type = FIXApi.OrderType.CreditMarginSell;
                entry.ticker = ticker;
                entry.quantityListed = sd.Quantity;

                entry.priceListed = Math.Round(sd.PrevClose * 1.098, 2);

                //entry.orderTime = sd.BuyTime;
                //entry.orderDate = sd.DateStamp;
                Thread orderThread = new Thread(new ParameterizedThreadStart(tc.OrderSender_StockTrending));
                orderThread.IsBackground = true;
                orderThread.Start(entry);
                Thread.Sleep(1000); //避免堵塞
            }
        }

        public void ShortSellTest()
        {

            //OrderList = new Dictionary<string, int>();
            //OrderList.Add("000768", 1000);
            //OrderList.Add("600893", 600);
            //OrderList.Add("300070", 1200);
            //OrderList.Add("300024", 1000);
            //OrderList.Add("300124", 700);
            //OrderList.Add("300058", 800);
            //OrderList.Add("300002", 1000);
            //OrderList.Add("300003", 800);

            if (OrderList == null) return;

            foreach (KeyValuePair<string, int> kvp in OrderList)
            {
                try
                {
                    SingleStockData sd = dataDict[kvp.Key];

                    FIXApi.OrderBookEntry entry = new OrderBookEntry();
                    entry.strategies = TradingStrategies.StockTrending;
                    entry.action = OrderAction.ShortSell;
                    entry.type = FIXApi.OrderType.CreditMarginSell;
                    entry.ticker = kvp.Key;
                    entry.quantityListed = kvp.Value;

                    int tCount = 0;
                    while (sd.AskPrices[0] == 0)
                    {
                        tCount++;
                        Thread.Sleep(100);
                        if (tCount > 100) break;
                    }

                    if (sd.AskPrices[0] == 0) continue;

                    entry.priceListed = sd.AskPrices[0];

                    Thread orderThread = new Thread(new ParameterizedThreadStart(tc.OrderSender_StockTrending));
                    orderThread.IsBackground = true;
                    orderThread.Start(entry);
                }
                catch (Exception ex)
                {
                    strListLog.Add(ex.Message);
                }
            }

        }

        public void Run()
        {
            bTestEngine = false;

            int tLength = 60; //只使用过去一分钟数据

            if (tc.mode == TradeMode.Backtest) //回测模式包括TDF和数据库两种回测
            {
                //回测时是第一层对每只股票代码循环，第二层对每只股票的日期做循环，第三层对每只股票的时间序列循环
                foreach (string ticker in dc.TickerList)
                {
                    if (!dataDict.Keys.Contains(ticker)) continue;

                    SingleStockData sd = dataDict[ticker];

                    //if (sd.zbDataList.Count < 2000) continue;

                    int iStart = 0, iEnd;
                    for (iEnd = 1; iEnd < sd.zbDataList.Count; iEnd++)
                    {
                        sd.totalVolTillNow += sd.zbDataList[iEnd].volume;
                        int timeTillNow = ComputeIntTimeDiff(sd.zbDataList[0].timeStamp, sd.zbDataList[iEnd].timeStamp);
                        sd.avgVolTillNow = (double)sd.totalVolTillNow / (double)timeTillNow;

                        if (sd.zbDataList[iEnd].timeStamp < 94500) continue;

                        if (timeTillNow < tLength) continue;
                        while (ComputeIntTimeDiff(sd.zbDataList[iStart].timeStamp, sd.zbDataList[iEnd].timeStamp) > tLength) //保持内存中时间序列长度固定
                        {
                            iStart++;
                        }

                        if (sd.StatusTrending == StrategyStatus.None) continue; //该股票已交易过，当日只交易一次

                        ComputeOneTick(ticker, iStart, iEnd);
                    }
                }
            }
            else
            {
                //实时时是第一层每秒重新计算一次，第二层对每只股票代码循环，每只股票此时只需计算一次

                //PrevLockAll();

                bRunning = true;

                while (bRunning)
                {
                    int timeStamp=0;
                    foreach (string ticker in dc.TickerList)  //注意不能直接对dict用foreach，否则任何修改都会抛出异常
                    {
                        lock (this.dataDict)
                        {
                            if (!dataDict.Keys.Contains(ticker)) continue;

                            SingleStockData sd = dataDict[ticker];

                            if (sd.StatusTrending == StrategyStatus.None) continue; //该股票已交易过，当日只交易一次

                            if (((sd.ZBFirstIndex) < 0) || ((sd.ZBLastIndex) < 0)) continue;

                            timeStamp=sd.zbDataList[sd.ZBLastIndex].timeStamp;

                            if (timeStamp > sd.prevTimeStamp) sd.prevTimeStamp = timeStamp;
                            else continue; //时间尚未更新，无需后续计算

                            if (timeStamp < 93130) continue; //盘前30秒的数据暂不使用

                            int tDiff = ComputeIntTimeDiff(sd.zbDataList[sd.ZBFirstIndex].timeStamp, sd.zbDataList[sd.ZBLastIndex].timeStamp);

                            int timeTillNow = ComputeIntTimeDiff(93000, sd.zbDataList[sd.ZBLastIndex].timeStamp);


                            sd.avgVolTillNow = (double)sd.totalVolTillNow / (double)timeTillNow;

                            if (bTestEngine)
                            { if ((sd.ZBFirstIndex == 0) && tDiff < 2) continue; } //已读取数据长度不满2分钟
                            else
                            { if ((sd.ZBFirstIndex == 0) && tDiff < tLength) continue; }//已读取数据长度不满2分钟 DEBUG

                            if (sd.zbDataList[sd.ZBLastIndex].timeStamp < 93500) continue;

                            while (ComputeIntTimeDiff(sd.zbDataList[sd.ZBFirstIndex].timeStamp, sd.zbDataList[sd.ZBLastIndex].timeStamp) > tLength) //保持内存中时间序列长度固定
                            {
                                if (sd.zbDataList.Count < 30) break;//交易不活跃，距离太短，保留至少30个tick

                                if ((sd.zbDataList[sd.ZBFirstIndex].timeStamp > 93500)&&((sd.ZBLastIndex-sd.ZBFirstIndex)>tLength))                            
                                    ComputeOneTick(ticker, sd.ZBFirstIndex, sd.ZBFirstIndex + tLength -1); //以秒为tick单位，以后需修改

                                sd.zbDataList.RemoveAt(sd.ZBFirstIndex);
                                sd.ZBLastIndex = sd.ZBLastIndex - 1;


                                //sd.ZBFirstIndex++;
                            }


                            ComputeOneTick(ticker, sd.ZBFirstIndex, sd.ZBLastIndex);
                        }
                    }
                    if (tc.mode == TradeMode.Debug)
                        Thread.Sleep(10); //每秒计算一次
                    else if (tc.mode == TradeMode.Production)
                        Thread.Sleep(500); //每秒计算一次

                    if (timeStamp > 145900) break;
                }
            }

            dc.WriteToDatabase(tc.dt_PnL);
            WriteLog(this, null);
        }
        public void ComputeOneTick(string ticker, int firstIndex, int lastIndex)
        {
            int test;

            double pctChgThreshold = 0.003, //涨幅指标
                volThreshold = 3, //放量指标
                bsThreshold = 0.66, //内外盘指标
                priceVolThreshold = 0.01;

            bool cPriceVol = true, cWithDraw = true, cSingleHugeOrder = true;

            bool cPriceLevel = false, cVolume = false, cBuySellRatio = false, cPriceTrend = true;
            bool cNoSuddenUp = true;//暂不加涨速过快指标，先用内外盘指标控制

            //CuScore常数
            int MALength = 60; //过去一分钟均价


            SingleStockData sd = dataDict[ticker];


            int midIndex = firstIndex;

            ////统计预观察期交易量和价格
            //double p0 = sd.zbDataList[firstIndex].price, p2 = sd.zbDataList[midIndex].price;

            //for (int i = firstIndex; i < midIndex; i++) //遍历过去10分钟逐笔数据，计算相应指标
            //{
            //    ZBTickData zbData = sd.zbDataList[i];

            //    double t1 = Convert.ToDouble(ComputeIntTimeDiff(sd.zbDataList[firstIndex].timeStamp, sd.zbDataList[i].timeStamp));
            //    double p1 = (p2 - p0) / t0 * t1 + p0;

            //    if (Math.Abs(zbData.price / p1 - 1) > priceVolThreshold)
            //    {
            //        cPriceVol = false; //一旦预观察期价格波动幅度过大，则不符合要求
            //        return; //直接退出，该段不符合要求
            //    }

            //    totalPreVolume += zbData.volume;
            //}


            if (sd.zbDataList[lastIndex].timeStamp > 94600)
                cPriceLevel = false;

            double zbMinPrice = sd.zbDataList[midIndex].price;
            double zbMaxPrice = sd.zbDataList[midIndex].price;

            int zbMinIndex = midIndex, zbMaxIndex = midIndex;

            int totalBuyVolume = 0, totalSellVolume = 0, totalBuyCount = 0, totalSellCount = 0;
            int totalObsVolume = 0;
            int maxSingleVolume = 0;

            double timeToLast = 0;

            double maPriceSum = 0, maCountSum = 0, maPrice = 0;

            for (int i = lastIndex; i > midIndex; i--) //遍历过去观察期内逐笔数据，计算相应指标
            {
                ZBTickData zbData = sd.zbDataList[i];

                if (sd.bComputeCuscore)
                {
                    maPriceSum = maPriceSum + zbData.price;
                    maCountSum = maCountSum + 1;
                }
                


                if (sd.StatusTrending == StrategyStatus.LongExecuted)
                {
                    if (zbData.price > sd.MaxPriceAfterBuy) sd.MaxPriceAfterBuy = zbData.price;
                }

                //if (zbData.direction != 1) continue; //必须是买盘成交

                totalObsVolume += zbData.volume;
                if (zbData.volume > maxSingleVolume) maxSingleVolume = zbData.volume;

                //if (zbData.price < zbMinPrice) //寻找最小值
                //{
                //    zbMinPrice = zbData.price; //寻找2分钟内的最低价格
                //    zbMinIndex = i; //寻找2分钟内最低价格的索

                //    if (sd.StatusTrending == StrategyStatus.New)  //以免影响卖出逻辑
                //    {
                //        totalObsVolume = 0;
                //        totalBuyVolume = 0; totalSellVolume = 0; totalBuyCount = 0; totalSellCount = 0; //清零，只计算从最低点开始的成交量
                //    }
                //}

                //if (zbData.price > zbMaxPrice) //寻找最大值
                //{
                //    zbMaxPrice = zbData.price; //寻找2分钟内的最低价格
                //    zbMaxIndex = i; //寻找2分钟内最低价格的索引
                //}


                //unable to compute huge order for now 
                //if (zbData.price * zbData.volume > 100000) //只统计大单
                //{
                //    if (zbData.direction == 1)
                //    {
                //        totalBuyCount = totalBuyCount + 1;
                //        totalBuyVolume = totalBuyVolume + zbData.volume;
                //    }
                //    else if (zbData.direction == -1)
                //    {
                //        totalSellCount = totalSellCount + 1;
                //        totalSellVolume = totalSellVolume + zbData.volume;
                //    }
                //}


                totalBuyCount = totalBuyCount+zbData.countB;
                totalBuyVolume = totalBuyVolume + zbData.volumeB;

                totalSellCount = totalSellCount + zbData.countS;
                totalSellVolume = totalSellVolume + zbData.volumeS;

                double pctChg = (sd.zbDataList[lastIndex].price - zbData.price) / zbData.price;

                if (zbData.price > 5)
                {
                    if (pctChg > pctChgThreshold) cPriceLevel = true;
                }
                else
                { if (pctChg > pctChgThreshold * 2) cPriceLevel = true; }

                if (cPriceLevel) //涨幅条件满足，检测其它条件
                {
                    double timeToLast1 = 0;
                    zbMinIndex = i;
                    timeToLast1 = Convert.ToDouble(ComputeIntTimeDiff(sd.zbDataList[zbMinIndex].timeStamp, sd.zbDataList[lastIndex].timeStamp)); //时间，以秒计

                    if (timeToLast1 < 10) continue; //跳涨，不符合要求，继续往前搜索，看是否上涨趋势

                    break;
                }

                //if (i > midIndex)
                //{
                //    double oneTickPctChg = (zbData.price - sd.zbDataList[i - 1].price) / sd.zbDataList[i - 1].price;

                //    ////条件1: 要求2分钟内没有某一笔拉升很大涨幅，即5块钱以下的不能有一笔的涨幅不超过0.5%，5块钱以上的不能有一笔超过0.25%
                //    if (zbMinPrice > 5)
                //    {
                //        if (oneTickPctChg > pctChgThreshold*0.5) cNoSuddenUp = false;
                //    }
                //    else
                //    { if (oneTickPctChg > 2*pctChgThreshold*0.5) cNoSuddenUp = false; }

                //    //最大回撤不超过涨幅的一半
                //    double pctWithDraw = (zbData.price - zbMaxPrice) / zbMaxPrice;
                //    if (pctWithDraw < - 0.5 * pctChgThreshold) cWithDraw = false;
                //}
            }
            //观察期遍历结束

            //计算CuScore
            if ((sd.zbDataList[lastIndex].timeStamp > 112955) && (ticker == "601628"))
            //if (sd.zbDataList[lastIndex].timeStamp > 112930)
                test = 0;

            if (sd.bComputeCuscore)
            {
                if (maCountSum == 0) throw new Exception("maSum=0");

                double cuT = ComputeIntTimeDiff(sd.BuyTime, sd.zbDataList[lastIndex].timeStamp);
                double cuScoreNew = 0;
                maPrice = maPriceSum / maCountSum;

                if (sd.zbDataList[lastIndex].timeStamp > sd.cuScoreLastTimeStamp)
                {

                    if ((sd.zbDataList[lastIndex].timeStamp > 93541) && (ticker == "600015"))
                        test = 0;

                    if (sd.cuScoreLastIndex == -1) //首次计算
                    {
                        cuScoreNew = (sd.zbDataList[lastIndex].price - maPrice) * cuT;
                    }
                    else
                    {
                        cuScoreNew = sd.CuScoreList[sd.cuScoreLastIndex] + (sd.zbDataList[lastIndex].price - maPrice) * cuT;
                    }

                    sd.CuScoreList.Add(cuScoreNew);
                    sd.cuScoreLastIndex = sd.cuScoreLastIndex + 1;
                    sd.cuScoreLastTimeStamp = sd.zbDataList[lastIndex].timeStamp;
                }

                sd.bCuScoreSell=false;
                if (sd.cuScoreLastIndex >60)
                {
                    double minScore = sd.CuScoreList[sd.cuScoreLastIndex-1];
                    for (int iCu = sd.cuScoreLastIndex-1; iCu > (sd.cuScoreLastIndex - 60); iCu--)
                    {
                        if (sd.CuScoreList[iCu] < minScore) minScore = sd.CuScoreList[iCu];
                    }
                    if (sd.CuScoreList[sd.cuScoreLastIndex] < minScore)
                    {
                        sd.bCuScoreSell = true;

                        //if (ticker == "600015") 
                        //    sd.bCuScoreSell = true;
                    }
                }
            }

            //检查单笔大单
            //if (((double)maxSingleVolume / (double)totalObsVolume) > 0.5) cSingleHugeOrder = false; ; // 一笔大单占交易量一半以上

            //检查趋势
            //for (int k = zbMinIndex; k < lastIndex - 1; k++)
            //{
            //    if ((sd.zbDataList[k + 1].price / sd.zbDataList[k].price - 1) < -0.002) cPriceTrend = false;

            //    if ((!cPriceTrend)&&(sd.zbDataList[lastIndex].timeStamp > 94550)) Thread.Sleep(1);
            //}


            //2 - Volume：2分钟成交量均超过过去一周平均水平的3倍。后续再加上成交笔数要求


            //if (Convert.ToDouble((lastIndex - zbMinIndex)) / (double)t < 0.3) return; //数据点过少，平均超过2秒才有一次成交

            double avgVolume1 = (double)sd.PrevVolume /(4*3600); //每秒平均成交量
            //double avgVolume1 = sd.avgVolTillNow; //每秒平均成交量

            timeToLast = Convert.ToDouble(ComputeIntTimeDiff(sd.zbDataList[firstIndex].timeStamp, sd.zbDataList[lastIndex].timeStamp)); //时间，以秒计
            if (totalObsVolume > volThreshold * avgVolume1 * timeToLast) cVolume = true;

            if ((totalBuyCount + totalSellCount) == 0) return; //盘前数据，无交易量

            //3 - 内外盘：2分钟内内外盘指标，买盘的交易量和交易笔数占比都要超过75%

            //if ((totalBuyVolume > bsThreshold * (totalBuyVolume + totalSellVolume)) && (totalBuyCount > bsThreshold * (totalBuyCount + totalSellCount)))
            if (totalBuyVolume > bsThreshold * (totalBuyVolume + totalSellVolume))
                cBuySellRatio = true;


            ////条件3为要求前10分钟振幅不能太高，不超过2.5%              
            //double range=(priceArray.Max() - priceArray.Min())/priceArray.Min();
            //if (range<0.025) c3=true;

            ////条件5要求当时涨幅不能超过0.05，且必须是上涨的 //此处为固定区间涨幅，并非区间振幅
            //double pctChgFromStart = (dd.tickData[obsEndIndex].LastPrice - startingPrice) / startingPrice;
            //if ((pctChgFromStart > 0) && (pctChgFromStart < 0.05)) c5 = true;


            if ((sd.zbDataList[lastIndex].timeStamp > 93739) && (ticker == "600015"))
                test = 0;


            //if (sd.zbDataList[lastIndex].direction != 1) return; //必须是买盘成交

            /*涨停撤单逻辑，暂停使用
            if (sd.zbDataList[lastIndex].price > sd.PrevClose*1.08)
            {
                FIXApi.OrderBookEntry entry = new OrderBookEntry();
                entry.strategies = TradingStrategies.StockTrending;
                entry.action = OrderAction.CancelLock;
                entry.ticker = ticker;
                entry.quantityListed = 0;//没有必要输入Quantity
                entry.priceListed = 0;
                entry.orderTime = sd.BuyTime;
                entry.orderDate = sd.DateStamp;

                sd.StatusTrending = StrategyStatus.Pending; //获得成交回报后再改变状态，避免重复执行
                Thread orderThread = new Thread(new ParameterizedThreadStart(tc.OrderSender_StockTrending));
                orderThread.IsBackground = true;
                orderThread.Start(entry);
            }
            */
 
            if (sd.StatusTrending == StrategyStatus.New) //尚未下过单
            {
                //判断是否需要下买单
                if ((cPriceLevel && cVolume && cBuySellRatio && cNoSuddenUp && cWithDraw && cPriceVol && cPriceTrend && cSingleHugeOrder) || bTestEngine)
                {
                    //到达此处即条件符合，下买单
                    sd.BuyTime = sd.zbDataList[lastIndex].timeStamp;//用逐笔时间，此处修改Dictionary，如发生线程冲突可能出现问题，后续把所有涉及修改zbDataList的逻辑全部去掉，engine中只读
                    sd.bComputeCuscore = true; //开始计算CuScore;
                    sd.CuScoreList = new List<double>();
                    sd.cuScoreLastIndex = -1;
                    sd.cuScoreLastTimeStamp = sd.BuyTime;

                    if ((tc.mode == TradeMode.Backtest) || (tc.mode == TradeMode.Debug))
                    {
                        sd.StatusTrending = StrategyStatus.LongExecuted;
                        if (sd.BuyTime > 144500) return; //14:45分后不再下单

                        FIXApi.OrderBookEntry entry = new OrderBookEntry();
                        entry.strategies = TradingStrategies.StockTrending;
                        entry.action = OrderAction.Buy;
                        entry.type = FIXApi.OrderType.CashBuy;
                        entry.ticker = ticker;
                        entry.quantityListed = sd.Quantity;
                        entry.priceListed = sd.zbDataList[lastIndex].price;

                        entry.lockPrice = Math.Round(sd.PrevClose * 1.09, 2);


                        entry.orderTime = sd.BuyTime;
                        entry.orderDate = sd.DateStamp;

                        tc.OrderSender_StockTrending(entry);

                    }
                    else if (tc.mode == TradeMode.Production)
                    {
                        if (sd.BuyTime > 144500) return; //14:45分后不再下单

                        FIXApi.OrderBookEntry entry = new OrderBookEntry();
                        entry.strategies = TradingStrategies.StockTrending;
                        entry.action = OrderAction.Buy;
                        //entry.type = FIXApi.OrderType.CreditMarginBuy; //融资买入
                        entry.type = FIXApi.OrderType.CashBuy; //信用账户现金买入
                        entry.ticker = ticker;

                        //double maxAmount = 20000;
                        //int quantityListed = 100;
                        //if (sd.PrevClose > 0)
                        //{
                        //    quantityListed = Math.Min(sd.MaxQuantity, Convert.ToInt32(maxAmount / sd.PrevClose / 100) * 100);
                        //}
                        //entry.quantityListed = quantityListed;
                        entry.quantityListed = sd.Quantity; //DEBUG

                        if (bTestEngine)
                            entry.priceListed = sd.AskPrices[4];//测试用，挂买五 DEBUG
                        else
                            entry.priceListed = sd.AskPrices[4];//直接按卖五下限价买单，以保证成交

                        entry.lockPrice = Math.Round(sd.PrevClose * 1.098, 2);

                        entry.orderTime = sd.BuyTime;
                        entry.orderDate = sd.DateStamp;

                        sd.StatusTrending = StrategyStatus.Pending; //获得成交回报后再改变状态，避免重复执行

                        OrderRouter(entry);
                    }
                }
            }
            else if (sd.StatusTrending == StrategyStatus.LongExecuted) //买单已下，下卖单
            {
                //判断是否需要下卖单
                int s = ComputeIntTimeDiff(sd.BuyTime, sd.zbDataList[lastIndex].timeStamp);

                int sellTime = 120;//最多5分钟后卖出

                if (s < sellTime) return; //5分钟内无论如何不卖

                if (!sd.bCuScoreSell) return;

                //if (bTestEngine) sellTime = 10; //测试下单用，10秒后即卖出 DEBUG

                //if ((s < sellTime) && (!sd.bCuScoreSell))
                //{
                //    //double bsSellThreshold = 0.5;
                //    //根据内外盘指标决定卖出时间，
                //    //if ((totalBuyVolume > bsSellThreshold * (totalBuyVolume + totalSellVolume)) && (totalBuyCount > bsSellThreshold * (totalBuyCount + totalSellCount))) //只要买盘足够大就继续持有
                //    //if (totalBuyVolume > bsSellThreshold * (totalBuyVolume + totalSellVolume)) 
                //    return;
                //}



                //条件通过，下卖单
                if ((tc.mode == TradeMode.Backtest) || (tc.mode == TradeMode.Debug))
                {
                    FIXApi.OrderBookEntry entry = new OrderBookEntry();
                    entry.strategies = TradingStrategies.StockTrending;
                    entry.action = OrderAction.Sell;
                    entry.type = FIXApi.OrderType.CashSell;
                    entry.ticker = ticker;
                    entry.quantityListed = sd.Quantity;
                    entry.priceListed = sd.zbDataList[lastIndex].price;
                    entry.orderTime = sd.zbDataList[lastIndex].timeStamp;
                    entry.orderDate = sd.DateStamp;

                    sd.StatusTrending = StrategyStatus.None; //不重复执行
                    //sd.StatusTrending = StrategyStatus.New; //重复执行
                    tc.OrderSender_StockTrending(entry);

                    tc.CalcPnL();
                }
                else if (tc.mode == TradeMode.Production)
                {

                    FIXApi.OrderBookEntry entry = new OrderBookEntry();
                    entry.strategies = TradingStrategies.StockTrending;
                    //entry.action = OrderAction.ShortSell;
                    //entry.type = FIXApi.OrderType.CreditMarginSell;
                    entry.action = OrderAction.Sell;
                    entry.type = FIXApi.OrderType.CashSell;
                    entry.ticker = ticker;
                    entry.quantityListed = sd.Quantity;
                    //entry.quantityListed = 0;//没有必要输入Quantity, 完全依赖锁券和预先锁券
                    //if (bTestEngine)
                    //    entry.priceListed = sd.AskPrices[5];//测试，按卖五挂单
                    //else
                    //    entry.priceListed = sd.AskPrices[0];//按卖一挂单
                    entry.priceListed = sd.BidPrices[4];//按卖五挂单，等同市价
                    entry.orderTime = sd.zbDataList[lastIndex].timeStamp; //SellTime
                    entry.orderDate = sd.DateStamp;

                    sd.StatusTrending = StrategyStatus.Pending; //获得成交回报后再改变状态，避免重复执行

                    OrderRouter(entry);

                }
            }
        }

        public void OrderRouter(object oEntry)
        {
            OrderBookEntry entry = (OrderBookEntry)oEntry;

            OrderRouter(entry);
          
        }
        public void OrderRouter(FIXApi.OrderBookEntry entry)
        {
            Thread orderThread = new Thread(new ParameterizedThreadStart(tc.OrderSender_StockTrending));
            orderThread.IsBackground = true;
            orderThread.Start(entry);

            string direction="";
            if (entry.action==OrderAction.Buy) direction="B";
            else if (entry.action==OrderAction.Sell) direction="S";
            else return;


            if(bCicc1)
            {
                //测试用
                //if (direction=="B")
                //    PythonWrapper.PutOrder(strCookie1, "B880303376", "0899090767", "511990", direction, 101, 100);
                //else
                //    PythonWrapper.PutOrder(strCookie1, "B880303376", "0899090767", "511990", direction, 99, 100);


                PythonWrapper.PutOrder(strCookie1, "B880303376", "0899090767", entry.ticker, direction, entry.priceListed, 100);
            }

            if (bCicc2)
            {
                //if (direction == "B")
                //    PythonWrapper.PutOrder(strCookie2, "B880374335", "0899094261", "511990", direction, 101, 100);
                //else
                //    PythonWrapper.PutOrder(strCookie2, "B880374335", "0899094261", "511990", direction, 99, 100);


                PythonWrapper.PutOrder(strCookie2, "B880374335", "0899094261", entry.ticker, direction, entry.priceListed, 100);
            }

            
        }
 
        public void RunReverseStrategy()
        {
            //实时是第一层每秒重新计算一次，第二层对每只股票代码循环，每只股票此时只需计算一次
            //PrevLockAll();

            bRunning = true;
            while (bRunning)
            {
                foreach (string ticker in dc.TickerList)  //注意不能直接对dict用foreach，否则任何修改都会抛出异常
                {
                    lock (this.dataDict)
                    {
                        if (!dataDict.Keys.Contains(ticker)) continue;

                        SingleStockData sd = dataDict[ticker];

                        if (sd.StatusReverse == StrategyStatus.None) continue; //该股票已交易过，当日只交易一次

                        if (((sd.ZBFirstIndex) < 0) || ((sd.ZBLastIndex) < 0)) continue;

                        if (sd.zbDataList[sd.ZBLastIndex].timeStamp < 93000) continue;

                        if (sd.zbDataList[sd.ZBLastIndex].timeStamp > 145900)
                        {
                            bRunning = false;
                            break;
                        }

                        int tDiff = ComputeIntTimeDiff(sd.zbDataList[sd.ZBFirstIndex].timeStamp, sd.zbDataList[sd.ZBLastIndex].timeStamp);

                        int obsTime = bTestEngine?2:600;

                        if ((sd.ZBFirstIndex == 0) && tDiff < obsTime) continue;//已读取数据长度不满2分钟 DEBUG

                        while (ComputeIntTimeDiff(sd.zbDataList[sd.ZBFirstIndex].timeStamp, sd.zbDataList[sd.ZBLastIndex].timeStamp) > 120) //保持内存中时间序列长度在2分钟左右
                        {
                            sd.zbDataList.RemoveAt(sd.ZBFirstIndex);
                            sd.ZBLastIndex = sd.ZBLastIndex - 1;

                            //sd.ZBFirstIndex++;
                        }
                        ComputeOneTick(ticker, sd.ZBFirstIndex, sd.ZBLastIndex);
                    }
                }

                if (tc.mode == TradeMode.Debug)
                    Thread.Sleep(10); //每秒计算一次
                else if (tc.mode == TradeMode.Production)
                    Thread.Sleep(1000); //每秒计算一次
            }
        }
        DateTime intTimeToDateTime(int intTime)
        {
            string strTime=intTime.ToString();
            
            return new DateTime(Convert.ToInt32(strTime.Substring(0,4)),
                Convert.ToInt32(strTime.Substring(5,2)),
                Convert.ToInt32(strTime.Substring(7,2)),
            Convert.ToInt32(strTime.Substring(9,2)),
            Convert.ToInt32(strTime.Substring(11,2)),
            Convert.ToInt32(strTime.Substring(13,2)));
        }

        public int ComputeIntTimeDiff(int t0, int t1) //93114
        {
            if ((t0 <= 113000) && (t1 >= 130000))
                return ComputeIntTimeDiff(t0, 113000) + ComputeIntTimeDiff(130000, t1);
            else
            {

                string strT0, strT1;
                if (t0.ToString().Length == 5) strT0 = "0" + t0.ToString(); else strT0 = t0.ToString();
                if (t1.ToString().Length == 5) strT1 = "0" + t1.ToString(); else strT1 = t1.ToString();
                int h0, h1, m0, m1, s0, s1;
                h0 = Convert.ToInt32(strT0.Substring(0, 2));
                h1 = Convert.ToInt32(strT1.Substring(0, 2));
                m0 = Convert.ToInt32(strT0.Substring(2, 2));
                m1 = Convert.ToInt32(strT1.Substring(2, 2));
                s0 = Convert.ToInt32(strT0.Substring(4, 2));
                s1 = Convert.ToInt32(strT1.Substring(4, 2));

                return (h1 - h0) * 3600 + (m1 - m0) * 60 + (s1 - s0);
            }

        }

    }
}