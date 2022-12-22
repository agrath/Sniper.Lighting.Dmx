using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Sniper.Lighting.DMX.Properties;
using FT_HANDLE = System.UInt32;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.ExceptionServices;

namespace Sniper.Lighting.DMX
{
    public class DMXProUSB
    {
        protected byte[] buffer;
        protected int busLength;
        protected Dictionary<Guid, QueueBuffer> queueBuffers;
        protected uint handle;
        protected bool done = false;
        private int bytesWritten = 0;
        protected FT_STATUS status;

        protected const byte BITS_8 = 8;
        protected const byte STOP_BITS_2 = 2;
        protected const byte PARITY_NONE = 0;
        protected const UInt16 FLOW_NONE = 0;
        protected const byte PURGE_RX = 1;
        protected const byte PURGE_TX = 2;
        private const UInt32 FT_LIST_NUMBER_ONLY = 0x80000000;

        protected const int GET_WIDGET_PARAMS = 3;
        protected const int GET_WIDGET_SN = 10;
        private const int GET_WIDGET_PARAMS_REPLY = 3;
        private const int SET_WIDGET_PARAMS = 4;
        private const int SET_DMX_RX_MODE = 5;
        protected const int SET_DMX_TX_MODE = 6;
        private const int ONE_BYTE = 1;

        internal byte getDefaultForChannel(int channel)
        {
            if (defaults != null && defaults.Values != null && defaults.Values.Length < channel) return defaults.Values[channel];
            return 0;
        }

        private const byte DMX_START_CODE = 0x7E;
        protected const byte DMX_END_CODE = 0xE7;
        private const byte OFFSET = 0xFF;
        private const int DMX_HEADER_LENGTH = 4;
        private const int BYTE_LENGTH = 8;
        protected const bool NO_RESPONSE = false;
        private const int DMX_PACKET_SIZE = 512;
        public bool Connected = false;
        public int StartCounter = 0;

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_Open(UInt32 uiPort, ref uint ftHandle);
        [DllImport("FTD2XX.dll")]
        protected static extern unsafe FT_STATUS FT_OpenEx(void* pvArg1, UInt32 dwFlags, ref FT_HANDLE ftHandle);

        protected DmxDefaults defaults;
        internal void setDefaults(DmxDefaults newDefaults)
        {
            defaults = newDefaults;
            if (buffer != null)
            {
                int i = 0;
                foreach (var defaultValue in newDefaults.Values)
                {
                    buffer[i++] = defaultValue;
                }
            }
        }

        [DllImport("FTD2XX.dll")]
        extern static unsafe FT_STATUS FT_ListDevices(void* pvArg1, void* pvArg2, UInt32 dwFlags);	// FT_ListDevices by number only
        [DllImport("FTD2XX.dll")]
        extern static unsafe FT_STATUS FT_ListDevices(UInt32 pvArg1, void* pvArg2, UInt32 dwFlags);	// FT_ListDevcies by serial number or description by index only
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_Close(uint ftHandle);
        [DllImport("FTD2XX.dll")]
        static extern FT_STATUS FT_Read(uint ftHandle, IntPtr lpBuffer, UInt32 dwBytesToRead, ref UInt32 lpdwBytesReturned);
        [DllImport("FTD2XX.dll")]
        static extern FT_STATUS FT_Write(uint ftHandle, IntPtr lpBuffer, UInt32 dwBytesToRead, ref UInt32 lpdwBytesWritten);
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_SetDataCharacteristics(uint ftHandle, byte uWordLength, byte uStopBits, byte uParity);
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_SetFlowControl(uint ftHandle, char usFlowControl, byte uXon, byte uXoff);
        [DllImport("FTD2XX.dll")]
        static extern FT_STATUS FT_GetModemStatus(uint ftHandle, ref UInt32 lpdwModemStatus);
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_Purge(uint ftHandle, UInt32 dwMask);
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_SetBreakOn(uint ftHandle);
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_SetBreakOff(uint ftHandle);
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_GetStatus(uint ftHandle, ref UInt32 lpdwAmountInRxQueue, ref UInt32 lpdwAmountInTxQueue, ref UInt32 lpdwEventStatus);
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_ResetDevice(uint ftHandle);
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_SetDivisor(uint ftHandle, char usDivisor);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_SetDtr(FT_HANDLE ftHandle);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_ClrDtr(FT_HANDLE ftHandle);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_SetRts(FT_HANDLE ftHandle);
        [DllImport("FTD2XX.dll")]
        protected static extern unsafe FT_STATUS FT_ClrRts(FT_HANDLE ftHandle);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_GetQueueStatus(FT_HANDLE ftHandle, ref UInt32 lpdwAmountInRxQueue);
        [DllImport("FTD2XX.dll")]
        protected static extern unsafe FT_STATUS FT_GetLatencyTimer(FT_HANDLE ftHandle, ref byte pucTimer);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_SetLatencyTimer(FT_HANDLE ftHandle, byte ucTimer);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_GetBitMode(FT_HANDLE ftHandle, ref byte pucMode);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_SetBitMode(FT_HANDLE ftHandle, byte ucMask, byte ucEnable);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_SetUSBParameters(FT_HANDLE ftHandle, UInt32 dwInTransferSize, UInt32 dwOutTransferSize);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_GetDeviceInfo(FT_HANDLE ftHandle, ref FT_DEVICE ftDevice, ref UInt32 deviceID, void* SerialNumber, void* Description, UInt32 Flag);
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_GetDriverVersion(FT_HANDLE ftHandle, ref uint lpdwVersion);
        [DllImport("FTD2XX.dll")]
        protected static extern FT_STATUS FT_SetTimeouts(FT_HANDLE ftHandle, uint ReadTimeout, uint WriteTimeout);

