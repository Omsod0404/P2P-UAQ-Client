using P2P_UAQ_Client.Core;
using P2P_UAQ_Client.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace P2P_UAQ_Client.View
{
    /// <summary>
    /// Lógica de interacción para Client_chat.xaml
    /// </summary>
    public partial class ClientChatView : Window
    {
        public ClientChatView()
        {
            InitializeComponent();
			DataContext = new ClientChatViewModel();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
            CoreHandler.Instance.Dispose();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			CoreHandler.Instance.Dispose();
		}
	}
}
