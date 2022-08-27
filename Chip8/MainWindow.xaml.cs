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
        Chip8.src.Chip8 chip8;
        private readonly CancellationTokenSource _shutDown = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();
            this.chip8 = new src.Chip8(display);
            display.Source = this.chip8.display.writeableImg;
            Task.Run(() =>
            {
                while (true)
                {
                    chip8.loop();
                    Task.Delay(1);
                }
            });

            this.Closed += (s, e) => this._shutDown.Cancel();
            renderLoopTimer = new Timer(17);
            renderLoopTimer.Start();
            renderLoopTimer.Elapsed += new ElapsedEventHandler(runRenderLoop);
        }

        private void runRenderLoop(object? source, ElapsedEventArgs e)
        {
            if (!this._shutDown.IsCancellationRequested)
                display.Dispatcher.Invoke(() => this.chip8.render());
        }
    }
}
