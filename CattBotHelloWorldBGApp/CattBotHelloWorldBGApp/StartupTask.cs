using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.System.Threading;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace CattBotHelloWorldBGApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        private const int SAFETYLIGHT_PIN = 3;

        private GpioPin _safetyLightPin;
        private GpioPinValue _safetyLightState;
        private BackgroundTaskDeferral _deferral;
        private ThreadPoolTimer _timer;

        private void InitGpio()
        {
            var gpio = GpioController.GetDefault();
            if (gpio == null)
            {
                return;
            }
            _safetyLightPin = gpio.OpenPin(SAFETYLIGHT_PIN);
            _safetyLightPin.Write(GpioPinValue.Low);
            _safetyLightPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            InitGpio();
            _timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromSeconds(2));
        }

        private void Timer_Tick(ThreadPoolTimer timer)
        {
            _safetyLightState = (_safetyLightState == GpioPinValue.High) ? GpioPinValue.Low : GpioPinValue.High;
            _safetyLightPin.Write(_safetyLightState);
            Debug.WriteLine("Tick! {0}", DateTime.Now);
        }
    }
}