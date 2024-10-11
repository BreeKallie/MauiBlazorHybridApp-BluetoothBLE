namespace MauiBlazorHybridApp
{
    public partial class MainPage : ContentPage
    {
        private readonly BluetoothService _bluetoothService;
        public MainPage()
        {
            InitializeComponent();
            _bluetoothService = new BluetoothService();
        }
    }
}