        protected Thread threadWriteDMXBuffer;
        public event StateChangedEventHandler StateChanged;

        public DMXProUSB()
        {
            busLength = Settings.Default.DMXChannelCount;
            if (buffer == null)
            {
                buffer = new byte[busLength]; // can be any length up to 512. The shorter the faster.
            }
            queueBuffers = new Dictionary<Guid, QueueBuffer>();
        }

        protected DmxLimits limits;
        public void setLimits(DmxLimits newLimits)
        {
            limits = newLimits;
        }
        protected object startLock = null;

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        public virtual bool start()
        {
            if (startLock == null)
            {
                startLock = new object();
            }
            try
            {
                lock (startLock)
                {
                    if (!Connected)
                    {
                        StartDMXWriteThread();
                        handle = 0;
                        if (FTDI_OpenDevice(0, ref status))
                        {
                            if (status == FT_STATUS.FT_OK)
                            {
                                // FT_Open OK, use ftHandle to access device 
                                Connected = true;
                                byte value = 0;
                                for (int channel = 0; channel < buffer.Length; channel++)
                                {
                                    if (defaults != null)
                                    {
                                        value = defaults.Values[channel];
                                    }
                                    else
                                    {
                                        value = 0;
                                    }

                                    if (limits != null)
                                    {
                                        if (value < limits.Min[channel])
                                            value = limits.Min[channel];
                                    }
                                    //initial state of channel is set here
                                    SetDmxValue(channel, value, Guid.Empty, 1);
                                }

                                return true;
                            }
                            else
                            {
                                Connected = false;
                            }
                        }
                        else
                        {
                            Connected = false;
                        }
                    }
                }
            }
            catch
            {
                stop();
                Connected = false;
            }
            return false;
        }

        public virtual void StartDMXWriteThread()
        {
            done = false;
            if (threadWriteDMXBuffer == null)
            {
                threadWriteDMXBuffer = new Thread(new ThreadStart(writeDMXBuffer));
                threadWriteDMXBuffer.IsBackground = true;
                threadWriteDMXBuffer.Start();
            }
        }

