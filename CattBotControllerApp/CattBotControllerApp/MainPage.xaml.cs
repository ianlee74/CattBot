using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CattBotControllerApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int SAFETYLIGHT_PIN = 3;
        private const int CATAPULT_UP_PIN = 19;
        private const int CATAPULT_DOWN_PIN = 26;
        private const int CATAPULT_CONTROL_PIN = 20;

        private GpioPin _safetyLightPin;
        private GpioPinValue _safetyLightState;
        private GpioPin _catapultControlButtonPin;
        private GpioPin _catapultUpPin;
        private GpioPin _catapultDownPin;
        private CatapultMovement _lastCatapultMovement = CatapultMovement.Off;

        private void InitGpio()
        {
            var gpio = GpioController.GetDefault();
            if (gpio == null)
            {
                Debug.WriteLine("No GPIO controller found on the device!");
                return;
            }
            // Initialize safety light pin.
            _safetyLightPin = gpio.OpenPin(SAFETYLIGHT_PIN);
            _safetyLightPin.Write(GpioPinValue.Low);
            _safetyLightPin.SetDriveMode(GpioPinDriveMode.Output);
            // Initialize catapult control button.
            _catapultControlButtonPin = gpio.OpenPin(CATAPULT_CONTROL_PIN);
            _catapultControlButtonPin.SetDriveMode(GpioPinDriveMode.Input);
            _catapultControlButtonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
            _catapultControlButtonPin.ValueChanged += CatapultControlButtonPinOnValueChanged;
            // Initialize catapult up pin.
            _catapultUpPin = gpio.OpenPin(CATAPULT_UP_PIN);
            _catapultUpPin.Write(GpioPinValue.High);
            _catapultUpPin.SetDriveMode(GpioPinDriveMode.Output);
            // Initialize catapult down pin.
            _catapultDownPin = gpio.OpenPin(CATAPULT_DOWN_PIN);
            _catapultDownPin.Write(GpioPinValue.High);
            _catapultDownPin.SetDriveMode(GpioPinDriveMode.Output);

            Debug.WriteLine("GPIO initialized.");
        }

        public MainPage()
        {
            this.InitializeComponent();
            InitGpio();

            // Blink the safety light for a moment to indicate that its ready for control.
            TurnSafetyLightOn();
            var t = Task.Run(async delegate {
                await Task.Delay(TimeSpan.FromSeconds(3));
                TurnSafetyLightOff();
            });
            t.Wait();

            Debug.WriteLine("Ready!");
        }

        private void CatapultControlButtonPinOnValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (args.Edge == GpioPinEdge.RisingEdge)
            {
                MoveCatapult(CatapultMovement.Off);
                return;
            }
            _lastCatapultMovement = (_lastCatapultMovement == CatapultMovement.PowerDown) ? CatapultMovement.PowerUp : CatapultMovement.PowerDown;

            if (_safetyLightState == GpioPinValue.Low) TurnSafetyLightOn();
            MoveCatapult(_lastCatapultMovement);
        }

        private void MoveCatapult(CatapultMovement direction)
        {
            switch (direction)
            {
                case CatapultMovement.PowerDown:
                    Debug.WriteLine("Powering down the catapult.");
                    _catapultUpPin.Write(GpioPinValue.Low);
                    _catapultDownPin.Write(GpioPinValue.High);
                    return;
                case CatapultMovement.PowerUp:
                    Debug.WriteLine("Powering up the catapult.");
                    _catapultDownPin.Write(GpioPinValue.Low);
                    _catapultUpPin.Write(GpioPinValue.High);
                    return;
                default:
                    Debug.WriteLine("Stopping catapult arming.");
                    _catapultUpPin.Write(GpioPinValue.High);
                    _catapultDownPin.Write(GpioPinValue.High);
                    return;
            }
        }

        private void TurnSafetyLightOn()
        {
            Debug.WriteLine("Turning safety light on.");
            _safetyLightState = GpioPinValue.High;
            _safetyLightPin.Write(_safetyLightState);
        }

        private void TurnSafetyLightOff()
        {
            Debug.WriteLine("Turning safety light off.");
            _safetyLightState = GpioPinValue.Low;
            _safetyLightPin.Write(_safetyLightState);
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveCatapult(CatapultMovement.PowerUp);
        }

        private void TurnOnLightButton_Click(object sender, RoutedEventArgs e)
        {
            TurnSafetyLightOn();
        }

        private void TurnOffLightButton_Click(object sender, RoutedEventArgs e)
        {
            TurnSafetyLightOff();
        }

        private void UpButton_OnClick(object sender, RoutedEventArgs e)
        {
            MoveCatapult(CatapultMovement.PowerDown);
        }

        private void DownButton_OnClick(object sender, RoutedEventArgs e)
        {
            MoveCatapult(CatapultMovement.PowerUp);
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            MoveCatapult(CatapultMovement.Off);
        }
    }

    enum CatapultMovement
    {
        Off,
        PowerUp,
        PowerDown
    }
}
