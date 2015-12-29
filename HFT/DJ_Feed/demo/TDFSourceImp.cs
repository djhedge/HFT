using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDFAPI;
using System.IO;

namespace CSDemo
{
    class TDFSourceImp : TDFDataSource
    {
        // 非代理方式连接和登陆服务器
        public TDFSourceImp(TDFOpenSetting openSetting)
            : base(openSetting)
        {
            ShowAllData = false;
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
                Console.WriteLine("{0}！server:{1}:{2},{3},{4}, connect id:{5}", strPrefix, connectResult.Ip, connectResult.Port, connectResult.Username, connectResult.Password, connectResult.ConnectID);
            }
            else if (msg.MsgID == TDFMSGID.MSG_SYS_LOGIN_RESULT)
            {
                TDFLoginResult loginResult = msg.Data as TDFLoginResult;
                if (loginResult.LoginResult)
                {
                    //登陆结果
                    Console.WriteLine("登陆成功，市场个数:{0}:", loginResult.Markets.Length);
                    for (int i = 0; i < loginResult.Markets.Length; i++)
                    {
                        Console.WriteLine("market:{0}, dyn-date:{1}", loginResult.Markets[i], loginResult.DynDate[i]);
                    }
                }
                else
                {
                    Console.WriteLine("登陆失败！info:{0}", loginResult.Info);
                }
            }
            else if (msg.MsgID == TDFMSGID.MSG_SYS_CODETABLE_RESULT)
            {
                //接收代码表结果
                TDFCodeResult codeResult = msg.Data as TDFCodeResult;
                Console.WriteLine("获取到代码表, info:{0}，市场个数:{1}", codeResult.Info, codeResult.Markets.Length);
                for (int i = 0; i < codeResult.Markets.Length; i++)
                {
                    Console.WriteLine("market:{0}, date:{1}, code count:{2}", codeResult.Markets[i], codeResult.CodeDate[i], codeResult.CodeCount[i]);
                }



                //FileStream fsFile = new FileStream(@"d:\log.txt", FileMode.OpenOrCreate);
                //StreamWriter swWriter = new StreamWriter(fsFile);
                ////寫入數據
                ////swWriter.WriteLine("Hello Wrold.");
                ////swWriter.WriteLine("It is now {0}", DateTime.Now.ToLongDateString());

                ////客户端请自行保存代码表，本处演示怎么获取代码表内容
                //TDFCode[] codeArr;
                //GetCodeTable("", out codeArr);
                ////Console.WriteLine("接收到{0}项代码!, 输出前100项", codeArr.Length);
                ////for (int i = 0; i < 100 && i < codeArr.Length; i++)
                //for (int i = 0; i < codeArr.Length; i++)
                //{
                //    if (codeArr[i].Type >= 0x90 && codeArr[i].Type <= 0x95)
                //    {
                //        // 期权数据
                //        TDFOptionCode code = new TDFOptionCode();
                //        var ret = GetOptionCodeInfo(codeArr[i].WindCode, ref code);
                //        PrintHelper.PrintObject(code);
                //        swWriter.WriteLine(codeArr[i].WindCode + "\t" + codeArr[i].Code + "\t" + codeArr[i].CNName + "\t" + codeArr[i].Type);
                //    }
                //    else
                //    {
                //        //PrintHelper.PrintObject(codeArr[i]);
                //        swWriter.WriteLine(codeArr[i].WindCode + "\t" + codeArr[i].Code + "\t" + codeArr[i].CNName + "\t" + codeArr[i].Type);
                //    }
                //}

                //swWriter.Close();
            }
            else if (msg.MsgID == TDFMSGID.MSG_SYS_QUOTATIONDATE_CHANGE)
            {
                //行情日期变更。
                TDFQuotationDateChange quotationChange = msg.Data as TDFQuotationDateChange;
                Console.WriteLine("接收到行情日期变更通知消息，market:{0}, old date:{1}, new date:{2}", quotationChange.Market, quotationChange.OldDate, quotationChange.NewDate);
            }
            else if (msg.MsgID == TDFMSGID.MSG_SYS_MARKET_CLOSE)
            {
                //闭市消息
                TDFMarketClose marketClose = msg.Data as TDFMarketClose;
                Console.WriteLine("接收到闭市消息, 交易所:{0}, 时间:{1}, 信息:{2}", marketClose.Market, marketClose.Time, marketClose.Info);
            }
            else if (msg.MsgID == TDFMSGID.MSG_SYS_HEART_BEAT)
            {
                //心跳消息
                Console.WriteLine("接收到心跳消息!");
            }
        }
        
