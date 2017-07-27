using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

static class AzureIoTHub
{
    //
    // Note: this connection string is specific to the device "cattbot". To configure other devices,
    // see information on iothub-explorer at http://aka.ms/iothubgetstartedVSCS
    //
    const string deviceConnectionString = "HostName=cattbot-iothub-01.azure-devices.net;DeviceId=cattbot;SharedAccessKey=Dy4z8EFcChoD/rxlCzYzzaLJHAS+mesZVecIKtSiN2g=";

    //
    // To monitor messages sent to device "cattbot" use iothub-explorer as follows:
    //    iothub-explorer monitor-events --login HostName=cattbot-iothub-01.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=byMed+wGV/WxOofAyHkMO8z+nOsoDQHc6BtR8FX4IvU= "cattbot"
    //

    // Refer to http://aka.ms/azure-iot-hub-vs-cs-wiki for more information on Connected Service for Azure IoT Hub

    public static async Task SendDeviceToCloudMessageAsync(string msg)
    {
        var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
        var message = new Message(Encoding.ASCII.GetBytes(msg));
        await deviceClient.SendEventAsync(message);
    }

    public static async Task<string> ReceiveCloudToDeviceMessageAsync()
    {
        var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);

        while (true)
        {
            var receivedMessage = await deviceClient.ReceiveAsync();

            if (receivedMessage != null)
            {
                var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                await deviceClient.CompleteAsync(receivedMessage);
                return messageData;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}
