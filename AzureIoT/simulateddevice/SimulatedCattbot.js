'use strict';

var clientFromConnectionString = require('azure-iot-device-mqtt').clientFromConnectionString;
var Message = require('azure-iot-device').Message;

var connectionString = 'HostName=cattbot-iothub-01.azure-devices.net;DeviceId=cattbot;SharedAccessKey=Dy4z8EFcChoD/rxlCzYzzaLJHAS+mesZVecIKtSiN2g=';

var client = clientFromConnectionString(connectionString);

function printResultFor(op) {
  return function printResult(err, res) {
    if (err) console.log(op + ' error: ' + err.toString());
    if (res) console.log(op + ' status: ' + res.constructor.name);
  };
}

var connectCallback = function (err) {
  if (err) {
    console.log('Could not connect: ' + err);
  } else {
    console.log('Client connected');

    // Create a message and send it to the IoT Hub every second
    setInterval(function(){
        var mc1Temperature = 20 + (Math.random() * 15);
	var mc1MainBatteryV = 13 + (Math.random() * 10);
	var mc1LogicBatteryV = 13 + (Math.random() * 10);
        var mc1M1Amps = 60 + (Math.random() * 20);            
        var mc1M2Amps = 60 + (Math.random() * 20);            
        var mc1M1EncoderCnt = 60 + (Math.random() * 20);            
        var mc1M2EncoderCnt = 60 + (Math.random() * 20);            
        var data = JSON.stringify({ deviceId: 'cattbot', mc1_temperature: mc1Temperature, mc1M1Amps: mc1M1Amps, mc1M2Amps: mc1M2Amps, mc1M1EncoderCnt: mc1M1EncoderCnt, mc1M2EncoderCnt: mc1M2EncoderCnt, mc1MainBatteryV: mc1MainBatteryV, mc1LogicBatteryV: mc1LogicBatteryV});
        var message = new Message(data);
        message.properties.add('temperatureAlert', (mc1Temperature > 30) ? 'true' : 'false');
        console.log("Sending message: " + message.getData());
        client.sendEvent(message, printResultFor('send'));
    }, 10000);
  }
};

client.open(connectCallback);
