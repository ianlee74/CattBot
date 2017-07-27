using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CattbotSimulator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            var timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 10);
            timer.Tick += (sender, e) => {  
                // Send simulated CATTbot telemetry data.
                var rnd = new Random();
                var mc1Temperature = rnd.Next(50);
                var mc1MainBatteryV = rnd.Next(350) / 10.0;
                var mc1LogicBatteryV = rnd.Next(35) / 10.0;
                var mc1M1Amps = rnd.Next(600) / 10.0;
                var mc1M2Amps = rnd.Next(600) / 10.0;
                var mc1M1EncoderCnt = rnd.Next(35000);
                var mc1M2EncoderCnt = rnd.Next(35000);
                var temperatureAlert = (mc1Temperature > 30) ? "true" : "false";
                var msg = $"{{ deviceId: 'cattbot', mc1_temperature: {mc1Temperature}, mc1M1Amps: {mc1M1Amps}, mc1M2Amps: {mc1M2Amps}, mc1M1EncoderCnt: {mc1M1EncoderCnt}, mc1M2EncoderCnt: {mc1M2EncoderCnt}, mc1MainBatteryV: {mc1MainBatteryV}, mc1LogicBatteryV: {mc1LogicBatteryV}, temperatureAlert: {temperatureAlert}}}";
                Debug.WriteLine("Sending... {0}", msg);
                AzureIoTHub.SendDeviceToCloudMessageAsync(msg);
            };
            timer.Start();
        }
    }
}
