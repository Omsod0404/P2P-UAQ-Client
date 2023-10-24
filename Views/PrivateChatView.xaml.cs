using P2P_UAQ_Client.Models;
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

namespace P2P_UAQ_Client.Views
{
    /// <summary>
    /// Lógica de interacción para Client_chat_private.xaml
    /// </summary>
    public partial class PrivateChatView : Window
    {
        private PrivateChatViewModel _viewModel;
        public PrivateChatView(PrivateChatViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.SetWindowReference(this);
            InitializeComponent();
			DataContext = viewModel;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
            _viewModel.RequestCloseRoom();
            this.Close();
		}

		private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}
	}
}
