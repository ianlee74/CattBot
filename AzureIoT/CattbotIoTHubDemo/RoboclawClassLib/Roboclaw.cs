using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using USBLib;

namespace RoboclawClassLib
{
    public class Roboclaw
    {
        private System.IO.Ports.SerialPort serialPort;

		private UInt16 m_crc;
		private byte m_address;

		static List<USBDeviceInfo> GetDevices()
        {
			List<USBDeviceInfo> devices = USB.GetUSBDevices();

			foreach (USBDeviceInfo device in devices)
			{
				if (device.Description == "RC5A32KULDR" ||
					device.Description == "RC15A32KULDR" ||
					device.Description == "RC15AV532KULDR" ||
					device.Description == "RC30A32KULDR" ||
					device.Description == "RC30AV532KULDR" ||
					device.Description == "RC45A32KULDR" ||
					device.Description == "RC60A32KULDR" ||
					device.Description == "RC5A64KULDR" ||
					device.Description == "RC15A64KULDR" ||
					device.Description == "RC15AV564KULDR" ||
					device.Description == "RC30A64KULDR" ||
					device.Description == "RC30AV564KULDR" ||
					device.Description == "RC45A64KULDR" ||
					device.Description == "RC60A64KULDR" ||
					device.Description == "RC60HV6032KULDR" ||
					device.Description == "RC60HV6064KULDR" ||
					device.Description == "RC60HV8032KULDR" ||
					device.Description == "RC60HV8064KULDR" ||
					device.Description == "RC80HV6032KULDR" ||
					device.Description == "RC80HV6064KULDR" ||
					device.Description == "RC120HV6032KULDR" ||
					device.Description == "RC120HV6064KULDR" ||
					device.Description == "RC120HV8032KULDR" ||
					device.Description == "RC120HV8064KULDR" ||
					device.Description == "RC160HV6032KULDR" ||
					device.Description == "RC160HV6064KULDR" ||
					device.Description == "RC160HV8032KULDR" ||
					device.Description == "RC160HV8064KULDR" ||
					device.Description == "MCP230ULDR" ||
					device.Description == "MCP260ULDR" ||
					device.Description == "MCP2160ULDR" ||
					device.Description == "USB Roboclaw 2x5A" ||
					device.Description == "USB Roboclaw 2x15A" ||
					device.Description == "USB Roboclaw 2x30A" ||
					device.Description == "USB Roboclaw 2x45A" ||
					device.Description == "USB Roboclaw 2x60A" ||
					device.Description == "USB Roboclaw 2x60HV60" ||
					device.Description == "USB Roboclaw 2x60HV80" ||
					device.Description == "USB Roboclaw 2x80HV60" ||
					device.Description == "USB Roboclaw 2x80HV80" ||
					device.Description == "USB Roboclaw 2x120HV60" ||
					device.Description == "USB Roboclaw 2x120HV80" ||
					device.Description == "USB Roboclaw 2x160HV60" ||
					device.Description == "USB Roboclaw 2x160HV80" ||
					device.Description == "MCP230 2x30A" ||
					device.Description == "MCP260 2x60A" ||
					device.Description == "MCP2160 2x160A")
				{
					System.Text.RegularExpressions.Regex rgx = new System.Text.RegularExpressions.Regex(@"\((COM\d+)\)");  // "..... (COMxxx)" -> COMxxxx
					if (rgx.Matches(device.Name).Count > 0)
					{
						string raw = rgx.Match(device.Name).ToString();
						System.Text.RegularExpressions.Regex rgx2 = new System.Text.RegularExpressions.Regex(@"(COM\d+)");  // "..... (COMxxx)" -> COMxxxx
						device.DeviceID = rgx2.Match(raw).ToString();
					}
				}
			}

            return devices;
        }

        private void DumpGarbage()
		{
			Int32 count = serialPort.BytesToRead;
			char[] buffer = new char[1];
			if(count!=0){
				String text = "";
				for (UInt32 i = 0; i < count; i++)
				{
					serialPort.Read(buffer, 0, 1);
					text += String.Format("{0:X2} ", buffer[0]);
				}
				text += "\n\r";
				//Debug.WriteLine(text);
			}
		}

		private void crc_clear()
		{
			m_crc = 0;
		}

		private UInt16 crc_update (byte data)
		{
			int i;
			m_crc = (UInt16)(m_crc ^ ((UInt16)data << 8));
			for (i=0; i<8; i++)
			{
				if ((m_crc & 0x8000)!=0)
					m_crc = (UInt16)((m_crc << 1) ^ 0x1021);
				else
					m_crc <<= 1;
			}
			return m_crc;
		}

		private UInt16 crc_get()
		{
			return m_crc;
		}

		private bool Write_CRC(byte[] data)
        {
            crc_clear();
            int len = data.GetLength(0);
            for (int i = 0; i < len-2; i++)
            {
                crc_update(data[i]);
            }
			UInt16 crc = crc_get();
            data[data.GetLength(0) - 2] = (byte)(crc>>8);
			data[data.GetLength(0) - 1] = (byte)crc;
            serialPort.Write(data, 0, len);
            try
            {
				serialPort.ReadByte();
			}
			catch (TimeoutException)
            {
				System.Threading.Thread.Sleep(20);
                return false;
            }
			return true;
        }

        private bool readcmd(byte address, byte cmd, uint len, out byte[] arr)
        {
			DumpGarbage();

			arr = new byte[len];
			uint timeout = 0;
			UInt16 ccrc;
									
			do{
				if(timeout!=0)
					System.Threading.Thread.Sleep(100);

				timeout++;

				uint timeout0 = 0;
				crc_clear();
				do{
					serialPort.DiscardInBuffer();
					if (timeout0 != 0)
						System.Threading.Thread.Sleep(20);

					crc_update(address);
					crc_update(cmd);

					serialPort.Write(new byte[] { address, cmd }, 0, 2);

					for (Int32 i = 0; i < len; i++)
					{
						try{
							arr[i] = (byte)serialPort.ReadByte();
							timeout0 = 0;
							crc_update(arr[i]);
						}
						catch (TimeoutException){
							serialPort.DiscardInBuffer();
							if (i == 0){
								timeout0++;
								break;
							}
							else{
								return false;
							}
						}
					}
				}while(timeout0>0 && timeout0<5);
				if(timeout0==5){
					return false;
				}

				try{
					ccrc = (UInt16)(serialPort.ReadByte()<<8);
					ccrc |= (UInt16)serialPort.ReadByte();
				}
				catch (TimeoutException){
					serialPort.DiscardInBuffer();
					return false;
				}
			}while(timeout<5 && crc_get()!=ccrc);

			if (crc_get()!=ccrc){
				serialPort.DiscardInBuffer();
				return false;
            }

            return true;
        }

        public bool IsOpen()
        {
			if(null!=serialPort){
				return serialPort.IsOpen;
			}
			return false;
        }

        public void Close()
        {
			try{
				//serialPort.RtsEnable = false;
				serialPort.Close();
			}
			catch(Exception){}
			try{
				serialPort.Dispose();
			}
			catch(Exception){}
			serialPort=null;
        }

		public string Find(ref string model)
		{
			string comport = "";

			//This code will autodetect USB Roboclaw boards
			var usbDevices = GetDevices();   //Get all USB to Serial adapters
			foreach (var usbDevice in usbDevices)
			{
				//Check if this is a USB Roboclaw
				comport = usbDevice.DeviceID;
				model = usbDevice.Description;
			}

			return comport;
		}

        public string Open(
            string comport,
            ref string model,
			byte address,
            Int32 baudrate)
        {
			m_address = address;

			//Close port if its already open
			Close();

			int retry = 10;
			if (comport == "AUTO")
            {
				do
				{
	                //This code will autodetect USB Roboclaw boards
		            var usbDevices = GetDevices();   //Get all USB to Serial adapters
			        //foreach (var usbDevice in usbDevices)
					if(usbDevices.Count()>0)
					{
						//Check if this is a USB Roboclaw
	                    comport = usbDevices[0].DeviceID;
		                model = usbDevices[0].Description;
						break;
					}
					else{
						System.Threading.Thread.Sleep(100);
					}
				}while((retry--)!=0);

				if (comport == "AUTO")
                    return "";
            }
			else{
				model = "AUTO";
			}

			retry=20;
			do{
				try{
					serialPort = new System.IO.Ports.SerialPort(comport, baudrate);
					serialPort.Open();
					GC.SuppressFinalize(serialPort.BaseStream);
					serialPort.RtsEnable = true;
					serialPort.WriteTimeout = 5000;
					serialPort.ReadTimeout = 100;
					System.Threading.Thread.Sleep(250);
					serialPort.DiscardInBuffer();
					serialPort.DiscardOutBuffer();
					return comport;
				}
				catch(Exception pEx){
					string message = pEx.Message;
				}
				System.Threading.Thread.Sleep(100);
			} while ((--retry) != 0);
			return ""; 
        }

