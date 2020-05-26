using System;
using System.Windows.Interop;

namespace GTAMissionDownloader.Views
{
    /// <summary>
    /// Interaction logic for TsView.xaml
    /// </summary>
    public partial class TsView
    {
        public TsView()
        {
            InitializeComponent();
        }

        //No window move
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MOVE = 0xF010;
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_SYSCOMMAND:
                    int command = wParam.ToInt32() & 0xfff0;
                    if (command == SC_MOVE)
                        handled = true;
                    break;
            }
            return IntPtr.Zero;
        }

        public void Window_SourceInitialized(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(WndProc);
        }
    }
}
