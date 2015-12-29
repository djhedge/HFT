using System;
using System.Collections.Generic;
using System.Text;

namespace FIXApi
{
    public class SingleStockData
    {
        //数据区，不同策略间共享数据
        public int TimeStamp; //eg: 93508, 仅用于传递数据用
        public int DateStamp; //20141128
        public double LastPrice;


        public double[] BidPrices;
        public double[] AskPrices;
        public uint[] BidVol;
        public uint[] AskVol;

        public List<ZBTickData> zbDataList;

        public int ZBFirstIndex, ZBLastIndex;
        public int totalVolTillNow;
        public double avgVolTillNow;

        //交易参数
        public int Quantity = 1; //后续改为从数据库读取
        public int MaxQuantity = 1;
        public bool bPrevLock = false; //是否预先锁过券
        public int PrevLockQuantity; //预先锁券数量

        public double ShortPriceListed;
        public int ShortQuantityListed;

        public double LongPriceListed;
        public int LongQuantityListed;

        public bool bComputeCuscore=false;
        public List<double> CuScoreList;
        public int cuScoreLastIndex=-1;
        public int cuScoreLastTimeStamp;
        public bool bCuScoreSell = false;
        public int prevTimeStamp=0;

        //控制区,不同策略使用不同的控制变量
        //CBFishing 所用参数
        public StrategyStatus StatusCBFishing;
        public double CBBuyPriceOffset = 0.005;
        public double CBSellPriceOffset = 0.001;

        //Stockfishing所用参数
        public StrategyStatus StatusStockFishing;

        //Trending所用参数
        public StrategyStatus StatusTrending;
        public int BuyTime; //买单下达时间
        public double MaxPriceAfterBuy;//下单后最高价

        public StrategyStatus StatusReverse;

        //股票基本信息，前收盘价、交易量等，从万得获取，用于计算
        public double PrevClose;
        public Int64  PrevVolume;
        
        public bool bAskPriceChanged;//用于通知ShortSell重新挂单

        //public long TotalVolume;
        //public long Volume;
        //public long BuyVolume; //主动买量
        //public long SellVolume; //主动卖量
        //为未来加入lvl2数据留空间

        public SingleStockData(string _ticker)
        {
            BidPrices = new double[10];
            AskPrices = new double[10];
            BidVol = new uint[10];
            AskVol = new uint[10];

            StatusCBFishing = StrategyStatus.None;
            StatusStockFishing = StrategyStatus.None;
            StatusTrending = StrategyStatus.None;

            bAskPriceChanged = false;

            zbDataList = new List<ZBTickData>();

            ZBFirstIndex = -1;
            ZBLastIndex = -1;
        }
    }

    public enum StrategyStatus { None, New, ShortListedOnly, SellListedOnly, LongListedOnly, BothListed, ShortExecuted, LongExecuted, ShortCovered, LongCovered, Pending}

    public struct ZBTickData
    {
        public int timeStamp;
        public double price;
        public int volume;
        public double priceB;
        public double priceS;
        public int volumeB;
        public int volumeS;

        //以下为按秒、方向累加后的逐笔数据
        public double priceTotalB; //价格累加，不同方向单独统计
        public int countB; //笔数累加，不同方向单独统计
        public double priceTotalS; //价格累加，不同方向单独统计
        public int countS; //笔数累加，不同方向单独统计


    }
    public class TickData
    {
        public int TimeStamp; //eg: 93508
        public double LastPrice;
        public double Bid1;
        public double Ask1;
        public long TotalVolume;
        public long Volume;
        public long BuyVolume; //主动买量
        public long SellVolume; //主动卖量
        //public double[] BidPrices;
        //public double[] AskPrices;
        //public int[] BidShares;
        //public int[] AskShares;
        //to add per tick data and Bid/Ask details

        public TickData(int inTime, double inPrice)
        {
            TimeStamp = inTime;
            LastPrice = inPrice;
        }

        public TickData(int inTime, double inPrice, double bid1, double ask1, long totalVolume, long volume)
        {
            TimeStamp = inTime;
            LastPrice = inPrice;
            Bid1 = bid1;
            Ask1 = ask1;
            TotalVolume = totalVolume;
            Volume = volume;
        }
    }

    public class DailyData
    {
        public int DateStamp; //eg: 141025
        public int FirstTimeIndex, LastTimeIndex;
        public List<TickData> tickData;
        public double prevClose;
        public SignalStatus status;
        public int BuyTime;

        public DailyData(int inDate)
        {
            DateStamp = inDate;
            FirstTimeIndex = 0; LastTimeIndex = -1;
            status = SignalStatus.New;
            tickData = new List<TickData>();
        }


        public void AddToEnd(int inTime, double inPrice)
        {
            LastTimeIndex++;
            TickData td = new TickData(inTime, inPrice);
            tickData.Insert(LastTimeIndex, td);
        }

        public void AddToEnd(TickData td)
        {
            LastTimeIndex++;
            tickData.Insert(LastTimeIndex, td);
        }

        public double GetLastestPrice()
        {
            if (LastTimeIndex >= 0)
                return tickData[LastTimeIndex].LastPrice;
            else return -1;
        }

        public double GetLastestAskPrice()
        {
            if (LastTimeIndex >= 0)
                return tickData[LastTimeIndex].Ask1;
            else return -1;
        }
    }

    public enum SignalStatus { New, Bought, Sold }

    public class StockData
    {

        public string Ticker;
        public DailyData currentPrices;
        public List<DailyData> DailyDataList;
        public int FirstDateIndex, LastDateIndex;

        public StockData(string inTicker)
        {
            Ticker = inTicker;
            FirstDateIndex = 0; LastDateIndex = -1;
            DailyDataList = new List<DailyData>();
        }

        public int AddToEnd(DailyData dd)
        {
            LastDateIndex++;
            DailyDataList.Insert(LastDateIndex, dd);

            return LastDateIndex;

        }

    }
}
