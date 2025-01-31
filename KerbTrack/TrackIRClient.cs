﻿using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TrackIRUnity
{
    public class TrackIRClient
    {
        private TrackIRClient.dNP_GetSignatureDelegate NP_GetSignatureDelegate;
        private TrackIRClient.dNP_RegisterWindowHandle NP_RegisterWindowHandle;
        private TrackIRClient.dNP_UnregisterWindowHandle NP_UnregisterWindowHandle;
        private TrackIRClient.dNP_RegisterProgramProfileID NP_RegisterProgramProfileID;
        private TrackIRClient.dNP_QueryVersion NP_QueryVersion;
        private TrackIRClient.dNP_RequestData NP_RequestData;
        private TrackIRClient.dNP_GetData NP_GetData;
        //private TrackIRClient.dNP_UnregisterNotify NP_UnregisterNotify;
        private TrackIRClient.dNP_StartCursor NP_StartCursor;
        private TrackIRClient.dNP_StopCursor NP_StopCursor;
        private TrackIRClient.dNP_ReCenter NP_ReCenter;
        private TrackIRClient.dNP_StartDataTransmission NP_StartDataTransmission;
        private TrackIRClient.dNP_StopDataTransmission NP_StopDataTransmission;
        private ulong NPFrameSignature;
        private ulong NPStaleFrames;

        [DllImport("user32.dll")]
        private static extern int GetForegroundWindow();

        [DllImport("kernel32.dll")]
        private static extern int LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(int hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(int hModule);

        public string TrackIR_Enhanced_Init()
        {
            this.NPFrameSignature = 0UL;
            this.NPStaleFrames = 0UL;
            string dllPath = "";
            string result = "";
            this.GetDLLLocation(ref dllPath);
            int NPResult = (int)this.NPClient_Init(dllPath);
            if (this.NPClient_Init(dllPath) == TrackIRClient.NPRESULT.NP_OK)
            {
                result += "NPClient interface -- initialize OK\r\n";
                int foregroundWindow = TrackIRClient.GetForegroundWindow();
                result += "ForegroundWindow handle: " + foregroundWindow.ToString() + "\r\n";
                if (this.NP_RegisterWindowHandle(foregroundWindow) == TrackIRClient.NPRESULT.NP_OK)
                {
                    result += "NPClient : Window handle registration successful.\r\n";
                    ushort pwVersion = (ushort)0;
                    if (this.NP_QueryVersion(ref pwVersion) == TrackIRClient.NPRESULT.NP_OK)
                    {
                        result += string.Format("NaturalPoint software version is " + string.Format("{0:d}",
                            (object)((int)pwVersion >> 8)) + "." + ((object)string.Format("{0:d}",
                            (object)((int)pwVersion & (int)byte.MaxValue))).ToString() + "\r\n");
                        int num2 = (int)this.NP_RequestData((ushort)((uint)(0 | 2) | 4U | 1U | 16U | 32U | 64U));
                        int num3 = (int)this.NP_RegisterProgramProfileID((ushort)20430);
                        if (this.NP_StopCursor() == TrackIRClient.NPRESULT.NP_OK)
                        {
                            result += "Cursor stopped\r\n";
                            if (this.NP_StartDataTransmission() == TrackIRClient.NPRESULT.NP_OK)
                                return result + "Data Transmission started\r\n";
                            result += "NPCLient : Error starting data transmission\r\n";
                            return result;
                        }
                        else
                        {
                            result += "NPClient : Error stopping cursor\r\n";
                            return result;
                        }
                    }
                    else
                    {
                        result += "NPClient : Error querying NaturalPoint software version!!\r\n";
                        return result;
                    }
                }
                else
                {
                    result += "NPClient : Error registering window handle!!\r\n";
                    return result;
                }
            }
            else
            {
                result = result + "Error initializing NPClient interface!!\r\n";
                return result;
            }
        }

        public TrackIRClient.LPTRACKIRDATA client_HandleTrackIRData()
        {
            TrackIRClient.LPTRACKIRDATA pTID = new TrackIRClient.LPTRACKIRDATA();
            if (this.NP_GetData(ref pTID) != TrackIRClient.NPRESULT.NP_OK || (int)pTID.wNPStatus != (int)NPSTATUS.NPSTATUS_REMOTEACTIVE)
                return pTID;
            if ((long)this.NPFrameSignature != (long)pTID.wPFrameSignature)
            {
                this.NPFrameSignature = (ulong)pTID.wPFrameSignature;
                this.NPStaleFrames = 0UL;
                return pTID;
            }
            else
            {
                if (this.NPStaleFrames > 30UL)
                    return pTID;
                ++this.NPStaleFrames;
                return pTID;
            }
        }

        public string client_TestTrackIRData()
        {
            TrackIRClient.LPTRACKIRDATA pTID = new TrackIRClient.LPTRACKIRDATA();
            string result = "";
            if (this.NP_GetData(ref pTID) == TrackIRClient.NPRESULT.NP_OK)
            {
                if ((int)pTID.wNPStatus == (int)NPSTATUS.NPSTATUS_REMOTEACTIVE)
                {
                    if ((long)this.NPFrameSignature != (long)pTID.wPFrameSignature)
                    {
                        result = result + "Pitch: " + pTID.fNPPitch + "\r\n" +
                          "Roll: " + pTID.fNPRoll + "\r\n" +
                          "Yaw: " + pTID.fNPYaw + "\r\n" +
                          "PosX: " + pTID.fNPX + "\r\n" +
                          "PosY: " + pTID.fNPY + "\r\n" +
                          "PosZ: " + pTID.fNPX + "\r\n";
                        this.NPFrameSignature = (ulong)pTID.wPFrameSignature;
                        this.NPStaleFrames = 0UL;
                    }
                    else if (this.NPStaleFrames > 30UL)
                    {
                        result += "No New Data. Paused or Not Tracking?\r\n" +
                            "Information NPStatus = " + pTID.wNPStatus + "\r\n";
                    }
                    else
                    {
                        ++this.NPStaleFrames;
                        result += "No New Data for " + this.NPStaleFrames + " frames\r\n" +
                            "Information NPStatus = " + pTID.wNPStatus + "\r\n";
                    }
                }
            }
            else
                result = result + "User Disabled";
            return result;
        }

        public string TrackIR_Shutdown()
        {
            string result = "";
            result += this.NP_StopDataTransmission() != TrackIRClient.NPRESULT.NP_OK ? "StopDataTransmission() ERROR!!\r\n" : "StopDataTransmission() OK\r\n";
            result += this.NP_StartCursor() != TrackIRClient.NPRESULT.NP_OK ? "StartCursor() ERROR!!\r\n" : "StartCursor() OK\r\n";
            result += this.NP_UnregisterWindowHandle() != TrackIRClient.NPRESULT.NP_OK ? "UnregisterWindowHandle() ERROR!!\r\n" : "UnregisterWindowHandle() OK\r\n";
            return result;
        }

        public TrackIRClient.NPRESULT NPClient_Init(string dllPath)
        {
            //LET THE SORCERY COMMENCE
            if (IntPtr.Size == 4) //32 bit
            {
                dllPath = dllPath + "NPClient.dll";
            }
            else if (IntPtr.Size == 8) //64 bit
            {
                dllPath = dllPath + "NPClient64.dll";
            }
            if (!File.Exists(dllPath))
                return TrackIRClient.NPRESULT.NP_ERR_DLL_NOT_FOUND;
            int hModule = TrackIRClient.LoadLibrary(dllPath);
            if (hModule == 0)
                return TrackIRClient.NPRESULT.NP_ERR_DLL_NOT_FOUND;
            this.NP_GetSignatureDelegate = (TrackIRClient.dNP_GetSignatureDelegate)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_GetSignature"), typeof(TrackIRClient.dNP_GetSignatureDelegate));
            TrackIRClient.LPTRACKIRSIGNATUREDATA signature = new TrackIRClient.LPTRACKIRSIGNATUREDATA();
            TrackIRClient.LPTRACKIRSIGNATUREDATA lptrackirsignaturedata = new TrackIRClient.LPTRACKIRSIGNATUREDATA();
            lptrackirsignaturedata.DllSignature = "precise head tracking\n put your head into the game\n now go look around\n\n Copyright EyeControl Technologies";
            lptrackirsignaturedata.AppSignature = "hardware camera\n software processing data\n track user movement\n\n Copyright EyeControl Technologies";
            TrackIRClient.NPRESULT npresult;
            if (this.NP_GetSignatureDelegate(ref signature) == TrackIRClient.NPRESULT.NP_OK)
            {
                if (string.Compare(lptrackirsignaturedata.DllSignature, signature.DllSignature) == 0 && string.Compare(lptrackirsignaturedata.AppSignature, signature.AppSignature) == 0)
                {
                    npresult = TrackIRClient.NPRESULT.NP_OK;
                    this.NP_RegisterWindowHandle = (TrackIRClient.dNP_RegisterWindowHandle)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_RegisterWindowHandle"), typeof(TrackIRClient.dNP_RegisterWindowHandle));
                    this.NP_UnregisterWindowHandle = (TrackIRClient.dNP_UnregisterWindowHandle)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_UnregisterWindowHandle"), typeof(TrackIRClient.dNP_UnregisterWindowHandle));
                    this.NP_RegisterProgramProfileID = (TrackIRClient.dNP_RegisterProgramProfileID)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_RegisterProgramProfileID"), typeof(TrackIRClient.dNP_RegisterProgramProfileID));
                    this.NP_QueryVersion = (TrackIRClient.dNP_QueryVersion)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_QueryVersion"), typeof(TrackIRClient.dNP_QueryVersion));
                    this.NP_RequestData = (TrackIRClient.dNP_RequestData)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_RequestData"), typeof(TrackIRClient.dNP_RequestData));
                    this.NP_GetData = (TrackIRClient.dNP_GetData)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_GetData"), typeof(TrackIRClient.dNP_GetData));
                    this.NP_StartCursor = (TrackIRClient.dNP_StartCursor)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_StartCursor"), typeof(TrackIRClient.dNP_StartCursor));
                    this.NP_StopCursor = (TrackIRClient.dNP_StopCursor)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_StopCursor"), typeof(TrackIRClient.dNP_StopCursor));
                    this.NP_ReCenter = (TrackIRClient.dNP_ReCenter)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_ReCenter"), typeof(TrackIRClient.dNP_ReCenter));
                    this.NP_StartDataTransmission = (TrackIRClient.dNP_StartDataTransmission)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_StartDataTransmission"), typeof(TrackIRClient.dNP_StartDataTransmission));
                    this.NP_StopDataTransmission = (TrackIRClient.dNP_StopDataTransmission)Marshal.GetDelegateForFunctionPointer(TrackIRClient.GetProcAddress(hModule, "NP_StopDataTransmission"), typeof(TrackIRClient.dNP_StopDataTransmission));
                }
                else
                    npresult = TrackIRClient.NPRESULT.NP_ERR_DLL_NOT_FOUND;
            }
            else
                npresult = TrackIRClient.NPRESULT.NP_ERR_DLL_NOT_FOUND;
            return npresult;
        }

        public void GetDLLLocation(ref string dllPath)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\NaturalPoint\\NATURALPOINT\\NPClient Location", false);
            dllPath = registryKey.GetValue("Path").ToString();
            registryKey.Close();
        }

        private delegate TrackIRClient.NPRESULT PF_NOTIFYCALLBACK(ushort a, ushort b);

        private delegate TrackIRClient.NPRESULT dNP_GetSignatureDelegate(ref TrackIRClient.LPTRACKIRSIGNATUREDATA signature);

        private delegate TrackIRClient.NPRESULT dNP_RegisterWindowHandle(int hWnd);

        private delegate TrackIRClient.NPRESULT dNP_RegisterProgramProfileID(ushort wPPID);

        private delegate TrackIRClient.NPRESULT dNP_UnregisterWindowHandle();

        private delegate TrackIRClient.NPRESULT dNP_QueryVersion(ref ushort pwVersion);

        private delegate TrackIRClient.NPRESULT dNP_RequestData(ushort wDataReq);

        private delegate TrackIRClient.NPRESULT dNP_GetData(ref TrackIRClient.LPTRACKIRDATA pTID);

        private delegate TrackIRClient.NPRESULT dNP_RegisterNotify(TrackIRClient.PF_NOTIFYCALLBACK pfNotify);

        private delegate TrackIRClient.NPRESULT dNP_UnregisterNotify();

        private delegate TrackIRClient.NPRESULT dNP_StartCursor();

        private delegate TrackIRClient.NPRESULT dNP_StopCursor();

        private delegate TrackIRClient.NPRESULT dNP_ReCenter();

        private delegate TrackIRClient.NPRESULT dNP_StartDataTransmission();

        private delegate TrackIRClient.NPRESULT dNP_StopDataTransmission();

        public struct LPTRACKIRSIGNATUREDATA
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
            public string DllSignature;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
            public string AppSignature;
        }

        public enum NPSTATUS
        {
            NPSTATUS_REMOTEACTIVE = 0,
            NPSTATUS_REMOTEDISABLED = 1
        }

        public enum NPRESULT
        {
            NP_OK,
            NP_ERR_DEVICE_NOT_PRESENT,
            NP_ERR_UNSUPPORTED_OS,
            NP_ERR_INVALID_ARG,
            NP_ERR_DLL_NOT_FOUND,
            NP_ERR_NO_DATA,
            NP_ERR_INTERNAL_DATA,
        }

        public struct LPTRACKIRDATA
        {
            public ushort wNPStatus;
            public ushort wPFrameSignature;
            public uint dwNPIOData;
            public float fNPRoll;
            public float fNPPitch;
            public float fNPYaw;
            public float fNPX;
            public float fNPY;
            public float fNPZ;
            public float fNPRawX;
            public float fNPRawY;
            public float fNPRawZ;
            public float fNPDeltaX;
            public float fNPDeltaY;
            public float fNPDeltaZ;
            public float fNPSmoothX;
            public float fNPSmoothY;
            public float fNPSmoothZ;
        }
    }
}