        public virtual void stop()
        {
            byte value = 0;
            for (int channel = 0; channel < buffer.Length; channel++)
            {
                if (limits != null)
                {
                    if (value < limits.Min[channel])
                        value = limits.Min[channel];
                }
                buffer[channel] = value;
            }
            newData = true;
            Thread.Sleep(500);
            FTDI_ClosePort();
            FT_Close(handle);
            Connected = false;
            done = true;
            threadWriteDMXBuffer = null;
        }
        public byte GetDmxValue(int channel)
        {
            if (buffer != null)
            {
                if (channel < buffer.Length)
                {
                    return buffer[channel];
                }
            }
            return 0;
        }
        private byte[] GetBufferForQueue(Guid queue, int priority)
        {
            QueueBuffer queueBuffer = null;
            lock (queueBuffers)
            {
                if (queueBuffers.ContainsKey(queue))
                {
                    queueBuffer = queueBuffers[queue];
                    queueBuffer.CurrentPriority = priority;
                    return queueBuffer.Buffer;
                }
            }
            return null;
        }
        public QueueBuffer CreateQueue(Guid queue, int priority)
        {
            lock (queueBuffers)
            {
                QueueBuffer queueBuffer = null;
                if (!queueBuffers.ContainsKey(queue))
                {
                    queueBuffer = new QueueBuffer(busLength);
                    queueBuffer.CurrentPriority = priority;
                    queueBuffers.Add(queue, queueBuffer);
                }
                else
                {
                    queueBuffer = queueBuffers[queue];
                }
                return queueBuffer;
            }
        }

        public void ChangeQueuePriority(Guid queue, int priority)
        {
            QueueBuffer queueBuffer = null;
            lock (queueBuffers)
            {
                if (queueBuffers.ContainsKey(queue))
                {
                    queueBuffer = queueBuffers[queue];
                    queueBuffer.CurrentPriority = priority;
                }
            }
        }
        public void DeleteQueue(Guid queue)
        {
            lock (queueBuffers)
            {
                if (queueBuffers.ContainsKey(queue))
                {
                    queueBuffers.Remove(queue);
                }
            }
        }
        public Guid[] GetCurrentQueueIds()
        {
            lock (queueBuffers)
            {
                return queueBuffers.Keys.ToArray();
            }
        }


        protected bool BuildBufferFromQueues()
        {
            byte[] oldBuffer = GetCurrentBuffer();

            //copy the buffers to an array (fixed length) for sorting & merge - so we can unlock the dictionary sooner
            QueueBuffer[] queueBuffers = CopyQueueBuffersToArray();
            byte[] newBuffer = MergeQueueBuffers(queueBuffers);

            ApplyLimitsAndDefaults(ref newBuffer);

            bool bufferChanged = CompareBuffers(oldBuffer, newBuffer);
            if (bufferChanged)
            {
                newBuffer.CopyTo(buffer, 0);

                if (StateChanged != null)
                {
                    StateChanged(null, new StateChangedEventArgs()
                    {
                        CurrentState = buffer
                    });
                }
            }
            return bufferChanged;
        }

        private void ApplyLimitsAndDefaults(ref byte[] workingBuffer)
        {
            for (int channel = 0; channel < workingBuffer.Length; channel++)
            {
                byte value = workingBuffer[channel];
                if (defaults != null)
                {
                    byte defaultValue = defaults.Values[channel];
                    if(defaultValue != 0)
                    {
                        value = defaultValue;
                    }
                }

                if (limits != null)
                {
                    if (value < limits.Min[channel])
                        value = limits.Min[channel];
                }
                workingBuffer[channel] = value;
            }
        }

        private bool CompareBuffers(byte[] oldBuffer, byte[] newBuffer)
        {
            bool bufferChanged = false;
            for (int channel = 0; channel < busLength; channel++)
            {
                if (oldBuffer[channel] != newBuffer[channel])
                {
                    bufferChanged = true;
                    newData = true;
                    break;
                }
            }
            return bufferChanged;
        }

        private byte[] MergeQueueBuffers(QueueBuffer[] buffers)
        {
            byte[] newBuffer = new byte[busLength];
            IOrderedEnumerable<QueueBuffer> orderedBuffers = buffers.OrderBy(queueBuffer => queueBuffer.CurrentPriority);
            foreach (var queueBuffer in orderedBuffers)
            {
                byte[] queueBufferBuffer = queueBuffer.Buffer;
                for (int channel = 0; channel < busLength; channel++)
                {
                    byte value = queueBufferBuffer[channel];
                    newBuffer[channel] = value;
                }
            }
            return newBuffer;
        }

        private QueueBuffer[] CopyQueueBuffersToArray()
        {
            QueueBuffer[] buffers = null;
            lock (queueBuffers)
            {
                var queueCount = queueBuffers.Count;
                buffers = new QueueBuffer[queueCount];
                int index = 0;
                foreach (var queueBuffer in queueBuffers)
                {
                    buffers[index++] = queueBuffer.Value;
                }
            }
            return buffers;
        }