		BackgroundWorker fw;
		
		private void fw_DoWork(object sender, DoWorkEventArgs e)
		{
			string fileName = "";
			string model = "";
			int size = 0;
			string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

			int trys=50;
			bool softentry=true;
			do
			{
				if (Open("AUTO", ref model, 0x80, 115200) != "")
				{
					if (model == "RC5A32KULDR")
					{
						fileName = path + "\\MC5A32K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC5A64KULDR")
					{
						fileName = path + "\\MC5A64K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC15A32KULDR")
					{
						fileName = path + "\\MC15A32K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC15AV532KULDR")
					{
						fileName = path + "\\MC15AV532K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC15A64KULDR")
					{
						fileName = path + "\\MC15A64K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC15AV564KULDR")
					{
						fileName = path + "\\MC15AV564K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC30A32KULDR")
					{
						fileName = path + "\\MC30A32K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC30AV532KULDR")
					{
						fileName = path + "\\MC30AV532K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC30A64KULDR")
					{
						fileName = path + "\\MC30A64K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC30AV564KULDR")
					{
						fileName = path + "\\MC30AV564K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC45A32KULDR")
					{
						fileName = path + "\\MC45A32K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC45A64KULDR")
					{
						fileName = path + "\\MC45A64K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC60A32KULDR")
					{
						fileName = path + "\\MC60A32K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC60A64KULDR"){
						fileName = path + "\\MC60A64K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC60HV6032KULDR"){
						fileName = path + "\\MC60HV6032K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC60HV6064KULDR")
					{
						fileName = path + "\\MC60HV6064K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC60HV8032KULDR")
					{
						fileName = path + "\\MC60HV8032K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC60HV8064KULDR")
					{
						fileName = path + "\\MC60HV8064K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC80HV6032KULDR"){
						fileName = path + "\\MC80HV6032K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC80HV6064KULDR")
					{
						fileName = path + "\\MC80HV6064K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC80HV8032KULDR")
					{
						fileName = path + "\\MC80HV8032K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC80HV8064KULDR")
					{
						fileName = path + "\\MC80HV8064K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC120HV6032KULDR")
					{
						fileName = path + "\\MC120HV6032K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC120HV6064KULDR")
					{
						fileName = path + "\\MC120HV6064K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC120HV8032KULDR")
					{
						fileName = path + "\\MC120HV8032K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC120HV8064KULDR")
					{
						fileName = path + "\\MC120HV8064K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC160HV6032KULDR")
					{
						fileName = path + "\\MC160HV6032K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC160HV6064KULDR")
					{
						fileName = path + "\\MC160HV6064K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "RC160HV8032KULDR")
					{
						fileName = path + "\\MC160HV8032K.bin";
						size = 0x8000;
						softentry = false;
					}
					else if (model == "RC160HV8064KULDR")
					{
						fileName = path + "\\MC160HV8064K.bin";
						size = 0x10000;
						softentry = false;
					}
					else if (model == "MCP230ULDR")
					{
						fileName = path + "\\MCP230.bin";
						size = 983040;
						softentry = false;
					}
					else if (model == "MCP260ULDR")
					{
						fileName = path + "\\MCP260.bin";
						size = 983040;
						softentry = false;
					}
					else if (model == "MCP2160ULDR")
					{
						fileName = path + "\\MCP2160.bin";
						size = 983040;
						softentry = false;
					}

					if(softentry){
						serialPort.DiscardInBuffer();
						serialPort.RtsEnable = true;
						System.Threading.Thread.Sleep(250);
						serialPort.DiscardInBuffer();
						byte[] data = { 0x80, 0xFF, 0xFF, 0xA5, 0xA5, 0x12, 0x34, 0xB6, 0x70 };
						serialPort.Write(data,0,9);
						try{
							serialPort.Read(data,0,1);
						}
						catch (Exception)
						{
						}
						if ((trys--) == 0)
						{
							Close();
							e.Cancel = true;
							fw.CancelAsync();
							return;
						}
						Close();
						System.Threading.Thread.Sleep(100);
					}
				}
				else{
					if ((trys--) == 0)
					{
						if (model == "AUTO")
						{
							MessageBox.Show("Firmware Update requires a USB connection", "Update Error");
						}
						else
						if (model == "")
						{
							MessageBox.Show("Unable to find supported device", "Update Error");
						}
						else
						{
							MessageBox.Show("Unable to open communications port", "Update Error");
						}
						fw.CancelAsync();
						return;
					}
					System.Threading.Thread.Sleep(100);
				}
			}while(softentry);

			if (File.Exists(fileName))
			{
				byte[] data = new byte[0x1000000];
				try{
					using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open,FileAccess.Read)))
					{
						reader.Read(data, 0, size);

						try
						{
							serialPort.ReadTimeout = 10000;
							//write
							serialPort.Write(new byte[] { 0x01 }, 0, 1);
							if (serialPort.ReadByte() != 0xaa)
							{
								MessageBox.Show("Error Writing", "Update Error");
								Close();
								fw.CancelAsync();
								return;
							}

							serialPort.ReadTimeout = 100;
							for (uint c1 = 0; c1 < size; c1 += 256)
							{
								if (c1 % 0x200 == 0)
									fw.ReportProgress((int)(100 * c1 / size));
								serialPort.Write(data, (int)c1, 256);
								if (serialPort.ReadByte() != 0xaa)
								{
									MessageBox.Show("Error Writing: " + (c1 * 2).ToString(), "Update Error");
									Close();
									fw.CancelAsync();
									return;
								}
							}
							fw.ReportProgress(100);
							System.Threading.Thread.Sleep(1000);

							//reset
							serialPort.Write(new byte[] { 0x04 }, 0, 1);
						}
						catch (Exception pEx)
						{
							MessageBox.Show("Unexpected communications error: " + pEx.Message, "Update Error");
							Close();
							fw.CancelAsync();
							return;
						}
						Close();
					}
				}
				catch(Exception pEx)
				{
					MessageBox.Show("Firmware Binary Read error: " + pEx.Message, "Update Error");	
				}
			}
			else
			{
				MessageBox.Show("Firmware binary is missing", "Update Error");
				Close();
			}