        //重载OnRecvDataMsg方法，接收行情数据
        // 请注意：
        //  1. 不要在这个函数里做耗时操作
        //  2. 只在这个函数里做数据获取工作 -- 将数据复制到其它数据缓存区，由其它线程做业务逻辑处理
        public override void OnRecvDataMsg(TDFMSG msg)
        {

            if (msg.MsgID == TDFMSGID.MSG_DATA_MARKET)
            {
                //行情消息
                TDFMarketData[] marketDataArr = msg.Data as TDFMarketData[];

                foreach (TDFMarketData data in marketDataArr)
                {
                    if (!ShowAllData)
                        //Console.WriteLine("行情消息 - 代码:{0}, 时间:{1}, 价格:{2}, 交易量:{3} ", data.WindCode, data.Time, data.Match, data.Volume);
                        Console.WriteLine("行情消息 - 代码:{0}, 时间:{1}, 价格:{2}, 交易量:{3} ", DateTime.Now.Second.ToString(), data.Time, data.Match, data.Volume);
                    else
                        PrintHelper.PrintObject(data);
                    return; //Let's only show the first element.
                }                
            }
            else if (msg.MsgID == TDFMSGID.MSG_DATA_FUTURE)
            {
                //期货行情消息
                TDFFutureData[] futureDataArr = msg.Data as TDFFutureData[];
                foreach (TDFFutureData data in futureDataArr)
                {
                    if (!ShowAllData)
                        Console.WriteLine(data.WindCode);
                    else
                        PrintHelper.PrintObject(data);
                    return; //Let's only show the first element.
                }
            }
            else if (msg.MsgID == TDFMSGID.MSG_DATA_INDEX)
            {
                //指数消息
                TDFIndexData[] indexDataArr = msg.Data as TDFIndexData[];
                foreach (TDFIndexData data in indexDataArr)
                {
                    if (!ShowAllData)
                        Console.WriteLine("指数行情 - 代码:{0}, 时间:{1}, 价格:{2}, 交易量:{3} ", DateTime.Now.Second.ToString(), data.Time, data.LastIndex, data.TotalVolume);
                    else
                        PrintHelper.PrintObject(data);
                    return; //Let's only show the first element.
                }
                
            }
            else if(msg.MsgID == TDFMSGID.MSG_DATA_TRANSACTION)
            {
                //逐笔成交

                TDFTransaction[] transactionDataArr = msg.Data as TDFTransaction[];
                foreach (TDFTransaction data in 
                    transactionDataArr)
                {
                    if (!ShowAllData)
                        Console.WriteLine("逐笔成交 - 代码:{0}, 时间:{1}, 价格:{2}, 交易量:{3} ", DateTime.Now.Second.ToString(), data.Time, data.Price, data.Volume);
                    else
                        PrintHelper.PrintObject(data);
                    return; //Let's only show the first element.
                }
            }
            else if(msg.MsgID == TDFMSGID.MSG_DATA_ORDER)
            {
                //逐笔委托
                TDFOrder[] orderDataArr = msg.Data as TDFOrder[];
                foreach (TDFOrder data in orderDataArr)
                {
                    if (!ShowAllData)
                        Console.WriteLine("逐笔委托 - 代码:{0}, 时间:{1}, 价格:{2}, 交易量:{3} ", DateTime.Now.Second.ToString(), data.Time, data.Price, data.Volume);
                    else
                        PrintHelper.PrintObject(data);
                    return; //Let's only show the first element.
                }
            }
            else if(msg.MsgID == TDFMSGID.MSG_DATA_ORDERQUEUE)
            {
                //委托队列
                TDFOrderQueue[] orderQueueArr = msg.Data as TDFOrderQueue[];
                foreach (TDFOrderQueue data in orderQueueArr)
                {
                    if (!ShowAllData)
                        Console.WriteLine("委托队列 - 代码:{0}, 时间:{1}, 价格:{2}, 第一单:{3} ", DateTime.Now.Second.ToString(), data.Time, data.Price, data.ABVolume[0]);
                    else
                        PrintHelper.PrintObject(data);
                    return; //Let's only show the first element.
                }
            }
        }
    }
}