        public void SetDmxValue(int channel, byte value, Guid queue, int priority)
        {
            //using the queue id, get the buffer
            byte[] queueBuffer = GetBufferForQueue(queue, priority);
            if (queueBuffer != null) //if queue is defined
            {
                if (channel < queueBuffer.Length)
                {
                    if (limits != null)
                    {
                        if (value < limits.Min[channel])
                            value = limits.Min[channel];
                        if (value > limits.Max[channel])
                            value = limits.Max[channel];
                    }
                    else
                    {
                        if (value < 0)
                            value = 0;
                        if (value > 255)
                            value = 255;
                    }
                    if (queueBuffer[channel] != value)
                    {
                        queueBuffer[channel] = value;
                        newData = true;
                    }
                }
            }
        }

        public byte[] GetCurrentBuffer()
        {
            return (byte[])buffer.Clone();
        }

        protected bool newData = false;
        protected virtual void writeDMXBuffer()
        {
            while (!done)
            {
                newData = BuildBufferFromQueues();
                try
                {
                    if (Connected)
                    {
                        if (newData)
                        {
                            FT_STATUS status = FT_STATUS.FT_OK;
                            if (!FTDI_SendData(handle, SET_DMX_TX_MODE, (new byte[1]).Concat(buffer).ToArray(), ref status))
                            {
                                break;
                            }
                            newData = false;
                        }

                        System.Threading.Thread.Sleep(25);
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(25);
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }
            Connected = false;
            done = false;
        }

        protected int write(uint handle, byte[] data, int length)
        {
            IntPtr ptr = Marshal.AllocHGlobal((int)length);
            Marshal.Copy(data, 0, ptr, (int)length);
            uint bytesWritten = 0;
            status = FT_Write(handle, ptr, (uint)length, ref bytesWritten);
            Marshal.FreeHGlobal(ptr);
            if (status != FT_STATUS.FT_OK)
            {
                //write failed
                done = true;
                Connected = false;
                return 0;
            }

            return (int)bytesWritten;
        }

        private FT_STATUS read(uint handle, byte[] data, int length, ref int bytesRead)
        {
            uint uintBytesRead = 0;
            IntPtr ptr = Marshal.AllocHGlobal((int)length);
            status = FT_Read(handle, ptr, (uint)length, ref uintBytesRead);
            if (status != FT_STATUS.FT_OK)
            {
                //read failed
                return status;
            }
            Marshal.Copy(ptr, data, 0, (int)length);
            Marshal.FreeHGlobal(ptr);
            bytesRead = (int)uintBytesRead;
            return FT_STATUS.FT_OK;
        }

        protected struct DMXUSBPROParamsType
        {
            public byte UserSizeLSB;
            public byte UserSizeMSB;
            public byte BreakTime;
            public byte MaBTime;
            public byte RefreshRate;
        }

        protected virtual bool FTDI_OpenDevice(int device_num, ref FT_STATUS status)
        {
            //return false;

            uint RTimeout = 120;
            uint WTimeout = 100;
            int VersionMSB = 0;
            int VersionLSB = 0;
            uint[] temp = new uint[4];
            uint version = 0;
            uint major_ver, minor_ver, build_ver;
            int recvd = 0;
            byte[] singleBuff = new byte[1];
            int size = 0;
            bool res = false;
            int tries = 0;
            byte latencyTimer = (byte)0;
            int BreakTime;
            int MABTime;
            DMXUSBPROParamsType PRO_Params;
            // Try at least 3 times 
            StartCounter++;
            //Console.WriteLine("\n------ D2XX ------- Opening [Device {0}] ------ Try {1}", device_num, StartCounter);
            // Open the PRO 
            try
            {
                status = FT_Open((uint)device_num, ref handle);
                if (status != FT_STATUS.FT_OK)
                {
                    return false;
                }
                else
                {
                    Connected = true;
                    // GET D2XX Driver Version
                    status = FT_GetDriverVersion(handle, ref version);
                    if (status == FT_STATUS.FT_OK)
                    {
                        major_ver = (uint)version >> 16;
                        minor_ver = (uint)version >> 8;
                        build_ver = (uint)version & 0xFF;
                        Console.WriteLine("D2XX Driver Version:: {0:x2}.{1:x2}.{2:x2}", major_ver, minor_ver, build_ver);
                    }
                    else
                        Console.WriteLine("Unable to Get D2XX Driver Version");

                    //// GET Latency Timer
                    status = FT_GetLatencyTimer(handle, ref latencyTimer);
                    if (status == FT_STATUS.FT_OK)
                        Console.WriteLine("Latency Timer:: {0} ", latencyTimer);
                    else
                        Console.WriteLine("Unable to Get Latency Timer");

                    //// SET Default Read & Write Timeouts (in micro sec ~ 100)
                    FT_SetTimeouts(handle, RTimeout, WTimeout);
                    //// Piurges the buffer
                    FT_Purge(handle, PURGE_RX);
                    //// Send Get Widget Parameters to get Device Info
                    Console.WriteLine("Sending GET_WIDGET_PARAMS packet... ");
                    FT_Purge(handle, PURGE_TX);

                    res = FTDI_SendData(handle, GET_WIDGET_PARAMS, new byte[2], ref status);
                    //// Check Response
                    if (res != NO_RESPONSE)
                    {
                        Console.WriteLine("PRO Connected Succesfully");


                        //// Receive Widget Response
                        Console.WriteLine("Waiting for GET_WIDGET_PARAMS_REPLY packet... ");
                        PRO_Params = new DMXUSBPROParamsType();

                        byte[] paramsBuff = new byte[Marshal.SizeOf(PRO_Params)];
                        res = FTDI_ReceiveData(handle, GET_WIDGET_PARAMS_REPLY, paramsBuff, paramsBuff.Length);
                        PRO_Params.UserSizeLSB = paramsBuff[0];
                        PRO_Params.UserSizeMSB = paramsBuff[1];
                        PRO_Params.BreakTime = paramsBuff[2];
                        PRO_Params.MaBTime = paramsBuff[3];
                        PRO_Params.RefreshRate = paramsBuff[4];
                        // Check Response
                        if (res == NO_RESPONSE)
                        {
                            //Receive Widget Response packet
                            res = FTDI_ReceiveData(handle, GET_WIDGET_PARAMS_REPLY, paramsBuff, paramsBuff.Length);
                            PRO_Params.UserSizeLSB = paramsBuff[0];
                            PRO_Params.UserSizeMSB = paramsBuff[1];
                            PRO_Params.BreakTime = paramsBuff[2];
                            PRO_Params.MaBTime = paramsBuff[3];
                            PRO_Params.RefreshRate = paramsBuff[4];
                            if (res == NO_RESPONSE)
                            {
                                FTDI_ClosePort();
                                return NO_RESPONSE;
                            }
                        }
                        else
                            Console.WriteLine("GET WIDGET REPLY Received ... ");

                        //// Firmware  Version
                        VersionMSB = PRO_Params.UserSizeMSB;
                        VersionLSB = PRO_Params.UserSizeLSB;
                        //// GET PRO's serial number 
                        res = FTDI_SendData(handle, GET_WIDGET_SN, new byte[2], ref status);
                        byte[] serialBuff = new byte[4];
                        res = FTDI_ReceiveData(handle, GET_WIDGET_SN, serialBuff, 4);
                        //// Display All PRO Parametrs & Info avialable
                        Console.WriteLine("-----------::PRO Connected [Information Follows]::------------");
                        Console.WriteLine("FIRMWARE VERSION: {0}.{1}", VersionMSB, VersionLSB);
                        BreakTime = (int)(PRO_Params.BreakTime * 10.67) + 100;
                        Console.WriteLine("BREAK TIME: {0} micro sec ", BreakTime);
                        MABTime = (int)(PRO_Params.MaBTime * 10.67);
                        Console.WriteLine("MAB TIME: {0} micro sec", MABTime);
                        Console.WriteLine("SEND REFRESH RATE: {0} packets/sec", PRO_Params.RefreshRate);

                        //// return success
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private DevData[] DevInformation;
        private class DevData
        {
            public string DevDescription;
            public FT_DEVICE DevInfo = FT_DEVICE.FT_DEVICE_UNKNOWN;
            public string DevSerial;
            public uint DevID;
        }

        /* Function : FTDI_ClosePort
         * Author	: ENTTEC
         * Purpose  : Closes the Open DMX USB PRO Device Handle
         * Parameters: none
         **/
        void FTDI_ClosePort()
        {
            if (handle != null)
                FT_Close(handle);
        }

        private Int16 SearchDevice()
        {
            //cmbDevList.SelectedIndex = iCurrentIndex;
            FT_HANDLE ftHandle = new FT_HANDLE();
            FT_STATUS ftStatus;
            Int16 numDevs;

            string MyText = "";
            unsafe
            {
                ftStatus = FT_ListDevices(&numDevs, null, FT_LIST_NUMBER_ONLY);

                if (ftStatus == FT_STATUS.FT_OK)
                {
                    MyText += "Number of devices is: " + numDevs.ToString() + "\n";
                }
                else
                {
                    MyText += "Error";
                }

                DevInformation = new DevData[numDevs];
                //pN->NumDeviceInLine=numDevs;

                for (int i = 0; i < numDevs; i++)
                {

                    ftStatus = FT_Open((uint)i, ref ftHandle);
                    if (ftStatus != FT_STATUS.FT_OK)
                    {
                        MyText += "Open failed\n";
                        continue;
                    }
                    byte Tim = 1;
                    ftStatus = FT_SetLatencyTimer(ftHandle,
                        Tim);

                    Tim = 0;
                    ftStatus = FT_GetLatencyTimer(ftHandle,
                        ref Tim);

                    MyText += "Dev " + i.ToString() + " - ";

                    if (ftStatus == FT_STATUS.FT_OK)
                    {
                        MyText += "\tTim=";
                        MyText += Tim;
                    }
                    else
                    {
                        MyText += "FAILED\n";
                    }
                    string devSerial = "";
                    string devDescription = "";
                    byte[] SerialNumber = new byte[16];
                    byte[] sDescription = new byte[64];
                    uint devID = 0;
                    FT_DEVICE devType = FT_DEVICE.FT_DEVICE_UNKNOWN;
                    fixed (byte* pS = &SerialNumber[0])
                    fixed (byte* pD = &sDescription[0])
                    {
                        ftStatus = FT_GetDeviceInfo(
                            ftHandle
                            , ref devType
                            , ref devID
                            , pS
                            , pD
                            , 0);
                    }

                    for (int j = 0; j < SerialNumber.Length; j++)
                    {
                        if ((char)SerialNumber[j] == '\0')
                            break;
                        devSerial += (char)SerialNumber[j];
                    }

                    for (int j = 0; j < sDescription.Length; j++)
                    {
                        if ((char)sDescription[j] == '\0')
                            break;
                        devDescription += (char)sDescription[j];
                    }

                    DevData CurDev = new DevData()
                    {
                        DevDescription = devDescription,
                        DevID = devID,
                        DevInfo = devType,
                        DevSerial = devSerial
                    };
                    DevInformation[i] = CurDev;

                    FT_Close(ftHandle);

                }
            }
            return numDevs;
        }

        /* Function : FTDI_SendData
         * Author	: ENTTEC
         * Purpose  : Send Data (DMX or other packets) to the PRO
         * Parameters: Label, Pointer to Data Structure, Length of Data
         * Modified by Gareth Evans, Sniper Systems to convert this to c# for use
        */
        protected virtual bool FTDI_SendData(FT_HANDLE handle, int label, byte[] data, ref FT_STATUS status)
        {
            if (Connected)
            {
                int bytes_written = 0;
                int size = 0;
                // Form Packet Header
                byte[] header = GetProHeader(label, data.Length);
                byte[] footer = new byte[1] { DMX_END_CODE };
                byte[] packet = header.Concat(data).Concat(footer).ToArray();
                //Console.WriteLine("Write: {0}", Sniper.Common.Conversion.ToHex(true, packet));
                bytes_written = write(handle, packet, packet.Length);
                if (bytes_written != packet.Length)
                {
                    return false;
                }

                if (status == FT_STATUS.FT_OK)
                    return true;
                else
                    return false;
            }
            return false;
        }

        protected byte[] GetProHeader(int label, int length)
        {
            byte[] header = new byte[DMX_HEADER_LENGTH];
            header[0] = DMX_START_CODE;
            header[1] = (byte)label;
            header[2] = (byte)(length & OFFSET);
            header[3] = (byte)(length >> BYTE_LENGTH);
            return header;
        }
        public void Dispose()
        {
            //if (Connected)
            //{
            done = true;

            //}
            Thread.Sleep(400);
            if (threadWriteDMXBuffer != null)
            {
                threadWriteDMXBuffer.Abort();
                Thread.Sleep(400);
            }

        }
        /* Function : FTDI_ReceiveData
         * Author	: ENTTEC
         * Purpose  : Receive Data (DMX or other packets) from the PRO
         * Parameters: Label, Pointer to Data Structure, Length of Data
         * Modified by Gareth Evans, Sniper Systems to convert this to c# for use
         **/
        bool FTDI_ReceiveData(FT_HANDLE handle, int label, byte[] data, int expected_length)
        {
            if (Connected)
            {
                FT_STATUS status = 0;
                uint length = 0;
                //int bytes_to_read = 1;
                int bytes_read = 0;
                //unsigned char byte = 0;
                byte[] buffer = new byte[600];
                byte[] singleBuff = new byte[1];
                singleBuff[0] = (byte)0;
                // Check for Start Code and matching Label
                while (singleBuff[0] != label)
                {
                    while (singleBuff[0] != DMX_START_CODE)
                    {
                        status = read(handle, singleBuff, ONE_BYTE, ref bytes_read);
                        if (bytes_read == 0)
                            return NO_RESPONSE;
                    }
                    status = read(handle, singleBuff, ONE_BYTE, ref bytes_read);
                    if (bytes_read == 0)
                        return NO_RESPONSE;
                }
                // Read the rest of the Header Byte by Byte -- Get Length
                status = read(handle, singleBuff, ONE_BYTE, ref bytes_read);
                if (bytes_read == 0)
                    return NO_RESPONSE;
                length = singleBuff[0];
                status = read(handle, singleBuff, ONE_BYTE, ref bytes_read);
                if (status != FT_STATUS.FT_OK)
                    return NO_RESPONSE;
                length += ((uint)singleBuff[0]) << BYTE_LENGTH;
                // Check Length is not greater than allowed
                if (length > DMX_PACKET_SIZE)
                    return NO_RESPONSE;
                // Read the actual Response Data
                status = read(handle, buffer, (int)length, ref bytes_read);
                if (bytes_read != length)
                    return NO_RESPONSE;
                // Check The End Code
                status = read(handle, singleBuff, ONE_BYTE, ref bytes_read);
                if (bytes_read == 0)
                    return NO_RESPONSE;
                if (singleBuff[0] != DMX_END_CODE)
                    return NO_RESPONSE;
                // Copy The Data read to the buffer passed
                Array.ConstrainedCopy(buffer, 0, data, 0, data.Length);

                //Console.WriteLine("Read: {0}", Sniper.Common.Conversion.ToHex(true, data));

                return true;
            }
            return false;
        }

        protected enum FT_STATUS
        {
            FT_OK = 0,
            FT_INVALID_HANDLE,
            FT_DEVICE_NOT_FOUND,
            FT_DEVICE_NOT_OPENED,
            FT_IO_ERROR,
            FT_INSUFFICIENT_RESOURCES,
            FT_INVALID_PARAMETER,
            FT_INVALID_BAUD_RATE,
            FT_DEVICE_NOT_OPENED_FOR_ERASE,
            FT_DEVICE_NOT_OPENED_FOR_WRITE,
            FT_FAILED_TO_WRITE_DEVICE,
            FT_EEPROM_READ_FAILED,
            FT_EEPROM_WRITE_FAILED,
            FT_EEPROM_ERASE_FAILED,
            FT_EEPROM_NOT_PRESENT,
            FT_EEPROM_NOT_PROGRAMMED,
            FT_INVALID_ARGS,
            FT_OTHER_ERROR
        };

        private enum FT_DEVICE
        {
            FT_DEVICE_BM,
            FT_DEVICE_AM,
            FT_DEVICE_100AX,
            FT_DEVICE_UNKNOWN,
            FT_DEVICE_2232C
        };


    }






}
