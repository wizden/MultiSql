using System.Windows;
using NLog;

namespace MultiSql
{
    /// <summary>
    /// Interaction logic for ConnectServer.xaml
    /// </summary>
    public partial class ConnectServer : Window
    {

        // TODO: Change to allow loading of connections into controls on screen.

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public ConnectServer()
        {
            Logger.Debug("Opening Connection window.");
            InitializeComponent();
        }

    }
}
