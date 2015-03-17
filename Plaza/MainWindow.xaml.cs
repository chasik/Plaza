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

namespace Plaza
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CP2Connection connect;
        CP2DataStream m_streamAggregates;
        CP2DataStream m_streamTrades;
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
/*
                                 // создаем объект "входящий поток репликации" для потока агрегированых заявок
                                m_streamAggregates = new CP2DataStreamClass();
                                m_streamAggregates.DBConnString = "";
                                m_streamAggregates.type = TRequestType.RT_COMBINED_DYNAMIC;
                                m_streamAggregates.StreamName = streamAggregatesID;
                                m_streamAggregates.TableSet = new CP2TableSetClass();                
                                m_streamAggregates.TableSet.InitFromIni("orders_aggr.ini", "");
                                m_streamAggregates.TableSet.set_rev("orders_aggr", curr_rev + 1);

                                // создаем объект "входящий поток репликации" для потока агрегированых заявок
                                m_streamTrades = new CP2DataStreamClass();
                                m_streamTrades.DBConnString = "";
                                m_streamTrades.type = TRequestType.RT_COMBINED_DYNAMIC;
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
                 */

                connect.Connect();
            }
            catch (Exception ee) 
            {
                MessageBox.Show(ee.Message);
            }
        }
    }
}
