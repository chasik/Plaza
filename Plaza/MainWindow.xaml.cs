using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using P2ClientGateMTA;
using System.IO;

namespace Plaza
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool m_stop = false;
        public CP2Connection connect;
        public CP2DataStream m_streamAggregates;
        public CP2DataStream m_streamTrades;

        StreamWriter m_logFile;
        StreamWriter m_saveRevFile;
        StreamWriter m_saveDealFile;

        string m_saveRev = "SaveRev.txt";
        string m_saveDeal = "SaveDeal.txt";

        Int64 curr_rev = 0;
        Int64 curr_rev_deal = 0;
        /*
         * Идентификаторы потоков
         */
        string streamAggregatesID = "FORTS_FUTAGGR20_REPL";
        string streamTradesID = "FORTS_FUTTRADE_REPL";
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                connect = new CP2ConnectionClass();
                connect.Host = "localhost";
                connect.Port = 4001;
                connect.AppName = "myplazait";
                connect.ConnectionStatusChanged += delegate(CP2Connection _conn, TConnectionStatus _newStatus)
                {
                    //MessageBox.Show(_conn.Status.ToString() + " - newstatus: " + _newStatus);
                    MessageBox.Show("newstatus: " + _newStatus + " threadid: " + Thread.CurrentThread.ManagedThreadId);
                };

                if (File.Exists(m_saveRev))
                {
                    using (StreamReader sr = new StreamReader(m_saveRev))
                    {
                        while (sr.Peek() >= 0)
                        {
                            curr_rev = Convert.ToInt64(sr.ReadLine());
                        }
                    }
                }

                if (File.Exists(m_saveDeal))
                {
                    using (StreamReader sr = new StreamReader(m_saveDeal))
                    {
                        while (sr.Peek() >= 0)
                        {
                            string buf = sr.ReadLine();
                            if (buf.StartsWith("replRev"))
                                curr_rev_deal = Int64.Parse(buf.Split('=')[1]);
                        }
                    }
                }


                // создаем объект "входящий поток репликации" для потока агрегированых заявок
                m_streamAggregates = new CP2DataStreamClass();
                m_streamAggregates.DBConnString = "";
                m_streamAggregates.type = TRequestType.RT_COMBINED_DYNAMIC; // RT_REMOTE_ONLINE
                m_streamAggregates.StreamName = streamAggregatesID;
                m_streamAggregates.TableSet = new CP2TableSetClass();
                m_streamAggregates.TableSet.InitFromIni("orders_aggr.ini", "");
                m_streamAggregates.TableSet.set_rev("orders_aggr", curr_rev + 1);

                // создаем объект "входящий поток репликации" для потока агрегированых заявок
                m_streamTrades = new CP2DataStreamClass();
                m_streamTrades.DBConnString = "";
                m_streamTrades.type = TRequestType.RT_COMBINED_DYNAMIC; // RT_REMOTE_ONLINE
                m_streamTrades.StreamName = streamTradesID;
                m_streamTrades.TableSet = new CP2TableSetClass();
                m_streamTrades.TableSet.InitFromIni2("forts_scheme.ini", "FutTrade");
                m_streamTrades.TableSet.set_rev("deal", curr_rev_deal + 1);
                
                // регистрируем интерфейсы обратного вызова для получения данных
                IP2DataStreamEvents_StreamStateChangedEventHandler StateHandler = new IP2DataStreamEvents_StreamStateChangedEventHandler(StreamStateChanged);
                m_streamAggregates.StreamStateChanged += StateHandler;
                m_streamTrades.StreamStateChanged += StateHandler;

                IP2DataStreamEvents_StreamDataInsertedEventHandler InsHandler = new IP2DataStreamEvents_StreamDataInsertedEventHandler(StreamDataInserted);
                m_streamAggregates.StreamDataInserted += InsHandler;
                m_streamTrades.StreamDataInserted += InsHandler;

                IP2DataStreamEvents_StreamDataDeletedEventHandler DelHandler = new IP2DataStreamEvents_StreamDataDeletedEventHandler(StreamDataDeleted);
                m_streamAggregates.StreamDataDeleted += DelHandler;
                m_streamTrades.StreamDataDeleted += DelHandler;

                IP2DataStreamEvents_StreamLifeNumChangedEventHandler LifeNumHandler = new IP2DataStreamEvents_StreamLifeNumChangedEventHandler(StreamLifeNumChanged);
                m_streamAggregates.StreamLifeNumChanged += LifeNumHandler;
                m_streamTrades.StreamLifeNumChanged += LifeNumHandler;


                Run();
            }
            catch (Exception ee) 
            {
                MessageBox.Show(ee.Message);
            }
        }

        // ГЛАВНЫЙ ЦИКЛ
        public void Run()
        {

            while (!m_stop)
            {
                try
                {
                    // создаем соединение с роутером
                    connect.Connect();
                    try
                    {
                        while (!m_stop)
                        {
                            try
                            {
                                if (m_streamAggregates.State == TDataStreamState.DS_STATE_ERROR ||
                                    m_streamAggregates.State == TDataStreamState.DS_STATE_CLOSE)
                                {
                                    if (m_streamAggregates.State == TDataStreamState.DS_STATE_ERROR)
                                    {
                                        m_streamAggregates.Close();
                                    }
                                    // открываем поток репликации
                                    m_streamAggregates.TableSet.set_rev("orders_aggr", curr_rev + 1);
                                    m_streamAggregates.Open(connect);
                                }

                                if (m_streamTrades.State == TDataStreamState.DS_STATE_ERROR ||
                                    m_streamTrades.State == TDataStreamState.DS_STATE_CLOSE)
                                {
                                    if (m_streamTrades.State == TDataStreamState.DS_STATE_ERROR)
                                    {
                                        m_streamTrades.Close();
                                    }
                                    // открываем поток репликации
                                    m_streamTrades.TableSet.set_rev("deal", curr_rev_deal + 1);
                                    m_streamTrades.Open(connect);
                                }
                            }
                            catch (System.Runtime.InteropServices.COMException e)
                            {
                                LogWriteLine("Exception {0} {1:X}", e.Message, e.ErrorCode);
                            }
                            uint cookie;
                            // обрабатываем пришедшее сообщение. Обработка идет в интерфейсах обратного вызова
                            connect.ProcessMessage(out cookie, 100);

                        }
                    }

                    catch (System.Runtime.InteropServices.COMException e)
                    {
                        LogWriteLine("Exception {0} {1:X}", e.Message, e.ErrorCode);
                    }

                    if (m_streamAggregates.State != TDataStreamState.DS_STATE_CLOSE)
                    {
                        try
                        {
                            m_streamAggregates.Close();
                        }
                        catch (System.Runtime.InteropServices.COMException e)
                        {
                            LogWriteLine("Exception {0} {1:X}", e.Message, e.ErrorCode);
                        }
                    }

                    if (m_streamTrades.State != TDataStreamState.DS_STATE_CLOSE)
                    {
                        try
                        {
                            m_streamTrades.Close();
                        }
                        catch (System.Runtime.InteropServices.COMException e)
                        {
                            LogWriteLine("Exception {0} {1:X}", e.Message, e.ErrorCode);
                        }
                    }

                    connect.Disconnect();
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    LogWriteLine("Exception {0} {1:X}", e.Message, e.ErrorCode);
                }
                catch (System.Exception e)
                {
                    LogWriteLine("System Exception {0} {1}", e.Message, e.Source);
                }
            }
        }

        void StreamStateChanged(CP2DataStream stream, TDataStreamState newState)
        {
            String state = DateTime.Now.ToString() + " Stream " + stream.StreamName + " state: ";
            switch (newState)
            {
                case TDataStreamState.DS_STATE_CLOSE:
                    state += "CLOSE";
                    //m_opened = false;
                    break;
                case TDataStreamState.DS_STATE_CLOSE_COMPLETE:
                    state += "CLOSE_COMPLETE";
                    break;
                case TDataStreamState.DS_STATE_ERROR:
                    state += "ERROR";
                    //m_opened = false;
                    break;
                case TDataStreamState.DS_STATE_LOCAL_SNAPSHOT:
                    state += "LOCAL_SNAPSHOT";
                    break;
                case TDataStreamState.DS_STATE_ONLINE:
                    state += "ONLINE";
                    break;
                case TDataStreamState.DS_STATE_REMOTE_SNAPSHOT:
                    state += "REMOTE_SNAPSHOT";
                    break;
                case TDataStreamState.DS_STATE_REOPEN:
                    state += "REOPEN";
                    break;
            }
            LogWriteLine(state);
        }

        //вставка записи
        void StreamDataInserted(CP2DataStream stream, String tableName, CP2Record rec)
        {
            try
            {
                LogWriteLine(DateTime.Now.ToString() + " Insert " + tableName + " StreamName: " + stream.StreamName + "; TableSet.Count = " + stream.TableSet.Count.ToString());

                // Пришел поток FORTS_FUTAGGR20_REPL
                if (stream.StreamName == streamAggregatesID)
                {
                    SaveRev(rec.GetValAsVariantByIndex(1));
                    curr_rev = rec.GetValAsLongByIndex(1);
                    uint count = rec.Count;
                    for (uint i = 0; i < count; ++i)
                    {
                        if (i != count - 1)
                        {
                            LogWrite(rec.GetValAsStringByIndex(i) + ";");
                        }
                        else
                        {
                            LogWriteLine(rec.GetValAsStringByIndex(i));
                        }
                    }
                }

                // Пришел поток FORTS_FUTTRADE_REPL
                if (stream.StreamName == streamTradesID)
                {
                    string Fields = m_streamTrades.TableSet.get_FieldList("deal");
                    curr_rev_deal = rec.GetValAsLongByIndex(1);
                    for (uint i = 0; i < Fields.Split(',').Length; i++)
                    {
                        string Field = Fields.Split(',')[i];
                        string Value = "";
                        try
                        {
                            Value = rec.GetValAsString(Field);
                            SaveDeal(DateTime.Now.ToString() + " " + Fields.Split(',')[i], Value);
                        }
                        catch (System.Exception e)
                        {

                        }
                    }
                    m_saveDealFile.WriteLine("");
                    m_saveDealFile.Flush();
                }

            }
            catch (System.Exception e)
            {
                LogWriteLine("!!!" + e.Message + "!!!" + e.Source);
            }
        }


        void StreamDataDeleted(CP2DataStream stream, String tableName, Int64 Id, CP2Record rec)
        {
            SaveRev(rec.GetValAsVariantByIndex(1));
            LogWriteLine(DateTime.Now.ToString() + " Delete " + tableName + " " + Id);
        }


        void StreamLifeNumChanged(CP2DataStream stream, int lifeNum)
        {
            if (stream.StreamName == "FORTS_FUTAGGR20_REPL")
            {
                m_streamAggregates.TableSet.LifeNum = lifeNum;
                m_streamAggregates.TableSet.SetLifeNumToIni("orders_aggr.ini");
            }
            if (stream.StreamName == "FORTS_FUTTRADE_REPL")
            {
                m_streamTrades.TableSet.LifeNum = lifeNum;
                m_streamTrades.TableSet.SetLifeNumToIni("forts_scheme.ini");
            }
        }

        void LogWriteLine(string s, params object[] arg)
        {
            if (m_logFile == null)
            {
                m_logFile = new StreamWriter("P2SimpleGate2Client.log", false, System.Text.Encoding.Unicode);
            }
            m_logFile.WriteLine(s, arg);
            m_logFile.Flush();
        }

        void LogWrite(string s, params object[] arg)
        {
            if (m_logFile == null)
            {
                m_logFile = new StreamWriter("P2SimpleGate2Client.log", false, System.Text.Encoding.Unicode);
            }
            m_logFile.Write(s, arg);
            m_logFile.Flush();
        }

        void SaveRev(object rev)
        {

            if (m_saveRevFile == null)
            {
                m_saveRevFile = new StreamWriter(m_saveRev, false, System.Text.Encoding.Unicode);
            }
            m_saveRevFile.WriteLine(rev.ToString());
            m_saveRevFile.Flush();
        }

        void SaveDeal(string Field, string Value)
        {
            if (m_saveDealFile == null)
            {
                m_saveDealFile = new StreamWriter(m_saveDeal, false, System.Text.Encoding.Unicode);
            }
            m_saveDealFile.WriteLine(Field + " = " + Value);
            m_saveDealFile.Flush();
        }

    }
}
