using Sniper.Lighting.DMX.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FT_HANDLE = System.UInt32;

namespace Sniper.Lighting.DMX
{
    public class OpenDMXUSB : DMXProUSB
    {
      
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        public override bool start()
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
                        if (buffer == null)
                        {
                            buffer = new byte[busLength]; // can be any length up to 512. The shorter the faster.
                        }
                        handle = 0;
                        StartDMXWriteThread();
                        if (FTDI_OpenDevice(0, ref status))
                        {
                            if (status == FT_STATUS.FT_OK)
                            {
                                // FT_Open OK, use ftHandle to access device 
                                Connected = true;
                                byte value = 0;
                                for (int channel = 0; channel < buffer.Length; channel++)
                                {
                                    value = 0;
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
                                        if (value < limits.Min[channel]) value = limits.Min[channel];
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

        protected override bool FTDI_OpenDevice(int device_num, ref FT_STATUS status)
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
            Console.WriteLine("\n------ D2XX ------- Opening [Device {0}] ------ Try {1}", device_num, StartCounter);
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
                    status = FT_ResetDevice(handle);
                    status = FT_SetDivisor(handle, (char)12);  // set baud rate
                    status = FT_SetDataCharacteristics(handle, BITS_8, STOP_BITS_2, PARITY_NONE);
                    status = FT_SetFlowControl(handle, (char)FLOW_NONE, 0, 0);
                    status = FT_ClrRts(handle);
                    status = FT_Purge(handle, PURGE_TX);
                    status = FT_Purge(handle, PURGE_RX);

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


                    //    //// Receive Widget Response
                    //    Console.WriteLine("Waiting for GET_WIDGET_PARAMS_REPLY packet... ");
                    //    PRO_Params = new DMXUSBPROParamsType();

                    //    byte[] paramsBuff = new byte[Marshal.SizeOf(PRO_Params)];
                    //    res = FTDI_ReceiveData(handle, GET_WIDGET_PARAMS_REPLY, paramsBuff, paramsBuff.Length);
                    //    PRO_Params.UserSizeLSB = paramsBuff[0];
                    //    PRO_Params.UserSizeMSB = paramsBuff[1];
                    //    PRO_Params.BreakTime = paramsBuff[2];
                    //    PRO_Params.MaBTime = paramsBuff[3];
                    //    PRO_Params.RefreshRate = paramsBuff[4];
                    //    //// Check Response
                    //    if (res == NO_RESPONSE)
                    //    {
                    //        //Receive Widget Response packet
                    //        res = FTDI_ReceiveData(handle, GET_WIDGET_PARAMS_REPLY, paramsBuff, paramsBuff.Length);
                    //        PRO_Params.UserSizeLSB = paramsBuff[0];
                    //        PRO_Params.UserSizeMSB = paramsBuff[1];
                    //        PRO_Params.BreakTime = paramsBuff[2];
                    //        PRO_Params.MaBTime = paramsBuff[3];
                    //        PRO_Params.RefreshRate = paramsBuff[4];
                    //        if (res == NO_RESPONSE)
                    //        {
                    //            FTDI_ClosePort();
                    //            return NO_RESPONSE;
                    //        }
                    //    }
                    //    else
                    //        Console.WriteLine("GET WIDGET REPLY Received ... ");

                    //    //// Firmware  Version
                    //    VersionMSB = PRO_Params.UserSizeMSB;
                    //    VersionLSB = PRO_Params.UserSizeLSB;
                    //    //// GET PRO's serial number 
                    //    res = FTDI_SendData(handle, GET_WIDGET_SN, new byte[2], ref status);
                    //    byte[] serialBuff = new byte[4];
                    //    res = FTDI_ReceiveData(handle, GET_WIDGET_SN, serialBuff, 4);
                    //    //// Display All PRO Parametrs & Info avialable
                    //    Console.WriteLine("-----------::PRO Connected [Information Follows]::------------");
                    //    Console.WriteLine("FIRMWARE VERSION: {0}.{1}", VersionMSB, VersionLSB);
                    //    BreakTime = (int)(PRO_Params.BreakTime * 10.67) + 100;
                    //    Console.WriteLine("BREAK TIME: {0} micro sec ", BreakTime);
                    //    MABTime = (int)(PRO_Params.MaBTime * 10.67);
                    //    Console.WriteLine("MAB TIME: {0} micro sec", MABTime);
                    //    Console.WriteLine("SEND REFRESH RATE: {0} packets/sec", PRO_Params.RefreshRate);

                    //    //// return success
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

        protected override void writeDMXBuffer()
        {
            while (!done)
            {
                try
                {
                    newData = BuildBufferFromQueues();
                    if (Connected)
                    {
                        if (newData)
                        {
                            FT_SetBreakOn(handle);
                            FT_SetBreakOff(handle);
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
                        System.Threading.Thread.Sleep(1000);
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

        protected override bool FTDI_SendData(FT_HANDLE handle, int label, byte[] data, ref FT_STATUS status)
        {
            if (Connected)
            {
                int bytes_written = 0;
                // Form Packet Header
                byte[] header = GetProHeader(label, data.Length);
                byte[] footer = new byte[1] { DMX_END_CODE };
                byte[] packet = header.Concat(data).Concat(footer).ToArray();
                bytes_written = write(handle, data, data.Length);
                if (bytes_written != data.Length)
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

        public override void stop()
        {
            byte value = 0;
            for (int channel = 0; channel < buffer.Length; channel++)
            {
                if (limits != null)
                {
                    if (value < limits.Min[channel]) value = limits.Min[channel];
                }
                buffer[channel] = value;
            }
            newData = true;
            Thread.Sleep(200);
            done = true;
            FT_Close(handle);
            handle = 0;
        }
    }


}