			fw.CancelAsync();
			return;
		}

		public void UpdateFirmware(ProgressChangedEventHandler update,RunWorkerCompletedEventHandler completed)
        {
			System.OperatingSystem osInfo = System.Environment.OSVersion;
			if(osInfo.Version.Major<=6 && osInfo.Version.Minor<1){
				MessageBox.Show("Function requires Windows 7 or newer.","Unsupported OS");
			}
			else{
				fw = new BackgroundWorker();
				fw.WorkerSupportsCancellation = true;
				fw.WorkerReportsProgress = true;
				fw.DoWork += new DoWorkEventHandler(fw_DoWork);
				fw.ProgressChanged += new ProgressChangedEventHandler(update);
				fw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(completed);
				fw.RunWorkerAsync();
			}
        }

		public bool ResetEStop()
		{
			return Write_CRC(new byte[] { m_address, 200, 0, 0 });
		}

		public bool SetEStopLock(byte state)
		{
			return Write_CRC(new byte[] { m_address, 201, state, 0, 0 });
		}

		public bool GetEStopLock(out byte state)
		{
			byte[] arr;
			if (readcmd(m_address, 202, 1, out arr))
			{
				state = arr[0];
				return true;
			}
			state = 0;
			return false;
		}

		public bool SetScriptAuto(UInt32 time)
		{
			return Write_CRC(new byte[] { m_address, 246, (byte)(time >> 24), (byte)(time >> 16), (byte)(time >> 8), (byte)time, 0, 0 });
		}

		public bool GetScriptAuto(out UInt32 time)
		{
			byte[] arr;
			if (readcmd(m_address, 247, 4, out arr))
			{
				time = (UInt32)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]);
				return true;
			}
			time = 0;
			return false;
		}

		public bool StartScript()
		{
			return Write_CRC(new byte[] { m_address, 248, 0, 0 });
		}

		public bool StopScript()
		{
			return Write_CRC(new byte[] { m_address, 249, 0, 0 });
		}

		public delegate void CallbackProgress(uint val);
		public delegate void CallbackLabel(string label);
		public void Download(string filename, CallbackProgress doProgress,CallbackLabel doLabel)
		{
			if (File.Exists(filename))
			{
				int length = (int)new System.IO.FileInfo(filename).Length;
				byte[] data = new byte[length+16];
				using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
				{
					reader.Read(data, 0, length);
					serialPort.ReadTimeout = 2000;
					while (!Write_CRC(new byte[] { m_address, 250, 0x55, 0xAA, 0, 0 })) ; //Erase USERBLOCK
					byte [] status = new byte[1];
					serialPort.Read(status,0,1);
					doLabel("Programming...");
					serialPort.ReadTimeout = 100;
					uint lastprogress=0;
					for(int i=0x100;i<length;i+=16){
						int address = i-256;
						while(!Write_CRC(new byte[] {	m_address, 251,
												(byte)(address>>16), (byte)(address>>8), (byte)address,
												data[i+3],data[i+2],data[i+1],data[i+0],
												data[i+7],data[i+6],data[i+5],data[i+4],
												data[i+11],data[i+10],data[i+9],data[i+8],
												data[i+15],data[i+14],data[i+13],data[i+12],
												0, 0})) ;
						serialPort.Read(status, 0, 1);
						//Debug.WriteLine("{0:X6}:{1:X2}{2:X2}{3:X2}{4:X2} {5:X2}{6:X2}{7:X2}{8:X2} {9:X2}{10:X2}{11:X2}{12:X2} {13:X2}{14:X2}{15:X2}{16:X2}", address, data[i + 0], data[i + 1], data[i + 2], data[i + 3], data[i + 4], data[i + 5], data[i + 6], data[i + 7], data[i + 8], data[i + 9], data[i + 10], data[i + 11], data[i + 12], data[i + 13], data[i + 14], data[i + 15]);
						uint newprogress = (uint)((double)i / ((double)length / 100));
						if(newprogress!=lastprogress){
							doProgress(newprogress);
							lastprogress = (uint)((double)i/((double)length/100));
						}
					}
				}
			}
		}

        public bool ST_M1Forward(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 0, pwr, 0, 0 });
        }

        public bool ST_M2Forward(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 4, pwr, 0, 0 });
        }

        public bool ST_M1Backward(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 1, pwr, 0, 0 });
        }

        public bool ST_M2Backward(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 5, pwr, 0, 0 });
        }

        public bool ST_M1Drive(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 6, pwr, 0, 0 });
        }

        public bool ST_M2Drive(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 7, pwr, 0, 0 });
        }

        public bool ST_MixedForward(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 8, pwr, 0, 0 });
        }

        public bool ST_MixedBackward(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 9, pwr, 0, 0 });
        }

        public bool ST_MixedLeft(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 11, pwr, 0, 0 });
        }

        public bool ST_MixedRight(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 10, pwr, 0, 0 });
        }

        public bool ST_MixedDrive(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 12, pwr, 0, 0 });
        }

        public bool ST_MixedTurn(byte pwr)
        {
			return Write_CRC(new byte[] { m_address, 13, pwr, 0, 0 });
        }

        public bool ST_SetMinMainVoltage(byte set)
        {
			return Write_CRC(new byte[] { m_address, 2, set, 0, 0 });
        }

        public bool ST_SetMaxMainVoltage(byte set)
        {
			return Write_CRC(new byte[] { m_address, 3, set, 0, 0 });
        }

        public bool GetM1Encoder(out Int32 enc,out byte status)
        {
            byte[] arr;
            if(readcmd(m_address, 16, 5, out arr)){
                enc = (Int32)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]);
                status = arr[4];
                return true;
            }
            enc = 0;
            status = 0;
            return false;
        }

        public bool GetM2Encoder(out Int32 enc, out byte status)
        {
            byte[] arr;
            if (readcmd(m_address, 17, 5, out arr))
            {
                enc = (Int32)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]);
                status = arr[4];
                return true;
            }
            enc = 0;
            status = 0;
            return false;
        }

        public bool GetM1Speed(out Int32 speed,out byte status)
        {
            byte[] arr;
            if (readcmd(m_address, 18, 5, out arr))
            {
                speed = (Int32)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]);
                status = arr[4];
                return true;
            }
            speed = 0;
            status = 0;
            return false;
        }

        public bool GetM2Speed(out Int32 speed, out byte status)
        {
            byte[] arr;
            if (readcmd(m_address, 19, 5, out arr))
            {
                speed = (Int32)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]);
                status = arr[4];
                return true;
            }
            speed = 0;
            status = 0;
            return false;
        }

		public bool GetEncoders(out Int32 M1cnt, out Int32 M2cnt)
		{
			byte[] arr;
			if (readcmd(m_address, 78, 8, out arr))
			{
				M1cnt = (Int32)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]);
				M2cnt = (Int32)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]);
				return true;
			}
			M1cnt = 0;
			M2cnt = 0;
			return false;
		}

		public bool GetISpeeds(out Int32 M1speed, out Int32 M2speed)
		{
			byte[] arr;
			if (readcmd(m_address, 79, 8, out arr))
			{
				M1speed = (Int32)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]);
				M2speed = (Int32)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]);
				return true;
			}
			M1speed = 0;
			M2speed = 0;
			return false;
		}

		public bool ResetEncoders()
        {
			return Write_CRC(new byte[] { m_address, 20, 0, 0 });
        }

        public bool GetVersion(out string version)
        {
			DumpGarbage();

			version = "";
			crc_clear();
			crc_update(m_address);
			crc_update(21);
			serialPort.Write(new byte[] { m_address, 21 }, 0, 2);
			try{
				byte data;
				do{
					data = (byte)serialPort.ReadByte();
					crc_update(data);
					if(data!=0 && data!='\n')
						version+=Convert.ToChar(data);
				}while(data!=0);
                UInt16 ccrc = (UInt16)(serialPort.ReadByte()<<8);
                ccrc |= (UInt16)serialPort.ReadByte();
				if(crc_get()==ccrc)
					return true;
            }
            catch (Exception)
            {
				version = "";
            }
            return false;
        }

		public bool SetEncoder1(UInt32 pos)
		{
			return Write_CRC(new byte[] { m_address, 22, (byte)(pos >> 24), (byte)(pos >> 16), (byte)(pos >> 8), (byte)pos, 0, 0 });
		}

		public bool SetEncoder2(UInt32 pos)
		{
			return Write_CRC(new byte[] { m_address, 23, (byte)(pos >> 24), (byte)(pos >> 16), (byte)(pos >> 8), (byte)pos, 0, 0 });
		}

		public bool GetMainVoltage(out double voltage)
        {
            voltage = 0;
            byte[] arr;
            if (readcmd(m_address, 24, 2, out arr))
            {
                voltage = (double)(arr[0] << 8 | arr[1])/10.0;
                return true;
            }
            return false;
        }

        public bool GetLogicVoltage(out double voltage)
        {
            voltage = 0;
            byte[] arr;
            if (readcmd(m_address, 25, 2, out arr))
            {
                voltage = (double)(arr[0] << 8 | arr[1])/10.0;
                return true;
            }
            return false;
        }

        public bool ST_SetMaxLogicVoltage(byte set)
        {
			return Write_CRC(new byte[] { m_address, 26, set, 0, 0 });
        }

        public bool ST_SetMinLogicVolage(byte set)
        {
			return Write_CRC(new byte[] { m_address, 27, set, 0, 0 });
        }

        public bool SetM1VelocityConstants(double P, double I, double D, UInt32 qpps)
        {
            UInt32 p, i, d;
            p = (UInt32)(P * 65536.0);
            i = (UInt32)(I * 65536.0);
            d = (UInt32)(D * 65536.0);

			return Write_CRC(new byte[] { m_address, 28, (byte)(d >> 24), (byte)(d >> 16), (byte)(d >> 8), (byte)d,
                                          (byte)(p >> 24), (byte)(p >> 16), (byte)(p >> 8), (byte)p,
                                          (byte)(i >> 24), (byte)(i >> 16), (byte)(i >> 8), (byte)i,
                                          (byte)(qpps >> 24), (byte)(qpps >> 16), (byte)(qpps >> 8), (byte)qpps,
                                          0, 0 });
        }

        public bool SetM2VelocityConstants(double P, double I, double D, UInt32 qpps)
        {
            UInt32 p, i, d;
            p = (UInt32)(P * 65536.0);
            i = (UInt32)(I * 65536.0);
            d = (UInt32)(D * 65536.0);

			return Write_CRC(new byte[] { m_address, 29, (byte)(d >> 24), (byte)(d >> 16), (byte)(d >> 8), (byte)d,
                                          (byte)(p >> 24), (byte)(p >> 16), (byte)(p >> 8), (byte)p,
                                          (byte)(i >> 24), (byte)(i >> 16), (byte)(i >> 8), (byte)i,
                                          (byte)(qpps >> 24), (byte)(qpps >> 16), (byte)(qpps >> 8), (byte)qpps,
                                          0, 0 });
        }

        public bool GetM1ISpeed(out Int32 speed, out byte status)
        {
            byte[] arr;
            if (readcmd(m_address, 30, 5, out arr))
            {
                speed = (Int32)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]);
                status = arr[4];
                return true;
            }
            speed = 0;
            status = 0;
            return false;
        }

        public bool GetM2ISpeed(out Int32 speed, out byte status)
        {
            byte[] arr;
            if (readcmd(m_address, 31, 5, out arr))
            {
                speed = (Int32)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]);
                status = arr[4];
                return true;
            }
            speed = 0;
            status = 0;
            return false;
        }

        public bool M1Duty(Int32 duty)
        {
			return Write_CRC(new byte[] { m_address, 32, (byte)(duty >> 8), (byte)duty, 0, 0 });
        }

        public bool M2Duty(Int32 duty)
        {
			return Write_CRC(new byte[] { m_address, 33, (byte)(duty >> 8), (byte)duty, 0, 0 });
        }

        public bool MixedDuty(Int16 duty1, Int16 duty2)
        {
			return Write_CRC(new byte[] { m_address, 34, (byte)(duty1 >> 8), (byte)duty1, (byte)(duty2 >> 8), (byte)duty2, 0, 0 });
        }

        public bool M1Speed(Int32 speed)
        {
			return Write_CRC(new byte[] { m_address, 35, (byte)(speed >> 24), (byte)(speed >> 16), (byte)(speed >> 8), (byte)speed, 0, 0 });
        }

        public bool M2Speed(Int32 speed)
        {
			return Write_CRC(new byte[] { m_address, 36, (byte)(speed >> 24), (byte)(speed >> 16), (byte)(speed >> 8), (byte)speed, 0, 0 });
        }

        public bool MixedSpeed(Int32 speed1, Int32 speed2)
        {
			return Write_CRC(new byte[] { m_address, 37, (byte)(speed1 >> 24), (byte)(speed1 >> 16), (byte)(speed1 >> 8), (byte)speed1,
                                             (byte)(speed2 >> 24), (byte)(speed2 >> 16), (byte)(speed2 >> 8), (byte)speed2,
                                             0, 0 });
        }

        public bool M1SpeedAccel(UInt32 accel, Int32 speed)
        {
			return Write_CRC(new byte[] { m_address, 38, (byte)(accel >> 24), (byte)(accel >> 16), (byte)(accel >> 8), (byte)accel,
                                             (byte)(speed >> 24), (byte)(speed >> 16), (byte)(speed >> 8), (byte)speed,
                                             0, 0 });
        }

        public bool M2SpeedAccel(UInt32 accel, Int32 speed)
        {
			return Write_CRC(new byte[] { m_address, 39, (byte)(accel >> 24), (byte)(accel >> 16), (byte)(accel >> 8), (byte)accel,
                                             (byte)(speed >> 24), (byte)(speed >> 16), (byte)(speed >> 8), (byte)speed,
                                             0, 0 });
        }

        public bool MixedSpeedAccel(UInt32 accel, Int32 speed1, Int32 speed2)
        {
			return Write_CRC(new byte[] { m_address, 40, (byte)(accel >> 24), (byte)(accel >> 16), (byte)(accel >> 8), (byte)accel,
                                             (byte)(speed1 >> 24), (byte)(speed1 >> 16), (byte)(speed1 >> 8), (byte)speed1,
                                             (byte)(speed2 >> 24), (byte)(speed2 >> 16), (byte)(speed2 >> 8), (byte)speed2,
                                             0, 0 });
        }

        public bool M1SpeedDistance(Int32 speed, UInt32 distance, byte buffer)
        {
			return Write_CRC(new byte[] { m_address, 41, (byte)(speed >> 24), (byte)(speed >> 16), (byte)(speed >> 8), (byte)speed,
                                             (byte)(distance >> 24), (byte)(distance >> 16), (byte)(distance >> 8), (byte)distance,
                                             buffer, 0, 0 });
        }

        public bool M2SpeedDistance(Int32 speed, UInt32 distance, byte buffer)
        {
			return Write_CRC(new byte[] { m_address, 42, (byte)(speed >> 24), (byte)(speed >> 16), (byte)(speed >> 8), (byte)speed,
                                             (byte)(distance >> 24), (byte)(distance >> 16), (byte)(distance >> 8), (byte)distance,
                                             buffer, 0, 0 });
        }

        public bool MixedSpeedDistance(Int32 speed1, UInt32 distance1, Int32 speed2, UInt32 distance2, byte buffer)
        {
			return Write_CRC(new byte[] { m_address, 43, (byte)(speed1 >> 24), (byte)(speed1 >> 16), (byte)(speed1 >> 8), (byte)speed1,
                                             (byte)(distance1 >> 24), (byte)(distance1 >> 16), (byte)(distance1 >> 8), (byte)distance1,
                                             (byte)(speed2 >> 24), (byte)(speed2 >> 16), (byte)(speed2 >> 8), (byte)speed2,
                                             (byte)(distance2 >> 24), (byte)(distance1 >> 16), (byte)(distance2 >> 8), (byte)distance2,
                                             buffer, 0, 0 });
        }

        public bool M1SpeedAccelDistance(Int32 accel, UInt32 speed, UInt32 distance, byte buffer)
        {
			return Write_CRC(new byte[] { m_address, 44, (byte)(accel >> 24), (byte)(accel >> 16), (byte)(accel >> 8), (byte)accel,
                                             (byte)(speed >> 24), (byte)(speed >> 16), (byte)(speed >> 8), (byte)speed,
                                             (byte)(distance >> 24), (byte)(distance >> 16), (byte)(distance >> 8), (byte)distance,
                                             buffer, 0, 0 });
        }

        public bool M2SpeedAccelDistance(Int32 accel, UInt32 speed, UInt32 distance, byte buffer)
        {
			return Write_CRC(new byte[] { m_address, 45, (byte)(accel >> 24), (byte)(accel >> 16), (byte)(accel >> 8), (byte)accel,
                                             (byte)(speed >> 24), (byte)(speed >> 16), (byte)(speed >> 8), (byte)speed,
                                             (byte)(distance >> 24), (byte)(distance >> 16), (byte)(distance >> 8), (byte)distance,
                                             buffer, 0, 0 });
        }

        public bool MixedSpeedAccelDistance(UInt32 accel, Int32 speed1, UInt32 distance1, Int32 speed2, UInt32 distance2, byte buffer)
        {
			return Write_CRC(new byte[] { m_address, 46, (byte)(accel >> 24), (byte)(accel >> 16), (byte)(accel >> 8), (byte)accel,
                                             (byte)(speed1 >> 24), (byte)(speed1 >> 16), (byte)(speed1 >> 8), (byte)speed1,
                                             (byte)(distance1 >> 24), (byte)(distance1 >> 16), (byte)(distance1 >> 8), (byte)distance1,
                                             (byte)(speed2 >> 24), (byte)(speed2 >> 16), (byte)(speed2 >> 8), (byte)speed2,
                                             (byte)(distance2 >> 24), (byte)(distance1 >> 16), (byte)(distance2 >> 8), (byte)distance2,
                                             buffer, 0, 0 });
        }

        public bool GetBuffers(out byte buffer1, out byte buffer2)
        {
            byte[] arr;
            if (readcmd(m_address, 47, 2, out arr))
            {
                buffer1 = arr[0];
                buffer2 = arr[1];
                return true;
            }
            buffer1 = buffer2 = 0;
            return false;
        }

		public bool GetPWMs(out Int16 PWM1, out Int16 PWM2)
		{
			byte[] arr;
			if (readcmd(m_address, 48, 4, out arr))
			{
				PWM1 = (Int16)(arr[0] << 8 | arr[1]);
				PWM2 = (Int16)(arr[2] << 8 | arr[3]);
				return true;
			}
			PWM1 = PWM2 = 0;
			return false;
		}

		public bool GetCurrents(out Int16 current1, out Int16 current2)
        {
            byte[] arr;
            if (readcmd(m_address, 49, 4, out arr))
            {
                current1 = (Int16)(arr[0] << 8 | arr[1]);
                current2 = (Int16)(arr[2] << 8 | arr[3]);
                return true;
            }
            current1 = current2 = 0;
            return false;
        }

        public bool MixedSpeedAccel2(Int32 accel1, UInt32 speed1, Int32 accel2, UInt32 speed2)
        {
			return Write_CRC(new byte[] { m_address, 50, (byte)(accel1 >> 24), (byte)(accel1 >> 16), (byte)(accel1 >> 8), (byte)accel1,
                                             (byte)(speed1 >> 24), (byte)(speed1 >> 16), (byte)(speed1 >> 8), (byte)speed1,
                                             (byte)(accel2 >> 24), (byte)(accel2 >> 16), (byte)(accel2 >> 8), (byte)accel2,
                                             (byte)(speed2 >> 24), (byte)(speed2 >> 16), (byte)(speed2 >> 8), (byte)speed2,
                                             0, 0 });
        }

        public bool MixedSpeedAccelDistance2(Int32 accel1, UInt32 speed1, UInt32 distance1, Int32 accel2, UInt32 speed2, UInt32 distance2, byte buffer)
        {
			return Write_CRC(new byte[] { m_address, 51, (byte)(accel1 >> 24), (byte)(accel1 >> 16), (byte)(accel1 >> 8), (byte)accel1,
                                             (byte)(speed1 >> 24), (byte)(speed1 >> 16), (byte)(speed1 >> 8), (byte)speed1,
                                             (byte)(distance1 >> 24), (byte)(distance1 >> 16), (byte)(distance1 >> 8), (byte)distance1,
                                             (byte)(accel2 >> 24), (byte)(accel2 >> 16), (byte)(accel2 >> 8), (byte)accel2,
                                             (byte)(speed2 >> 24), (byte)(speed2 >> 16), (byte)(speed2 >> 8), (byte)speed2,
                                             (byte)(distance2 >> 24), (byte)(distance1 >> 16), (byte)(distance2 >> 8), (byte)distance2,
                                             buffer, 0, 0 });
        }

        public bool M1DutyAccel(Int16 duty, UInt32 accel)
        {
			return Write_CRC(new byte[] { m_address, 52, (byte)(duty >> 8), (byte)duty,
                                             (byte)(accel >> 24), (byte)(accel >> 16),(byte)(accel >> 8), (byte)accel,
                                             0, 0 });
        }

        public bool M2DutyAccel(Int16 duty, UInt32 accel)
        {
			return Write_CRC(new byte[] { m_address, 53, (byte)(duty >> 8), (byte)duty,
                                             (byte)(accel >> 24), (byte)(accel >> 16),(byte)(accel >> 8), (byte)accel,
                                             0, 0 });
        }

        public bool MixedDutyAccel(Int16 duty1, UInt32 accel1, Int16 duty2, UInt32 accel2)
        {
			return Write_CRC(new byte[] { m_address, 54, (byte)(duty1 >> 8), (byte)duty1,
                                             (byte)(accel1 >> 24), (byte)(accel1 >> 16),(byte)(accel1 >> 8), (byte)accel1,
                                             (byte)(duty2 >> 8), (byte)duty2,
                                             (byte)(accel2 >> 24), (byte)(accel2 >> 16),(byte)(accel2 >> 8), (byte)accel2,
                                             0, 0 });
        }

        public bool GetM1VelocityConstants(out double p, out double i, out double d, out UInt32 qpps)
        {
            byte[] arr;
            if (readcmd(m_address, 55, 16, out arr))
            {
                p = (double)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]) / 65536;
                i = (double)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]) / 65536;
                d = (double)(arr[8] << 24 | arr[9] << 16 | arr[10] << 8 | arr[11]) / 65536;
                qpps = (UInt32)(arr[12] << 24 | arr[13] << 16 | arr[14] << 8 | arr[15]);
                return true;
            }
            p = i = d = qpps = 0;
            return false;
        }

        public bool GetM2VelocityConstants(out double p, out double i, out double d, out UInt32 qpps)
        {
            byte[] arr;
            if (readcmd(m_address, 56, 16, out arr))
            {
                p = (double)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]) / 65536;
                i = (double)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]) / 65536;
                d = (double)(arr[8] << 24 | arr[9] << 16 | arr[10] << 8 | arr[11]) / 65536;
                qpps = (UInt32)(arr[12] << 24 | arr[13] << 16 | arr[14] << 8 | arr[15]);
                return true;
            }
            p = i = d = qpps = 0;
            return false;
        }

        public bool SetMainVoltageLimits(double Min, double Max)
        {
            UInt16 min = (UInt16)(Min * 10.0);
            UInt16 max = (UInt16)(Max * 10.0);
			return Write_CRC(new byte[] { m_address, 57, (byte)(min >> 8), (byte)min,
                                             (byte)(max >> 8), (byte)max,
                                             0, 0 });
        }

        public bool SetLogicVoltageLimits(double Min, double Max)
        {
            UInt16 min = (UInt16)(Min * 10.0);
            UInt16 max = (UInt16)(Max * 10.0);
			return Write_CRC(new byte[] { m_address, 58, (byte)(min >> 8), (byte)min,
                                             (byte)(max >> 8), (byte)max,
                                             0, 0 });
        }

        public bool GetMainVoltageLimits(out double min, out double max)
        {
            byte[] arr;
            if (readcmd(m_address, 59, 4, out arr))
            {
                min = (double)(arr[0] << 8 | arr[1])/10.0;
                max = (double)(arr[2] << 8 | arr[3])/10.0;
                return true;
            }
            min = max = 0;
            return false;
        }

        public bool GetLogicVoltageLimits(out double min, out double max)
        {
            byte[] arr;
            if (readcmd(m_address, 60, 4, out arr))
            {
                min = (double)(arr[0] << 8 | arr[1])/10.0;
                max = (double)(arr[2] << 8 | arr[3])/10.0;
                return true;
            }
            min = max = 0;
            return false;
        }

        public bool SetM1PositionConstants(double P, double I, double D, UInt32 imax, UInt32 deadzone, Int32 minlimit, Int32 maxlimit)
        {
            UInt32 p, i, d;
            p = (UInt32)(P * 1024.0);
            i = (UInt32)(I * 1024.0);
            d = (UInt32)(D * 1024.0);

			return Write_CRC(new byte[] { m_address, 61, (byte)(d >> 24), (byte)(d >> 16), (byte)(d >> 8), (byte)d,
                                          (byte)(p >> 24), (byte)(p >> 16), (byte)(p >> 8), (byte)p,
                                          (byte)(i >> 24), (byte)(i >> 16), (byte)(i >> 8), (byte)i,
                                          (byte)(imax >> 24), (byte)(imax >> 16), (byte)(imax >> 8), (byte)imax,
                                          (byte)(deadzone >> 24), (byte)(deadzone >> 16), (byte)(deadzone >> 8), (byte)deadzone,
                                          (byte)(minlimit >> 24), (byte)(minlimit >> 16), (byte)(minlimit >> 8), (byte)minlimit,
                                          (byte)(maxlimit >> 24), (byte)(maxlimit >> 16), (byte)(maxlimit >> 8), (byte)maxlimit,
                                          0, 0 });
        }

        public bool SetM2PositionConstants(double P, double I, double D, UInt32 imax, UInt32 deadzone, Int32 minlimit, Int32 maxlimit)
        {
            UInt32 p, i, d;
            p = (UInt32)(P * 1024.0);
            i = (UInt32)(I * 1024.0);
            d = (UInt32)(D * 1024.0);

			return Write_CRC(new byte[] { m_address, 62, (byte)(d >> 24), (byte)(d >> 16), (byte)(d >> 8), (byte)d,
                                          (byte)(p >> 24), (byte)(p >> 16), (byte)(p >> 8), (byte)p,
                                          (byte)(i >> 24), (byte)(i >> 16), (byte)(i >> 8), (byte)i,
                                          (byte)(imax >> 24), (byte)(imax >> 16), (byte)(imax >> 8), (byte)imax,
                                          (byte)(deadzone >> 24), (byte)(deadzone >> 16), (byte)(deadzone >> 8), (byte)deadzone,
                                          (byte)(minlimit >> 24), (byte)(minlimit >> 16), (byte)(minlimit >> 8), (byte)minlimit,
                                          (byte)(maxlimit >> 24), (byte)(maxlimit >> 16), (byte)(maxlimit >> 8), (byte)maxlimit,
                                          0, 0 });
        }

        public bool GetM1PositionConstants(out double p, out double i, out double d, out UInt32 imax, out UInt32 deadzone, out Int32 minlimit, out Int32 maxlimit)
        {
            byte[] arr;
            if (readcmd(m_address, 63, 28, out arr))
            {
                p = (double)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]) / 1024;
                i = (double)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]) / 1024;
                d = (double)(arr[8] << 24 | arr[9] << 16 | arr[10] << 8 | arr[11]) / 1024;
                imax = (UInt32)(arr[12] << 24 | arr[13] << 16 | arr[14] << 8 | arr[15]);
                deadzone = (UInt32)(arr[16] << 24 | arr[17] << 16 | arr[18] << 8 | arr[19]);
                minlimit = (Int32)(arr[20] << 24 | arr[21] << 16 | arr[22] << 8 | arr[23]);
                maxlimit = (Int32)(arr[24] << 24 | arr[25] << 16 | arr[26] << 8 | arr[27]);
                return true;
            }
            p = i = d = imax = deadzone = 0;
            minlimit = maxlimit = 0;
            return false;
        }

        public bool GetM2PositionConstants(out double p, out double i, out double d, out UInt32 imax, out UInt32 deadzone, out Int32 minlimit, out Int32 maxlimit)
        {
            byte[] arr;
            if (readcmd(m_address, 64, 28, out arr))
            {
                p= (double)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]) / 1024;
                i = (double)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]) / 1024;
                d = (double)(arr[8] << 24 | arr[9] << 16 | arr[10] << 8 | arr[11]) / 1024;
                imax = (UInt32)(arr[12] << 24 | arr[13] << 16 | arr[14] << 8 | arr[15]);
                deadzone = (UInt32)(arr[16] << 24 | arr[17] << 16 | arr[18] << 8 | arr[19]);
                minlimit = (Int32)(arr[20] << 24 | arr[21] << 16 | arr[22] << 8 | arr[23]);
                maxlimit = (Int32)(arr[24] << 24 | arr[25] << 16 | arr[26] << 8 | arr[27]);
                return true;
            }
            p = i = d = imax = deadzone = 0;
            minlimit = maxlimit = 0;
            return false;
        }

        public bool M1SpeedAccelDeccelPosition(UInt32 accel, UInt32 speed, UInt32 deccel, Int32 position, byte buffer)
        {
			return Write_CRC(new byte[] { m_address, 65, (byte)(accel >> 24), (byte)(accel >> 16), (byte)(accel >> 8), (byte)accel,
                                             (byte)(speed >> 24), (byte)(speed >> 16), (byte)(speed >> 8), (byte)speed,
                                             (byte)(deccel >> 24), (byte)(deccel >> 16), (byte)(deccel >> 8), (byte)deccel,
                                             (byte)(position >> 24), (byte)(position >> 16), (byte)(position >> 8), (byte)position,
                                             buffer, 0, 0 });
        }

        public bool M2SpeedAccelDeccelPosition(UInt32 accel, UInt32 speed, UInt32 deccel, Int32 position, byte buffer)
        {
            return Write_CRC(new byte[] { m_address, 66, (byte)(accel >> 24), (byte)(accel >> 16), (byte)(accel >> 8), (byte)accel,
                                             (byte)(speed >> 24), (byte)(speed >> 16), (byte)(speed >> 8), (byte)speed,
                                             (byte)(deccel >> 24), (byte)(deccel >> 16), (byte)(deccel >> 8), (byte)deccel,
                                             (byte)(position >> 24), (byte)(position >> 16), (byte)(position >> 8), (byte)position,
                                             buffer, 0, 0 });
        }

        public bool MixedSpeedAccelDeccelPosition(UInt32 accel1, UInt32 speed1, UInt32 deccel1, Int32 position1, UInt32 accel2, UInt32 speed2, UInt32 deccel2, Int32 position2, byte buffer)
        {
            return Write_CRC(new byte[] { m_address, 67, (byte)(accel1 >> 24), (byte)(accel1 >> 16), (byte)(accel1 >> 8), (byte)accel1,
                                             (byte)(speed1 >> 24), (byte)(speed1 >> 16), (byte)(speed1 >> 8), (byte)speed1,
                                             (byte)(deccel1 >> 24), (byte)(deccel1 >> 16), (byte)(deccel1 >> 8), (byte)deccel1,
                                             (byte)(position1 >> 24), (byte)(position1 >> 16), (byte)(position1 >> 8), (byte)position1,
                                             (byte)(accel2 >> 24), (byte)(accel2 >> 16), (byte)(accel2 >> 8), (byte)accel2,
                                             (byte)(speed2 >> 24), (byte)(speed2 >> 16), (byte)(speed2 >> 8), (byte)speed2,
                                             (byte)(deccel2 >> 24), (byte)(deccel2 >> 16), (byte)(deccel2 >> 8), (byte)deccel2,
                                             (byte)(position2 >> 24), (byte)(position2 >> 16), (byte)(position2 >> 8), (byte)position2,
                                             buffer, 0, 0 });
        }

		public bool SetPinModes(byte s3mode, byte s4mode, byte s5mode)
		{
			return Write_CRC(new byte[] { m_address, 74, s3mode, s4mode, s5mode, 0, 0 });
		}

		public bool GetPinModes(out byte s3mode, out byte s4mode, out byte s5mode)
		{
			byte[] arr;
			if (readcmd(m_address, 75, 3, out arr))
			{
				s3mode = arr[0];
				s4mode = arr[1];
				s5mode = arr[2];
				if(s3mode>3)
					s3mode=0;
				if (s4mode > 5)
					s4mode = 0;
				if (s5mode > 5)
					s5mode = 0;
				return true;
			}
			s3mode = 0;
			s4mode = 0;
			s5mode = 0;
			return false;
		}

		public bool SetDeadBand(byte min, byte max)
		{
			return Write_CRC(new byte[] { m_address, 76, min, max, 0, 0 });
		}

		public bool GetDeadBand(out byte min, out byte max)
		{
			byte[] arr;
			if (readcmd(m_address, 77, 2, out arr))
			{
				min = arr[0];
				max = arr[1];
				if (min > 250)
					min = 250;
				if (max > 250)
					max = 250;
				return true;
			}
			min = 0;
			max = 0;
			return false;
		}

		public bool Defaults()
		{
			bool result = Write_CRC(new byte[] { m_address, 80, 0, 0 });
			if(result)
				m_address = 0x80;
			return result;
		}

		public bool GetTemperature(out double temperature)
        {
            byte[] arr;
            if (readcmd(m_address, 82, 2, out arr))
            {
                temperature = (double)(arr[0] << 8 | arr[1])/10.0;
                return true;
            }
            temperature = 0;
            return false;
        }

		public bool GetTemperature2(out double temperature2)
		{
			byte[] arr;
			if (readcmd(m_address, 83, 2, out arr))
			{
				temperature2 = (double)(arr[0] << 8 | arr[1]) / 10.0;
				return true;
			}
			temperature2 = 0;
			return false;
		}

		public bool VelocityAutotuneM1(UInt32 amplitude)
        {
			return Write_CRC(new byte[] { m_address, 84, (byte)(amplitude >> 24), (byte)(amplitude >> 16), (byte)(amplitude >> 8), (byte)amplitude,
                                             0, 0 });
        }

		public bool VelocityAutotuneM2(UInt32 amplitude)
        {
			return Write_CRC(new byte[] { m_address, 85, (byte)(amplitude >> 24), (byte)(amplitude >> 16), (byte)(amplitude >> 8), (byte)amplitude,
                                             0, 0 });
        }

		public bool PositionAutotuneM1(UInt32 amplitude)
        {
			return Write_CRC(new byte[] { m_address, 86, (byte)(amplitude >> 24), (byte)(amplitude >> 16), (byte)(amplitude >> 8), (byte)amplitude,
                                             0, 0 });
        }

		public bool PositionAutotuneM2(UInt32 amplitude)
        {
			return Write_CRC(new byte[] { m_address, 87, (byte)(amplitude >> 24), (byte)(amplitude >> 16), (byte)(amplitude >> 8), (byte)amplitude,
                                             0, 0 });
        }

        public bool GetM1Data(out double a, out double p)
        {
			byte[] arr;
			if (readcmd(m_address, 88, 8, out arr))
			{
				a = (double)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]);
				p = (double)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]);
				return true;
			}
			a = p = 0;
			return false;
		}

		public bool GetM2Data(out double a, out double p)
		{
			byte[] arr;
			if (readcmd(m_address, 89, 8, out arr))
			{
				a = (double)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]);
				p = (double)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]);
				return true;
			}
			a = p = 0;
			return false;
		}

		public bool GetStatus(out UInt16 status)
        {
            byte[] arr;
            if (readcmd(m_address, 90, 2, out arr))
            {
                status = (UInt16)(arr[0]<<8 | arr[1]);
                return true;
            }
            status = 0;
            return false;
        }

        public bool GetEncoderModes(out byte m1mode, out byte m2mode)
        {
            byte[] arr;
            if (readcmd(m_address, 91, 2, out arr))
            {
                m1mode = arr[0];
                m2mode = arr[1];
				return true;
            }
            m1mode = m2mode = 0;
            return false;
        }

        public bool SetEncoder1Mode(byte mode)
        {
			return Write_CRC(new byte[] { m_address, 92, mode, 0, 0 });
        }

        public bool SetEncoder2Mode(byte mode)
        {
			return Write_CRC(new byte[] { m_address, 93, mode, 0, 0 });
        }

		public bool WriteNVM()
        {
			return Write_CRC(new byte[] { m_address, 94, 0xE2, 0x2E, 0xAB, 0x7A, 0, 0 });
        }

		public bool ReadNVM()
		{
			return Write_CRC(new byte[] { m_address, 95, 0, 0 });
		}

		public bool SetConfig(UInt16 config)
        {
			byte oldaddress = m_address;
			m_address = (byte)(((config >> 8) & 0x07) + 0x80);
			return Write_CRC(new byte[] { oldaddress, 98, (byte)(config >> 8), (byte)config, 0, 0 });
        }

        public bool GetConfig(out UInt16 config)
        {
            byte[] arr;
            if (readcmd(m_address, 99, 2, out arr))
            {
                config = (UInt16)(arr[0] << 8 | arr[1]);
                return true;
            }
            config = 0;
            return false;
        }

		public bool TestM1QPPS(out UInt32 qpps)
		{
			serialPort.ReadTimeout = 5000;
			byte[] arr;
			if (readcmd(m_address, 100, 2, out arr))
			{
				serialPort.ReadTimeout = 100;
				Int16 speed = (Int16)(arr[0] << 8 | arr[1]);
				qpps = (UInt32)Math.Abs(speed)*4;
				return true;
			}
			serialPort.ReadTimeout = 100;
			qpps = 0;
			return false;
		}

		public bool TestM2QPPS(out UInt32 qpps)
		{
			byte[] arr;
			if (readcmd(m_address, 101, 2, out arr))
			{
				Int16 ispeed = (Int16)(arr[0] << 8 | arr[1]);
				qpps = (UInt32)Math.Abs(ispeed * 300);
				return true;
			}
			qpps = 0;
			return false;
		}

		public bool SetM1LR(double L, double R)
		{
			UInt32 l,r;
			l = (UInt32)(L * 0x1000000);
			r = (UInt32)(R * 0x1000000);

			return Write_CRC(new byte[] { m_address, 128, (byte)(l >> 24), (byte)(l >> 16), (byte)(l >> 8), (byte)l,
                                          (byte)(r >> 24), (byte)(r >> 16), (byte)(r >> 8), (byte)r,
                                          0, 0 });
		}

		public bool SetM2LR(double L, double R)
		{
			UInt32 l, r;
			l = (UInt32)(L * 0x1000000);
			r = (UInt32)(R * 0x1000000);

			return Write_CRC(new byte[] { m_address, 129, (byte)(l >> 24), (byte)(l >> 16), (byte)(l >> 8), (byte)l,
                                          (byte)(r >> 24), (byte)(r >> 16), (byte)(r >> 8), (byte)r,
                                          0, 0});
		}

		public bool GetM1LR(out double l, out double r)
		{
			byte[] arr;
			if (readcmd(m_address, 130, 8, out arr))
			{
				l = (double)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]) / 0x1000000;
				r = (double)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]) / 0x1000000;
				return true;
			}
			l = r = 0;
			return false;
		}

		public bool GetM2LR(out double l, out double r)
		{
			byte[] arr;
			if (readcmd(m_address, 131, 8, out arr))
			{
				l = (double)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]) / 0x1000000;
				r = (double)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]) / 0x1000000;
				return true;
			}
			l = r = 0;
			return false;
		}

		public bool CalibrateLR()
		{
			return Write_CRC(new byte[] { m_address, 132, 0, 0 });
		}

		public bool SetM1Current(double fmin, double fmax)
		{
			UInt32 min,max;
			min = (UInt32)(fmin * 100);
			max = (UInt32)(fmax * 100);

			return Write_CRC(new byte[] { m_address, 133, (byte)(max >> 24), (byte)(max >> 16), (byte)(max >> 8), (byte)max,
                                          (byte)(min >> 24), (byte)(min >> 16), (byte)(min >> 8), (byte)min,
                                          0, 0 });
		}

		public bool SetM2Current(double fmin, double fmax)
		{
			UInt32 min, max;
			min = (UInt32)(fmin * 100);
			max = (UInt32)(fmax * 100);

			return Write_CRC(new byte[] { m_address, 134, (byte)(max >> 24), (byte)(max >> 16), (byte)(max >> 8), (byte)max,
                                          (byte)(min >> 24), (byte)(min >> 16), (byte)(min >> 8), (byte)min,
                                          0, 0 });
		}

		public bool GetM1Current(out double min, out double max)
		{
			byte[] arr;
			if (readcmd(m_address, 135, 8, out arr))
			{
				max = (double)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]) / 100;
				min = (double)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]) / 100;
				return true;
			}
			max = min = 0;
			return false;
		}

		public bool GetM2Current(out double min, out double max)
		{
			byte[] arr;
			if (readcmd(m_address, 136, 8, out arr))
			{
				max = (double)(arr[0] << 24 | arr[1] << 16 | arr[2] << 8 | arr[3]) / 100;
				min = (double)(arr[4] << 24 | arr[5] << 16 | arr[6] << 8 | arr[7]) / 100;
				return true;
			}
			max = min = 0;
			return false;
		}

		public bool SetDOUT(byte i, byte action)
		{
			return Write_CRC(new byte[] { m_address, 137, (byte)i,(byte)action,
                                          0, 0 });
		}

		public bool GetDOUT(out byte len, out byte[] actions)
		{
			DumpGarbage();

			serialPort.Write(new byte[] { m_address, 138 }, 0, 2);
			try{
				crc_clear();
				crc_update(m_address);
				crc_update(138);
				len = (byte)serialPort.ReadByte();
				crc_update(len);
				actions = new byte[len];
				for(uint i=0;i<len;i++){
					actions[i] = (byte)serialPort.ReadByte();
					crc_update(actions[i]);
				}
				UInt16 ccrc = (UInt16)(serialPort.ReadByte()<<8);
				ccrc |= (UInt16)serialPort.ReadByte();
				if(crc_get()==ccrc)
					return true;
			}
			catch(Exception){
				//Debug.WriteLine("GetDOUT Timeout");
				len = 0;
				actions = null;
			}
			return false;
		}

		public bool SetPriorityLevels(byte lvl1,byte lvl2,byte lvl3)
		{
			return Write_CRC(new byte[] { m_address, 139, lvl1, lvl2, lvl3,
                                          0, 0 });
		}

		public bool GetPriorityLevels(out byte lvl1, out byte lvl2, out byte lvl3)
		{
			byte[] arr;
			if (readcmd(m_address, 140, 3, out arr))
			{
				lvl1 = arr[0];
				lvl2 = arr[1];
				lvl3 = arr[2];
				if (lvl1>3)
					lvl1=0;
				if (lvl2 > 3)
					lvl2 = 0;
				if (lvl3 > 3)
					lvl3 = 0;
				return true;
			}
			lvl1 = lvl2 = lvl3 = 0;
			return false;
		}

		public bool SetMixed(byte mixed)
		{
			return Write_CRC(new byte[] { m_address, 141, m_address, mixed,
                                          0, 0 });
		}

		public bool GetMixed(out byte mixed)
		{
			byte[] arr;
			if (readcmd(m_address, 142, 1, out arr))
			{
				mixed = arr[0];
				return true;
			}
			mixed = 0;
			return false;
		}

		public bool SetSignal(byte i, byte type, byte mode, byte target, byte minaction, byte maxaction,
							  UInt32 timeout, Int32 loadhome, Int32 min, Int32 max, Int32 center,
							  UInt32 deadband, Int32 powerexp, Int32 minout, Int32 maxout, UInt32 powermin)
		{
			return Write_CRC(new byte[] { m_address, 143,
										  i,
										  type,
										  mode,
										  target,
										  minaction,
										  maxaction,
										  (byte)(timeout >> 24), (byte)(timeout >> 16), (byte)(timeout >> 8), (byte)timeout,
										  (byte)(loadhome >> 24), (byte)(loadhome >> 16), (byte)(loadhome >> 8), (byte)loadhome,
										  (byte)(min >> 24), (byte)(min >> 16), (byte)(min >> 8), (byte)min,
										  (byte)(max >> 24), (byte)(max >> 16), (byte)(max >> 8), (byte)max,
										  (byte)(center >> 24), (byte)(center >> 16), (byte)(center >> 8), (byte)center,
										  (byte)(deadband >> 24), (byte)(deadband >> 16), (byte)(deadband >> 8), (byte)deadband,
										  (byte)(powerexp >> 24), (byte)(powerexp >> 16), (byte)(powerexp >> 8), (byte)powerexp,
										  (byte)(minout >> 24), (byte)(minout >> 16), (byte)(minout >> 8), (byte)minout,
										  (byte)(maxout >> 24), (byte)(maxout >> 16), (byte)(maxout >> 8), (byte)maxout,
										  (byte)(powermin >> 24), (byte)(powermin >> 16), (byte)(powermin >> 8), (byte)powermin,
                                          0, 0 });
		}

		public bool GetSignals(out byte len,
							   out byte[] types, out byte[] modes, out byte[] targets, out byte[] minactions, out byte[] maxactions,
							   out UInt32[] timeouts, out Int32[] loadhomes, out Int32[] mins, out Int32[] maxs, out Int32[] centers,
							   out UInt32[] deadbands, out Int32[] powerexps, out Int32[] minouts, out Int32[] maxouts, out UInt32[] powermins)
		{
			DumpGarbage();

			serialPort.Write(new byte[] { m_address, 144 }, 0, 2);
			try{
				crc_clear();
				crc_update(m_address);
				crc_update(144);
				len = (byte)serialPort.ReadByte();
				crc_update(len);
				types = new byte[len];
				modes = new byte[len];
				targets = new byte[len];
				minactions = new byte[len];
				maxactions = new byte[len];
				timeouts = new UInt32[len];
				loadhomes = new Int32[len];
				mins = new Int32[len];
				maxs = new Int32[len];
				centers = new Int32[len];
				deadbands = new UInt32[len];
				powerexps = new Int32[len];
				minouts = new Int32[len];
				maxouts = new Int32[len];
				powermins = new UInt32[len];
				byte[] arr = new byte[len*45];
				for (uint i = 0; i < len*45; i++){
					arr[i] = (byte)serialPort.ReadByte();
					crc_update(arr[i]);
				}
				UInt16 ccrc = (UInt16)(serialPort.ReadByte()<<8);
				ccrc |= (UInt16)serialPort.ReadByte();
				if (crc_get() != ccrc)
					return false;

				UInt32 address = 0;
				for (uint i = 0; i < len; i++)
				{
					types[i] = arr[address++];
					modes[i] = arr[address++];
					targets[i] = arr[address++];
					minactions[i] = arr[address++];
					maxactions[i] = arr[address++];
					timeouts[i] = (UInt32)(arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3]);
					address += 4;
					loadhomes[i] = arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3];
					address += 4;
					mins[i] = arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3];
					address+=4;
					maxs[i] = arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3];
					address += 4;
					centers[i] = arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3]; 
					address += 4;
					deadbands[i] = (UInt32)(arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3]);
					address += 4;
					powerexps[i] = (Int32)(arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3]);
					address += 4;
					minouts[i] = (Int32)(arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3]);
					address += 4;
					maxouts[i] = (Int32)(arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3]);
					address += 4;
					powermins[i] = (UInt32)(arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3]);
					address += 4;
				}
				return true;
			}
			catch(Exception){
				//Debug.WriteLine("GetSignals Timeout");
				len = 0;
				types = null;
				modes = null;
				targets = null;
				minactions = null;
				maxactions = null;
				timeouts = null;
				loadhomes = null;
				mins = null;
				maxs = null;
				centers = null;
				deadbands = null;
				powerexps = null;
				minouts = null;
				maxouts = null;
				powermins = null;
			}
			return false;
		}

		public bool SetStream(byte i, byte type, UInt32 rate, UInt32 timeout)
		{
			return Write_CRC(new byte[] { m_address, 145,
										  i,
										  type,
										  (byte)(rate >> 24), (byte)(rate >> 16), (byte)(rate >> 8), (byte)rate,
										  (byte)(timeout >> 24), (byte)(timeout >> 16), (byte)(timeout >> 8), (byte)timeout,
                                          0, 0 });
		}

		public bool GetStreams(out byte len, out byte[] types, out UInt32[] rates, out UInt32[] timeouts)
		{
			DumpGarbage();

			serialPort.Write(new byte[] { m_address, 146 }, 0, 2);
			try{
				crc_clear();
				crc_update(m_address);
				crc_update(146);
				Int32 count = serialPort.BytesToRead;
				len = (byte)serialPort.ReadByte();
				crc_update(len);
				types = new byte[len];
				rates = new UInt32[len];
				timeouts = new UInt32[len];
				byte[] arr = new byte[len * 9];
				for (uint i = 0; i < len * 9; i++){
					arr[i] = (byte)serialPort.ReadByte();
					crc_update(arr[i]);
				}
				UInt16 ccrc = (UInt16)(serialPort.ReadByte()<<8);
				ccrc |= (UInt16)serialPort.ReadByte();
				if (crc_get() != ccrc)
					return false;

				UInt32 address = 0;
				for (uint i = 0; i < len; i++)
				{
					types[i] = arr[address++];
					rates[i] = (UInt32)(arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3]);
					address += 4;
					timeouts[i] = (UInt32)(arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3]);
					address += 4;
				}
				return true;
			}
			catch(Exception){
				//Debug.WriteLine("GetStreams Timeout");
				len = 0;
				types = null;
				rates = null;
				timeouts = null;
			}
			return false;
		}

		public bool GetSignalValues(out byte len,
							   out Int32[] commands, out Int32[] positions, out Int32[] percents, out Int32[] speeds, out Int32[] speedss)
		{
			DumpGarbage();

			serialPort.Write(new byte[] { m_address, 147 }, 0, 2);
			try{
				crc_clear();
				crc_update(m_address);
				crc_update(147);
				len = (byte)serialPort.ReadByte();
				crc_update(len);
				commands = new Int32[len];
				positions = new Int32[len];
				percents = new Int32[len];
				speeds = new Int32[len];
				speedss = new Int32[len];
				byte[] arr = new byte[len * 20];
				for (uint i = 0; i < len * 20; i++){
					arr[i] = (byte)serialPort.ReadByte();
					crc_update(arr[i]);
				}
				UInt16 ccrc = (UInt16)(serialPort.ReadByte()<<8);
				ccrc |= (UInt16)serialPort.ReadByte();
				if (crc_get() != ccrc)
					return false;

				UInt32 address = 0;
				for (uint i = 0; i < len; i++)
				{
					commands[i] = arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3];
					address += 4;
					positions[i] = arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3];
					address += 4;
					percents[i] = arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3];
					address += 4;
					speeds[i] = arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3];
					address += 4;
					speedss[i] = arr[address] << 24 | arr[address + 1] << 16 | arr[address + 2] << 8 | arr[address + 3];
					address += 4;
				}
				return true;
			}
			catch(Exception){
				//Debug.WriteLine("GetSignalValues Timeout");
				len = 0;
				commands = null;
				positions = null;
				percents = null;
				speeds = null;
				speedss = null;
			}
			return false;
		}

		public bool SetMode(byte mode)
		{
			return Write_CRC(new byte[] { m_address, 148, mode,
                                          0, 0 });
		}

		public bool GetMode(out byte mode)
		{
			byte[] arr;
			if (readcmd(m_address, 149, 1, out arr))
			{
				mode = (byte)(arr[0] & 0x81);
				return true;
			}
			mode = 0;
			return false;
		}

	}
}
