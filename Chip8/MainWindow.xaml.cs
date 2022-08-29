using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Timer = System.Timers.Timer;

namespace Chip8
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Timer renderLoopTimer;
        src.Display virtualDisplay;
        src.Chip8 chip8;
        src.Keyboard keyboard;
        private readonly CancellationTokenSource _shutDown = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();

            this.virtualDisplay = new src.Display();
            this.keyboard = new src.Keyboard(this);
            this.chip8 = new src.Chip8(virtualDisplay, keyboard);
            display.Source = this.virtualDisplay.writeableImg;

            this.Closed += (s, e) => this._shutDown.Cancel();

            RunMainLoop();
            renderLoopTimer = new Timer(17);
            renderLoopTimer.Elapsed += new ElapsedEventHandler(RunRenderLoop);
            renderLoopTimer.Start();
        }

        private void RunMainLoop()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    chip8.Loop();
                    Thread.Sleep(2);
                }
            });
        }

        private void RunRenderLoop(object? source, ElapsedEventArgs e)
        {
            if (!this._shutDown.IsCancellationRequested)
                display.Dispatcher.Invoke(() => this.virtualDisplay.render());
        }
    }
}
