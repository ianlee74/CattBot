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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;

using RoboclawClassLib;

namespace CattbotIoTHubDemoWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Roboclaw _rearRoboClaw;
        private string _roboClawModel;
        private int _m1EncoderTicksCnt, _m2EncoderTicksCnt;
        private short _m1Current, _m2Current;
        private double _temperature;
        private double _mainVoltage, _logicVoltage;
        private bool _encoderWatch = false;
        private static readonly Timer _telemetryTimer = new Timer();
        private bool _sendToIotHub;

        public MainWindow()
        {
            InitializeComponent();

            _rearRoboClaw = new Roboclaw();

            _telemetryTimer.Elapsed += TimerEventProcessor;  // Timer event and handler
            _telemetryTimer.Interval = 1000; // ms
            _sendToIotHub = false;
        }

        private void btnGoReverse_Click(object sender, RoutedEventArgs e)
        {
            if (_rearRoboClaw.IsOpen())
            {
                _rearRoboClaw.ST_M1Backward(100); // Start the motor going forward at power 100
                _rearRoboClaw.ST_M2Backward(100); // Start the motor going forward at power 100
                _telemetryTimer.Start(); // Start timer to show encoder ticks
                btnStop.IsEnabled = true;
                btnGoForward.IsEnabled = false;
                btnGoReverse.IsEnabled = false;
                btnDisconnect.IsEnabled = false;
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_rearRoboClaw.IsOpen())
            {
                _rearRoboClaw.ST_M1Forward(0); // Stop the motor
                _rearRoboClaw.ST_M2Forward(0); // Stop the motor
                _telemetryTimer.Stop(); // Stop timer to stop encoder updates
                btnStop.IsEnabled = false;
                btnGoForward.IsEnabled = true;
                btnGoReverse.IsEnabled = true;
                btnDisconnect.IsEnabled = true;
                _encoderWatch = false;
            }
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (_rearRoboClaw.IsOpen())
            {
                _telemetryTimer.Stop(); // Stop the timer to stop the encoder display updates
                _rearRoboClaw.Close(); // Close the RoboClaw interface
                lblModel.Text = " "; // Clear the RoboClaw device model number display
                btnStop.IsEnabled = false;
                btnGoForward.IsEnabled = false;
                btnGoReverse.IsEnabled = false;
                btnConnect.IsEnabled = true;
                btnDisconnect.IsEnabled = false;
            }
        }

        private void btnUpdateInterval_Click(object sender, RoutedEventArgs e)
        {
            _telemetryTimer.Stop();
            _telemetryTimer.Interval = int.Parse(txtUpdateInterval.Text);
            _telemetryTimer.Start();
        }

        private void chkSendToAzure_Checked(object sender, RoutedEventArgs e)
        {
            _sendToIotHub = chkSendToAzure.IsChecked.Value;
        }

        private void btnMoveForward_Click(object sender, RoutedEventArgs e)
        {
            if (_rearRoboClaw.IsOpen())
            {
                _rearRoboClaw.ST_M1Forward(100); // Start the motor going forward at power 100
                _rearRoboClaw.ST_M2Forward(100); // Start the motor going forward at power 100
                _telemetryTimer.Start(); // Start timer to show encoder ticks
                btnStop.IsEnabled = true;
                btnGoForward.IsEnabled = false;
                btnGoReverse.IsEnabled = false;
                btnDisconnect.IsEnabled = false;
            }
        }


        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!_rearRoboClaw.IsOpen())
            {
                _rearRoboClaw.Open("AUTO", ref _roboClawModel, 128, 38400); // Open the interface to the RoboClaw
                lblModel.Text = _roboClawModel; // Display the RoboClaw device model number
                _rearRoboClaw.ResetEncoders();
                btnConnect.IsEnabled = false;
                btnGoForward.IsEnabled = true;
                btnGoReverse.IsEnabled = true;
                btnDisconnect.IsEnabled = true;
            }
        }

        // This is the method to run when the timer is raised.
        private void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            // Get data points.
            _rearRoboClaw.GetEncoders(out _m1EncoderTicksCnt, out _m2EncoderTicksCnt);
            _rearRoboClaw.GetCurrents(out _m1Current, out _m2Current);
            _rearRoboClaw.GetTemperature(out _temperature);
            _rearRoboClaw.GetMainVoltage(out _mainVoltage);
            _rearRoboClaw.GetLogicVoltage(out _logicVoltage);

            // Update the UI
            Dispatcher.Invoke(() =>
            {
                lblM1EncoderTicksCount.Text = _m1EncoderTicksCnt.ToString();
                lblM2EncoderTicksCount.Text = _m2EncoderTicksCnt.ToString();
                lblM1Current.Text = _m1Current.ToString();
                lblM2Current.Text = _m2Current.ToString();
                lblTemperature.Text = _temperature.ToString("0.00");
                lblMainVoltage.Text = _mainVoltage.ToString("0.00");
                lblLogicVoltage.Text = _logicVoltage.ToString("0.00");
            });

            // Send to Azure IoT Hub
            if(_sendToIotHub)
            {
                var temperatureAlert = (_temperature > 35) ? "true" : "false";
                var msg = $"{{ deviceId: 'cattbot', mc1_temperature: {_temperature}, mc1M1Amps: {_m1Current}, mc1M2Amps: {_m2Current}, mc1M1EncoderTicksCnt: {_m1EncoderTicksCnt}, mc1M2EncoderTicksCnt: {_m2EncoderTicksCnt}, mc1MainBatteryV: {_mainVoltage}, mc1LogicBatteryV: {_logicVoltage}, temperatureAlert: {temperatureAlert}}}";
                AzureIoTHub.SendDeviceToCloudMessageAsync(msg);
            }

            if (_encoderWatch)
            {
                if (Math.Abs(_m1EncoderTicksCnt) < 10)
                {
                    _rearRoboClaw.ST_M1Forward(0);
                    _telemetryTimer.Stop(); // Stop timer to stop encoder updates
                    btnStop.IsEnabled = false;
                    btnGoForward.IsEnabled = true;
                    btnGoReverse.IsEnabled = true;
                    btnDisconnect.IsEnabled = true;
                    _encoderWatch = false;
                }
                else if (Math.Abs(_m1EncoderTicksCnt) < 100)
                {
                    if (_m1EncoderTicksCnt < 0)
                    {
                        _rearRoboClaw.ST_M1Forward(14);
                    }
                    else
                    {
                        _rearRoboClaw.ST_M1Backward(14);
                    }
                }
                else if (Math.Abs(_m1EncoderTicksCnt) < 250)
                {
                    if (_m1EncoderTicksCnt < 0)
                    {
                        _rearRoboClaw.ST_M1Forward(20);
                    }
                    else
                    {
                        _rearRoboClaw.ST_M1Backward(20);
                    }
                }
                else if (Math.Abs(_m1EncoderTicksCnt) < 1000)
                {
                    if (_m1EncoderTicksCnt < 0)
                    {
                        _rearRoboClaw.ST_M1Forward(30);
                    }
                    else
                    {
                        _rearRoboClaw.ST_M1Backward(30);
                    }
                }
            }
        }
    }
}
