/****************************************************************************

  Copyright (c) 2021 Ronald Sieber

  Project:      Esp32Config - UwpApp
  Description:  Implemention of BLE Device Profile

  -------------------------------------------------------------------------

    CONVENTION:                                                                         
                                                                                        
    In a GUID of a Characteristic the 1st group specifies the Characteristic purpose,   
    while the 2nd group is always '0000'. A Descriptor uses in its 1st group the same   
    value as it's assoziated Characteristic, but with a value unequal '0000' in the     
    2nd group. For the first Descriptor the value '0001' is used. This scheme allows    
    multiple Descriptors for one Characteristic in the future by continue numbering     
    with '0002' etc. However, this implementation always uses only the first descriptor 
    ('001').                                                                            
                                                                                        
    Sample:                                                                             
    GUID Characteristic:         "00003100-0000-1000-8000-E776CC14FE69"                 
    GUID Offset Descriptor:      "00000000-0001-0000-0000-000000000000"                 
                                 --------------------------------------                 
    Resulting Descriptor GUID:   "00003100-0001-1000-8000-E776CC14FE69"                 
                                                                                        
  -------------------------------------------------------------------------

  Revision History:

  2021/04/02 -rs:   V1.00 Initial version

****************************************************************************/


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;
using Windows.UI.Core;
using System.Data;
using System.Collections.ObjectModel;
using Windows.Devices.Bluetooth;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using System.Text;
using Windows.Storage.Streams;
using System.Diagnostics;                               // Debug.Writeln



namespace Esp32ConfigUwp
{

    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    //                                                                     //
    //          C L A S S   Esp32BleAppCfgData                             //
    //                                                                     //
    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    public  class  Esp32BleAppCfgData
    {

        // Members assoziated to Service 'BleGattServiceDevMnt'
        public  string  m_strDevName            = null;
        public  UInt32  m_ui32DevType           = 0;
        public  UInt32  m_ui32SysTickCnt        = 0;

        // Members assoziated to Service 'BleGattServiceWifi'
        public  string  m_strSSID               = null;
        public  string  m_strPasswd             = null;
        public  string  m_strOwnAddr            = null;
        public  UInt16  m_ui16OwnMode           = 0;
        public  UInt16  m_ui16OwnModeFeatList   = 0;

        // Members assoziated to Service 'BleGattServiceAppRt'
        public  UInt16  m_ui16Opt1              = 0;
        public  UInt16  m_ui16Opt2              = 0;
        public  UInt16  m_ui16Opt3              = 0;
        public  UInt16  m_ui16Opt4              = 0;
        public  UInt16  m_ui16Opt5              = 0;
        public  UInt16  m_ui16Opt6              = 0;
        public  UInt16  m_ui16Opt7              = 0;
        public  UInt16  m_ui16Opt8              = 0;
        public  string  m_strPeerAddr           = null;

        public  string  m_strLabelOpt1          = null;
        public  string  m_strLabelOpt2          = null;
        public  string  m_strLabelOpt3          = null;
        public  string  m_strLabelOpt4          = null;
        public  string  m_strLabelOpt5          = null;
        public  string  m_strLabelOpt6          = null;
        public  string  m_strLabelOpt7          = null;
        public  string  m_strLabelOpt8          = null;
        public  string  m_strLabelPeerAddr      = null;

    }





    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    //                                                                     //
    //          C L A S S   Esp32BleAppCfgProfile                          //
    //                                                                     //
    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    public  class  Esp32BleAppCfgProfile
    {

        //=================================================================//
        //                                                                 //
        //      D A T A   S E C T I O N                                    //
        //                                                                 //
        //=================================================================//

        //-------------------------------------------------------------------
        //  Types
        //-------------------------------------------------------------------

        public  delegate  void  DlgtInfoMessageHandler (String strInfoMessage_p);



        //-------------------------------------------------------------------
        //  Attributes
        //-------------------------------------------------------------------

        private  IReadOnlyList<GattDeviceService>   m_BleDeviceGattServiceList  = null;
        private  BleGattServiceDevMnt               m_BleGattServiceDevMnt      = null;
        private  BleGattServiceWifi                 m_BleGattServiceWifi        = null;
        private  BleGattServiceAppRt                m_BleGattServiceAppRt       = null;

        private  DlgtInfoMessageHandler             m_CbInfoMessageHandler      = null;



        //-------------------------------------------------------------------
        //  Error Codes
        //-------------------------------------------------------------------

        #region Error Codes
        readonly int E_BLUETOOTH_ABORT = unchecked((int)0x80004004);
        #endregion





        //=================================================================//
        //                                                                 //
        //      C O D E   S E C T I O N                                    //
        //                                                                 //
        //=================================================================//

        //-----------------------------------------------------------------//
        //                                                                 //
        //      P U B L I C   M E T H O D E S                              //
        //                                                                 //
        //-----------------------------------------------------------------//

        //-------------------------------------------------------------------
        //  Constructor
        //-------------------------------------------------------------------
        public  Esp32BleAppCfgProfile (IReadOnlyList<GattDeviceService> BleDeviceGattServiceList_p,
                                       DlgtInfoMessageHandler CbInfoMessageHandler_p)
        {

            m_BleDeviceGattServiceList = BleDeviceGattServiceList_p;
            m_CbInfoMessageHandler     = CbInfoMessageHandler_p;

            return;

        }



        //-------------------------------------------------------------------
        //  EstablishProfile (Esp32-specific BLE Profile)
        //-------------------------------------------------------------------
        public  async  Task<bool>  EstablishProfile (IReadOnlyList<GattDeviceService> BleDeviceGattServiceList_p)
        {

            bool  fProfileValid;

            m_BleDeviceGattServiceList = BleDeviceGattServiceList_p;

            fProfileValid = await EstablishProfile();

            return (fProfileValid);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  EstablishProfile()
        {

            GattDeviceService  BleGattService;
            Guid               ServiceUuid;
            StringBuilder      ServiceDescription;
            int                iCharacteristicsCnt;
            int                iIdx;
            bool               fProfileValid;

            // Is <m_BleDeviceGattServiceList> set?
            if (m_BleDeviceGattServiceList == null)
            {
                return (false);
            }
            InfoMessageHandler("Establish Profile... ");


            //---------------------------------------------------------------
            // Step(1): Associate Esp32-specific BleServices Instances
            //---------------------------------------------------------------
            for (iIdx=0; iIdx<m_BleDeviceGattServiceList.Count; iIdx++)
            {
                BleGattService = m_BleDeviceGattServiceList[iIdx];
                ServiceUuid = BleGattService.Uuid;

                if (ServiceUuid == BleGattServiceDevMnt.GUID)
                {
                    m_BleGattServiceDevMnt = new BleGattServiceDevMnt(BleGattService);
                }
                if (ServiceUuid == BleGattServiceWifi.GUID)
                {
                    m_BleGattServiceWifi = new BleGattServiceWifi(BleGattService);
                }
                if (ServiceUuid == BleGattServiceAppRt.GUID)
                {
                    m_BleGattServiceAppRt = new BleGattServiceAppRt(BleGattService);
                }
            }


            //---------------------------------------------------------------
            // Step(2): Check if all expected BleServices are available
            //---------------------------------------------------------------
            ServiceDescription = new StringBuilder();
            fProfileValid = true;

            ServiceDescription.Append("\nServiceDevMnt   -> ");
            if (m_BleGattServiceDevMnt != null)
            {
                ServiceDescription.Append(String.Format("ServiceUuid: {0}", BleGattServiceDevMnt.GUID));
            }
            else
            {
                ServiceDescription.Append("NOT FOUND!");
                fProfileValid = false;
            }

            ServiceDescription.Append("\nServiceWifiCfg  -> ");
            if (m_BleGattServiceWifi != null)
            {
                ServiceDescription.Append(String.Format("ServiceUuid: {0}", BleGattServiceWifi.GUID));
            }
            else
            {
                ServiceDescription.Append("NOT FOUND!");
                fProfileValid = false;
            }

            ServiceDescription.Append("\nServiceAppRtCfg -> ");
            if (m_BleGattServiceAppRt != null)
            {
                ServiceDescription.Append(String.Format("ServiceUuid: {0}", BleGattServiceAppRt.GUID));
            }
            else
            {
                ServiceDescription.Append("NOT FOUND!");
                fProfileValid = false;
            }

            ServiceDescription.Append("\n");
            InfoMessageHandler(ServiceDescription.ToString());


            //---------------------------------------------------------------
            // Step(3): Establish Characteristics
            //---------------------------------------------------------------
            if ( fProfileValid )
            {
                InfoMessageHandler("Establish Characteristics... ");
                iCharacteristicsCnt = await EstablishCharacteristics();
                if (iCharacteristicsCnt > 0)
                {
                    InfoMessageHandler("done.\n");
                }
                else
                {
                    InfoMessageHandler("FAILED!\n");
                    switch (iCharacteristicsCnt)
                    {
                        case -1:
                        {
                            InfoMessageHandler("Error accessing service.\n");
                            break;
                        }
                        case -2:
                        {
                            InfoMessageHandler("Error accessing service.\n");
                            break;
                        }
                        case -3:
                        {
                            InfoMessageHandler("Restricted service, can't read characteristics.\n");
                            break;
                        }
                    }
                    fProfileValid = false;
                }
            }


            return (fProfileValid);

        }



        //-------------------------------------------------------------------
        //  DisposeProfile (Esp32-specific BLE Profile)
        //-------------------------------------------------------------------
        public  void  DisposeProfile()
        {

            InfoMessageHandler("Dispose Profile...\n");

            if (m_BleGattServiceDevMnt != null)
            {
                m_BleGattServiceDevMnt.Dispose();
                m_BleGattServiceDevMnt = null;
            }

            if (m_BleGattServiceWifi != null)
            {
                m_BleGattServiceWifi.Dispose();
                m_BleGattServiceWifi = null;
            }

            if (m_BleGattServiceAppRt != null)
            {
                m_BleGattServiceAppRt.Dispose();
                m_BleGattServiceAppRt = null;
            }

            return;

        }



        //-------------------------------------------------------------------
        //  GetAppCfgData()
        //-------------------------------------------------------------------
        public  Esp32BleAppCfgData  GetAppCfgData()
        {

            Esp32BleAppCfgData  AppCfgData;

            AppCfgData = new Esp32BleAppCfgData();

            // Members assoziated to Service 'BleGattServiceDevMnt'
            if (m_BleGattServiceDevMnt != null)
            {
                AppCfgData.m_ui32DevType         = m_BleGattServiceDevMnt.DevType;
                AppCfgData.m_ui32SysTickCnt      = m_BleGattServiceDevMnt.SysTickCnt;
                AppCfgData.m_strDevName          = m_BleGattServiceDevMnt.DevName;
            }
            else
            {
                AppCfgData.m_ui32DevType         = 0;
                AppCfgData.m_ui32SysTickCnt      = 0;
                AppCfgData.m_strDevName          = "???";
            }

            // Members assoziated to Service 'BleGattServiceWifi'
            if (m_BleGattServiceWifi != null)
            {
                AppCfgData.m_strSSID             = m_BleGattServiceWifi.SSID;
                AppCfgData.m_strPasswd           = m_BleGattServiceWifi.Passwd;
                AppCfgData.m_strOwnAddr          = m_BleGattServiceWifi.OwnAddr;
                AppCfgData.m_ui16OwnMode         = m_BleGattServiceWifi.OwnMode;
                AppCfgData.m_ui16OwnModeFeatList = m_BleGattServiceWifi.OwnModeFeatList;
            }
            else
            {
                AppCfgData.m_strSSID             = "???";
                AppCfgData.m_strPasswd           = "???";
                AppCfgData.m_strOwnAddr          = "???";
                AppCfgData.m_ui16OwnMode         = 0;
                AppCfgData.m_ui16OwnModeFeatList = 0;
            }

            // Members assoziated to Service 'BleGattServiceAppRtCfg'
            if (m_BleGattServiceAppRt != null)
            {
                AppCfgData.m_ui16Opt1            = m_BleGattServiceAppRt.Opt1;
                AppCfgData.m_ui16Opt2            = m_BleGattServiceAppRt.Opt2;
                AppCfgData.m_ui16Opt3            = m_BleGattServiceAppRt.Opt3;
                AppCfgData.m_ui16Opt4            = m_BleGattServiceAppRt.Opt4;
                AppCfgData.m_ui16Opt5            = m_BleGattServiceAppRt.Opt5;
                AppCfgData.m_ui16Opt6            = m_BleGattServiceAppRt.Opt6;
                AppCfgData.m_ui16Opt7            = m_BleGattServiceAppRt.Opt7;
                AppCfgData.m_ui16Opt8            = m_BleGattServiceAppRt.Opt8;
                AppCfgData.m_strPeerAddr         = m_BleGattServiceAppRt.PeerAddr;

                AppCfgData.m_strLabelOpt1        = m_BleGattServiceAppRt.LabelOpt1;
                AppCfgData.m_strLabelOpt2        = m_BleGattServiceAppRt.LabelOpt2;
                AppCfgData.m_strLabelOpt3        = m_BleGattServiceAppRt.LabelOpt3;
                AppCfgData.m_strLabelOpt4        = m_BleGattServiceAppRt.LabelOpt4;
                AppCfgData.m_strLabelOpt5        = m_BleGattServiceAppRt.LabelOpt5;
                AppCfgData.m_strLabelOpt6        = m_BleGattServiceAppRt.LabelOpt6;
                AppCfgData.m_strLabelOpt7        = m_BleGattServiceAppRt.LabelOpt7;
                AppCfgData.m_strLabelOpt8        = m_BleGattServiceAppRt.LabelOpt8;
                AppCfgData.m_strLabelPeerAddr    = m_BleGattServiceAppRt.LabelPeerAddr;
            }
            else
            {
                AppCfgData.m_ui16Opt1            = 0;
                AppCfgData.m_ui16Opt2            = 0;
                AppCfgData.m_ui16Opt3            = 0;
                AppCfgData.m_ui16Opt4            = 0;
                AppCfgData.m_ui16Opt5            = 0;
                AppCfgData.m_ui16Opt6            = 0;
                AppCfgData.m_ui16Opt7            = 0;
                AppCfgData.m_ui16Opt8            = 0;
                AppCfgData.m_strPeerAddr         = "???";

                AppCfgData.m_strLabelOpt1        = null;
                AppCfgData.m_strLabelOpt2        = null;
                AppCfgData.m_strLabelOpt3        = null;
                AppCfgData.m_strLabelOpt4        = null;
                AppCfgData.m_strLabelOpt5        = null;
                AppCfgData.m_strLabelOpt6        = null;
                AppCfgData.m_strLabelOpt7        = null;
                AppCfgData.m_strLabelOpt8        = null;
                AppCfgData.m_strLabelPeerAddr    = null;
            }

            return (AppCfgData);

        }



        //-------------------------------------------------------------------
        //  SetAppCfgData()
        //-------------------------------------------------------------------
        public  bool  SetAppCfgData (Esp32BleAppCfgData AppCfgData_p)
        {

            bool  fResult;

            if (AppCfgData_p == null)
            {
                return (false);
            }

            fResult = true;

            // Members assoziated to Service 'BleGattServiceDevMnt'
            if (m_BleGattServiceDevMnt != null)
            {
                m_BleGattServiceDevMnt.DevName = AppCfgData_p.m_strDevName;
            }
            else
            {
                fResult = false;
            }

            // Members assoziated to Service 'BleGattServiceWifi'
            if (m_BleGattServiceWifi != null)
            {
                m_BleGattServiceWifi.SSID    = AppCfgData_p.m_strSSID;
                m_BleGattServiceWifi.Passwd  = AppCfgData_p.m_strPasswd;
                m_BleGattServiceWifi.OwnAddr = AppCfgData_p.m_strOwnAddr;
                m_BleGattServiceWifi.OwnMode = AppCfgData_p.m_ui16OwnMode;
            }
            else
            {
                fResult = false;
            }

            // Members assoziated to Service 'BleGattServiceAppRtCfg'
            if (m_BleGattServiceAppRt != null)
            {
                m_BleGattServiceAppRt.Opt1     = AppCfgData_p.m_ui16Opt1;
                m_BleGattServiceAppRt.Opt2     = AppCfgData_p.m_ui16Opt2;
                m_BleGattServiceAppRt.Opt3     = AppCfgData_p.m_ui16Opt3;
                m_BleGattServiceAppRt.Opt4     = AppCfgData_p.m_ui16Opt4;
                m_BleGattServiceAppRt.Opt5     = AppCfgData_p.m_ui16Opt5;
                m_BleGattServiceAppRt.Opt6     = AppCfgData_p.m_ui16Opt6;
                m_BleGattServiceAppRt.Opt7     = AppCfgData_p.m_ui16Opt7;
                m_BleGattServiceAppRt.Opt8     = AppCfgData_p.m_ui16Opt8;
                m_BleGattServiceAppRt.PeerAddr = AppCfgData_p.m_strPeerAddr;
            }
            else
            {
                fResult = false;
            }

            return (fResult);

        }



        //-------------------------------------------------------------------
        //  GetCurrentSysTickCnt()
        //-------------------------------------------------------------------
        public  async  Task<UInt32>  GetCurrentSysTickCnt()
        {

            UInt32  ui32SysTickCnt;

            if (m_BleGattServiceDevMnt != null)
            {
                ui32SysTickCnt = await m_BleGattServiceDevMnt.ReadCharacteristicSysTickCnt();
            }
            else
            {
                ui32SysTickCnt = 0;
            }

            return (ui32SysTickCnt);

        }



        //-------------------------------------------------------------------
        //  SaveConfig()
        //-------------------------------------------------------------------
        public  async  Task<int>  SaveConfig()
        {

            int   iCharacteristicsCnt;
            bool  fRes;

            // write/flush characterisic values
            iCharacteristicsCnt  = 0;
            iCharacteristicsCnt += await m_BleGattServiceDevMnt.FlushCharacteristics();
            iCharacteristicsCnt += await m_BleGattServiceWifi.FlushCharacteristics();
            iCharacteristicsCnt += await m_BleGattServiceAppRt.FlushCharacteristics();

            // write Save instruction
            fRes = await m_BleGattServiceDevMnt.WriteCharacteristicSaveCfg();
            if ( !fRes )
            {
                iCharacteristicsCnt = 0;
            }

            return (iCharacteristicsCnt);

        }



        //-------------------------------------------------------------------
        //  RestartDevice()
        //-------------------------------------------------------------------
        public  async  Task<bool>  RestartDevice()
        {

            bool  fRes;

            try
            {
                fRes = await m_BleGattServiceDevMnt.WriteCharacteristicRstDev();
            }
            catch (Exception ExceptionInfo_p)
            {
                if (ExceptionInfo_p.HResult == E_BLUETOOTH_ABORT)
                {
                    // When the Restart Command is executed, the ESP32 embedded system
                    // is restarted immediately. As a result, the started BLE operation
                    // is no longer acknowledged and indicated by the error code E_ABORT.
                    fRes = true;
                }
                else
                {
                    fRes = false;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  GetBleGattServiceDevMnt()
        //-------------------------------------------------------------------
        public  BleGattServiceDevMnt  GetBleGattServiceDevMnt()
        {

            return (m_BleGattServiceDevMnt);

        }



        //-------------------------------------------------------------------
        //  GetBleGattServiceWifi()
        //-------------------------------------------------------------------
        public  BleGattServiceWifi  GetBleGattServiceWifi()
        {

            return (m_BleGattServiceWifi);

        }



        //-------------------------------------------------------------------
        //  GetBleGattServiceAppRtCfg()
        //-------------------------------------------------------------------
        public  BleGattServiceAppRt  GetBleGattServiceAppRtCfg()
        {

            return (m_BleGattServiceAppRt);

        }





        //-----------------------------------------------------------------//
        //                                                                 //
        //      P R I V A T E   M E T H O D S                              //
        //                                                                 //
        //-----------------------------------------------------------------//

        //-------------------------------------------------------------------
        //  EstablishCharacteristics()
        //-------------------------------------------------------------------
        private  async  Task<int>  EstablishCharacteristics()
        {

            int  iCharacteristicsCnt;

            // establish/read characterisic values
            iCharacteristicsCnt  = 0;
            iCharacteristicsCnt += await m_BleGattServiceDevMnt.EstablishCharacteristics();
            iCharacteristicsCnt += await m_BleGattServiceWifi.EstablishCharacteristics();
            iCharacteristicsCnt += await m_BleGattServiceAppRt.EstablishCharacteristics();

            return (iCharacteristicsCnt);

        }



        //-------------------------------------------------------------------
        //  InfoMessageHandler()
        //-------------------------------------------------------------------
        private  void  InfoMessageHandler (String strInfoMessage_p)
        {

            if (m_CbInfoMessageHandler != null)
            {
                m_CbInfoMessageHandler (strInfoMessage_p);
            }

            return;

        }

    }





    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    //                                                                     //
    //          C L A S S   BleGattServiceDevMnt                           //
    //                                                                     //
    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    public  class  BleGattServiceDevMnt
    {

        //=================================================================//
        //                                                                 //
        //      D A T A   S E C T I O N                                    //
        //                                                                 //
        //=================================================================//

        //-------------------------------------------------------------------
        // Service specific GUID's
        //-------------------------------------------------------------------

        private  static  readonly  Guid  m_GuidServiceDevMnt                    = new Guid("00001000-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacDevMntDevType              = new Guid("00001100-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacDevMntSysTickCnt           = new Guid("00001200-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacDevMntDevName              = new Guid("00001300-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacDevMntSaveCfg              = new Guid("00001400-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacDevMntRstDev               = new Guid("00001500-0000-1000-8000-E776CC14FE69");



        //-------------------------------------------------------------------
        //  Attributes
        //-------------------------------------------------------------------

        private  GattDeviceService                  m_BleGattService            = null;
        private  IReadOnlyList<GattCharacteristic>  m_CharacteristicList        = null;
        private  GattCharacteristic                 m_CharacDevMntDevType       = null;
        private  GattCharacteristic                 m_CharacDevMntSysTickCnt    = null;
        private  GattCharacteristic                 m_CharacDevMntDevName       = null;
        private  GattCharacteristic                 m_CharacDevMntSaveCfg       = null;
        private  GattCharacteristic                 m_CharacDevMntRstDev        = null;

        private  string                             m_strDevName                = "";
        private  UInt32                             m_ui32DevType               = 0;
        private  UInt32                             m_ui32SysTickCnt            = 0;





        //=================================================================//
        //                                                                 //
        //      C O D E   S E C T I O N                                    //
        //                                                                 //
        //=================================================================//

        //-----------------------------------------------------------------//
        //                                                                 //
        //      P U B L I C   M E T H O D E S                              //
        //                                                                 //
        //-----------------------------------------------------------------//

        //-------------------------------------------------------------------
        //  STATIC: Get Service GUID
        //-------------------------------------------------------------------
        public  static  Guid  GUID
        {
            get
            {
                return (m_GuidServiceDevMnt);
            }
        }



        //-------------------------------------------------------------------
        //  Constructor
        //-------------------------------------------------------------------
        public  BleGattServiceDevMnt (GattDeviceService BleGattService_p)
        {

            m_BleGattService = BleGattService_p;
            return;

        }



        //-------------------------------------------------------------------
        //  Dispose()
        //-------------------------------------------------------------------
        public  void  Dispose()
        {

            m_BleGattService.Dispose();
            return;

        }



        //-------------------------------------------------------------------
        //  Property 'DevType'
        //-------------------------------------------------------------------
        public  UInt32  DevType
        {
            get
            {
                return (m_ui32DevType);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'SysTickCnt'
        //-------------------------------------------------------------------
        public  UInt32  SysTickCnt
        {
            get
            {
                return (m_ui32SysTickCnt);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'DevName'
        //-------------------------------------------------------------------
        public  string  DevName
        {
            set
            {
                m_strDevName = value;
            }
            get
            {
                return (m_strDevName);
            }
        }



        //-------------------------------------------------------------------
        //  EstablishCharacteristics()
        //-------------------------------------------------------------------
        public  async  Task<int>  EstablishCharacteristics()
        {

            DeviceAccessStatus         AccessStatus;
            GattCharacteristicsResult  GattCharacResult;
            GattCharacteristic         Characteristic;
            Guid                       CharacteristicUuid;
            int                        iIdx;


            //---------------------------------------------------------------
            // Step(1): Discover all available Characteristics
            //---------------------------------------------------------------
            m_CharacteristicList = null;
            try
            {
                // ensure access to device
                AccessStatus = await m_BleGattService.RequestAccessAsync();
                if (AccessStatus == DeviceAccessStatus.Allowed)
                {
                    // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only
                    // and the new Async functions to get the characteristics of unpaired devices as well.
                    GattCharacResult = await m_BleGattService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (GattCharacResult.Status == GattCommunicationStatus.Success)
                    {
                        m_CharacteristicList = GattCharacResult.Characteristics;
                    }
                    else
                    {
                        // error accessing service
                        return (-1);
                    }
                }
                else
                {
                    // error accessing service
                    return (-2);
                }
            }
            catch
            {
                // restricted service, can't read characteristics
                // on error, act as if there are no characteristics
                return (-3);
            }


            //---------------------------------------------------------------
            // Step(2): Associate Esp32-specific Characteristics
            //---------------------------------------------------------------
            if (m_CharacteristicList != null)
            {
                for (iIdx=0; iIdx<m_CharacteristicList.Count; iIdx++)
                {
                    Characteristic = m_CharacteristicList[iIdx];
                    CharacteristicUuid = Characteristic.Uuid;

                    if (CharacteristicUuid == m_GuidCharacDevMntDevType)
                    {
                        m_CharacDevMntDevType = Characteristic;
                        await ReadCharacteristicDevType();
                    }
                    if (CharacteristicUuid == m_GuidCharacDevMntSysTickCnt)
                    {
                        m_CharacDevMntSysTickCnt = Characteristic;
                        await ReadCharacteristicSysTickCnt();
                    }
                    if (CharacteristicUuid == m_GuidCharacDevMntDevName)
                    {
                        m_CharacDevMntDevName = Characteristic;
                        await ReadCharacteristicDevName();
                    }
                    if (CharacteristicUuid == m_GuidCharacDevMntSaveCfg)
                    {
                        m_CharacDevMntSaveCfg = Characteristic;
                    }
                    if (CharacteristicUuid == m_GuidCharacDevMntRstDev)
                    {
                        m_CharacDevMntRstDev = Characteristic;
                    }
                }
            }

            return (m_CharacteristicList.Count);

        }



        //-------------------------------------------------------------------
        //  FlushCharacteristics()
        //-------------------------------------------------------------------
        public  async  Task<int>  FlushCharacteristics()
        {

            GattCharacteristic  Characteristic;
            Guid                CharacteristicUuid;
            int                 iIdx;

            if (m_CharacteristicList != null)
            {
                for (iIdx=0; iIdx<m_CharacteristicList.Count; iIdx++)
                {
                    Characteristic = m_CharacteristicList[iIdx];
                    CharacteristicUuid = Characteristic.Uuid;

                    if (CharacteristicUuid == m_GuidCharacDevMntDevName)
                    {
                        await WriteCharacteristicDevName();
                    }
                }
            }

            return (m_CharacteristicList.Count);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicDevType()
        //-------------------------------------------------------------------
        public  async  Task<UInt32>  ReadCharacteristicDevType()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacDevMntDevType != null)
            {
                ReadResult = await m_CharacDevMntDevType.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_ui32DevType = (UInt32)BleUtils.DecodeInt32Value(Value);
                }
            }

            return (m_ui32DevType);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicSysTickCnt()
        //-------------------------------------------------------------------
        public  async  Task<UInt32>  ReadCharacteristicSysTickCnt()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacDevMntSysTickCnt != null)
            {
                ReadResult = await m_CharacDevMntSysTickCnt.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_ui32SysTickCnt = (UInt32)BleUtils.DecodeInt32Value(Value);
                }
            }

            return (m_ui32SysTickCnt);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicDevName()
        //-------------------------------------------------------------------
        public  async  Task<string>  ReadCharacteristicDevName()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacDevMntDevName != null)
            {
                ReadResult = await m_CharacDevMntDevName.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_strDevName = BleUtils.DecodeStringValue(Value);
                }
            }

            return (m_strDevName);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicDevName()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicDevName()
        {

            bool  fRes;

            fRes = await WriteCharacteristicDevName(m_strDevName);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicDevName(string strDevName_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacDevMntDevName != null)
            {
                Value = BleUtils.EncodeStringValue(strDevName_p);
                WriteResult = await m_CharacDevMntDevName.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicSaveCfg()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicSaveCfg()
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacDevMntSaveCfg != null)
            {
                Value = BleUtils.EncodeInt16Value(1);
                WriteResult = await m_CharacDevMntSaveCfg.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicRstDev()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicRstDev()
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacDevMntRstDev != null)
            {
                Value = BleUtils.EncodeInt16Value(1);
                WriteResult = await m_CharacDevMntRstDev.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }

    }





    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    //                                                                     //
    //          C L A S S   BleGattServiceWifi                             //
    //                                                                     //
    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    public  class  BleGattServiceWifi
    {

        //=================================================================//
        //                                                                 //
        //      D A T A   S E C T I O N                                    //
        //                                                                 //
        //=================================================================//

        //-------------------------------------------------------------------
        // Service specific GUID's
        //-------------------------------------------------------------------

        private  static  readonly  Guid  m_GuidServiceWifi                      = new Guid("00002000-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacWifiSSID                   = new Guid("00002100-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacWifiPasswd                 = new Guid("00002200-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacWifiOwnAddr                = new Guid("00002300-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacWifiOwnMode                = new Guid("00002400-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidDscrptWifiOwnModeFeatureList     = new Guid("00002400-0002-1000-8000-E776CC14FE69");


        //-------------------------------------------------------------------
        //  Attributes
        //-------------------------------------------------------------------

        private  GattDeviceService                  m_BleGattService            = null;
        private  IReadOnlyList<GattCharacteristic>  m_CharacteristicList        = null;
        private  GattCharacteristic                 m_CharacWifiSSID            = null;
        private  GattCharacteristic                 m_CharacWifiPasswd          = null;
        private  GattCharacteristic                 m_CharacWifiOwnAddr         = null;
        private  GattCharacteristic                 m_CharacWifiOwnMode         = null;

        private  string                             m_strSSID                   = "";
        private  string                             m_strPasswd                 = "";
        private  string                             m_strOwnAddr                = "";
        private  UInt16                             m_ui16OwnMode               = 0;
        private  UInt16                             m_ui16OwnModeFeatList       = 0;





        //=================================================================//
        //                                                                 //
        //      C O D E   S E C T I O N                                    //
        //                                                                 //
        //=================================================================//

        //-----------------------------------------------------------------//
        //                                                                 //
        //      P U B L I C   M E T H O D E S                              //
        //                                                                 //
        //-----------------------------------------------------------------//

        //-------------------------------------------------------------------
        //  STATIC: Get Service GUID
        //-------------------------------------------------------------------
        public  static  Guid  GUID
        {
            get
            {
                return (m_GuidServiceWifi);
            }
        }



        //-------------------------------------------------------------------
        //  Constructor
        //-------------------------------------------------------------------
        public  BleGattServiceWifi (GattDeviceService BleGattService_p)
        {

            m_BleGattService = BleGattService_p;
            return;

        }



        //-------------------------------------------------------------------
        //  Dispose()
        //-------------------------------------------------------------------
        public  void  Dispose()
        {

            m_BleGattService.Dispose();
            return;

        }



        //-------------------------------------------------------------------
        //  Property 'SSID'
        //-------------------------------------------------------------------
        public  string  SSID
        {
            set
            {
                m_strSSID = value;
            }
            get
            {
                return (m_strSSID);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'Passwd'
        //-------------------------------------------------------------------
        public  string  Passwd
        {
            set
            {
                m_strPasswd = value;
            }
            get
            {
                return (m_strPasswd);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'OwnAddr'
        //-------------------------------------------------------------------
        public  string  OwnAddr
        {
            set
            {
                m_strOwnAddr = value;
            }
            get
            {
                return (m_strOwnAddr);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'OwnMode'
        //-------------------------------------------------------------------
        public  UInt16  OwnMode
        {
            set
            {
                m_ui16OwnMode = value;
            }
            get
            {
                return (m_ui16OwnMode);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'OwnModeFeatList'
        //-------------------------------------------------------------------
        public  UInt16  OwnModeFeatList
        {
            get
            {
                return (m_ui16OwnModeFeatList);
            }
        }



        //-------------------------------------------------------------------
        //  EstablishCharacteristics()
        //-------------------------------------------------------------------
        public  async  Task<int>  EstablishCharacteristics()
        {

            DeviceAccessStatus         AccessStatus;
            GattCharacteristicsResult  GattCharacResult;
            GattCharacteristic         Characteristic;
            Guid                       CharacteristicUuid;
            int                        iIdx;


            //---------------------------------------------------------------
            // Step(1): Discover all available Characteristics
            //---------------------------------------------------------------
            m_CharacteristicList = null;
            try
            {
                // ensure access to device
                AccessStatus = await m_BleGattService.RequestAccessAsync();
                if (AccessStatus == DeviceAccessStatus.Allowed)
                {
                    // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only
                    // and the new Async functions to get the characteristics of unpaired devices as well.
                    GattCharacResult = await m_BleGattService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (GattCharacResult.Status == GattCommunicationStatus.Success)
                    {
                        m_CharacteristicList = GattCharacResult.Characteristics;
                    }
                    else
                    {
                        // error accessing service
                        return (-1);
                    }
                }
                else
                {
                    // error accessing service
                    return (-2);
                }
            }
            catch
            {
                // restricted service, can't read characteristics
                // on error, act as if there are no characteristics
                return (-3);
            }


            //---------------------------------------------------------------
            // Step(2): Associate Esp32-specific Characteristics
            //---------------------------------------------------------------
            if (m_CharacteristicList != null)
            {
                for (iIdx=0; iIdx<m_CharacteristicList.Count; iIdx++)
                {
                    Characteristic = m_CharacteristicList[iIdx];
                    CharacteristicUuid = Characteristic.Uuid;

                    if (CharacteristicUuid == m_GuidCharacWifiSSID)
                    {
                        m_CharacWifiSSID = Characteristic;
                        await ReadCharacteristicSSID();
                    }
                    if (CharacteristicUuid == m_GuidCharacWifiPasswd)
                    {
                        m_CharacWifiPasswd = Characteristic;
                        await ReadCharacteristicPasswd();
                    }
                    if (CharacteristicUuid == m_GuidCharacWifiOwnAddr)
                    {
                        m_CharacWifiOwnAddr = Characteristic;
                        await ReadCharacteristicOwnAddr();
                    }
                    if (CharacteristicUuid == m_GuidCharacWifiOwnMode)
                    {
                        m_CharacWifiOwnMode = Characteristic;
                        await ReadCharacteristicOwnMode();
                        await EnquireCharacteristicFeatList();
                    }
                }
            }

            return (m_CharacteristicList.Count);

        }



        //-------------------------------------------------------------------
        //  FlushCharacteristics()
        //-------------------------------------------------------------------
        public  async  Task<int>  FlushCharacteristics()
        {

            GattCharacteristic  Characteristic;
            Guid                CharacteristicUuid;
            int                 iIdx;

            if (m_CharacteristicList != null)
            {
                for (iIdx=0; iIdx<m_CharacteristicList.Count; iIdx++)
                {
                    Characteristic = m_CharacteristicList[iIdx];
                    CharacteristicUuid = Characteristic.Uuid;

                    if (CharacteristicUuid == m_GuidCharacWifiSSID)
                    {
                        await WriteCharacteristicSSID();
                    }
                    if (CharacteristicUuid == m_GuidCharacWifiPasswd)
                    {
                        await WriteCharacteristicPasswd();
                    }
                    if (CharacteristicUuid == m_GuidCharacWifiOwnAddr)
                    {
                        await WriteCharacteristicOwnAddr();
                    }
                    if (CharacteristicUuid == m_GuidCharacWifiOwnMode)
                    {
                        await WriteCharacteristicOwnMode();
                    }
                }
            }

            return (m_CharacteristicList.Count);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicSSID()
        //-------------------------------------------------------------------
        public  async  Task<string>  ReadCharacteristicSSID()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacWifiSSID != null)
            {
                ReadResult = await m_CharacWifiSSID.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_strSSID = BleUtils.DecodeStringValue(Value);
                }
            }

            return (m_strSSID);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicSSID()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicSSID()
        {

            bool  fRes;

            fRes = await WriteCharacteristicSSID(m_strSSID);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicSSID(string strSSID_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacWifiSSID != null)
            {
                Value = BleUtils.EncodeStringValue(strSSID_p);
                WriteResult = await m_CharacWifiSSID.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicPasswd()
        //-------------------------------------------------------------------
        public  async  Task<string>  ReadCharacteristicPasswd()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacWifiPasswd != null)
            {
                ReadResult = await m_CharacWifiPasswd.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_strPasswd = BleUtils.DecodeStringValue(Value);
                }
            }

            return (m_strPasswd);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicPasswd()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicPasswd()
        {

            bool  fRes;

            fRes = await WriteCharacteristicPasswd(m_strPasswd);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicPasswd (string strPasswd_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacWifiPasswd != null)
            {
                Value = BleUtils.EncodeStringValue(strPasswd_p);
                WriteResult = await m_CharacWifiPasswd.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicOwnAddr()
        //-------------------------------------------------------------------
        public  async  Task<string>  ReadCharacteristicOwnAddr()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacWifiOwnAddr != null)
            {
                ReadResult = await m_CharacWifiOwnAddr.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_strOwnAddr = BleUtils.DecodeStringValue(Value);
                }
            }

            return (m_strOwnAddr);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicOwnAddr()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOwnAddr()
        {

            bool  fRes;

            fRes = await WriteCharacteristicOwnAddr(m_strOwnAddr);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOwnAddr (string strOwnAddr_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacWifiOwnAddr != null)
            {
                Value = BleUtils.EncodeStringValue(strOwnAddr_p);
                WriteResult = await m_CharacWifiOwnAddr.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicOwnMode()
        //-------------------------------------------------------------------
        public  async  Task<UInt16>  ReadCharacteristicOwnMode()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacWifiOwnMode != null)
            {
                ReadResult = await m_CharacWifiOwnMode.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_ui16OwnMode = (UInt16)BleUtils.DecodeInt16Value(Value);
                }
            }

            return (m_ui16OwnMode);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicOwnMode()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOwnMode()
        {

            bool  fRes;

            fRes = await WriteCharacteristicOwnMode(m_ui16OwnMode);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOwnMode(UInt16 ui16OwnMode_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacWifiOwnMode != null)
            {
                Value = BleUtils.EncodeInt16Value((Int16)ui16OwnMode_p);
                WriteResult = await m_CharacWifiOwnMode.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  EnquireCharacteristicFeatList()
        //-------------------------------------------------------------------
        private  async  Task<UInt16>  EnquireCharacteristicFeatList()
        {

            GattDescriptorsResult  GattDscrptResult;
            GattDescriptor         GattDscrpt;
            GattReadResult         ReadResult;
            IBuffer                DescriptorValue;

            if ((m_CharacWifiOwnMode != null) && (m_GuidDscrptWifiOwnModeFeatureList != null))
            {
                GattDscrptResult = await m_CharacWifiOwnMode.GetDescriptorsForUuidAsync(m_GuidDscrptWifiOwnModeFeatureList, BluetoothCacheMode.Uncached);
                if (GattDscrptResult.Status == GattCommunicationStatus.Success)
                {
                    if (GattDscrptResult.Descriptors.Count > 0)
                    {
                        GattDscrpt = GattDscrptResult.Descriptors.FirstOrDefault();
                        if (GattDscrpt != null)
                        {
                            ReadResult = await GattDscrpt.ReadValueAsync(BluetoothCacheMode.Uncached);
                            if (ReadResult.Status == GattCommunicationStatus.Success)
                            {
                                DescriptorValue = ReadResult.Value;
                                m_ui16OwnModeFeatList = (UInt16)BleUtils.DecodeInt16Value(DescriptorValue);
                            }
                        }
                    }
                }
            }

            return (m_ui16OwnModeFeatList);

        }

    }





    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    //                                                                     //
    //          C L A S S   BleGattServiceAppRt                            //
    //                                                                     //
    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    public  class  BleGattServiceAppRt
    {

        //=================================================================//
        //                                                                 //
        //      D A T A   S E C T I O N                                    //
        //                                                                 //
        //=================================================================//

        //-------------------------------------------------------------------
        // Service specific GUID's
        //-------------------------------------------------------------------

        private  static  readonly  Guid  m_GuidServiceAppRt                     = new Guid("00003000-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacAppRtOpt1                  = new Guid("00003100-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacAppRtOpt2                  = new Guid("00003200-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacAppRtOpt3                  = new Guid("00003300-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacAppRtOpt4                  = new Guid("00003400-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacAppRtOpt5                  = new Guid("00003500-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacAppRtOpt6                  = new Guid("00003600-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacAppRtOpt7                  = new Guid("00003700-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacAppRtOpt8                  = new Guid("00003800-0000-1000-8000-E776CC14FE69");
        private  static  readonly  Guid  m_GuidCharacAppRtPeerAddr              = new Guid("00003900-0000-1000-8000-E776CC14FE69");

        private  static  readonly  Guid  m_GuidDescriptorOffset                 = new Guid("00000000-0001-0000-0000-000000000000");



        //-------------------------------------------------------------------
        //  Attributes
        //-------------------------------------------------------------------

        private  GattDeviceService                  m_BleGattService            = null;
        private  IReadOnlyList<GattCharacteristic>  m_CharacteristicList        = null;
        private  GattCharacteristic                 m_CharacAppRtOpt1           = null;
        private  GattCharacteristic                 m_CharacAppRtOpt2           = null;
        private  GattCharacteristic                 m_CharacAppRtOpt3           = null;
        private  GattCharacteristic                 m_CharacAppRtOpt4           = null;
        private  GattCharacteristic                 m_CharacAppRtOpt5           = null;
        private  GattCharacteristic                 m_CharacAppRtOpt6           = null;
        private  GattCharacteristic                 m_CharacAppRtOpt7           = null;
        private  GattCharacteristic                 m_CharacAppRtOpt8           = null;
        private  GattCharacteristic                 m_CharacAppRtPeerAddr       = null;

        private  UInt16                             m_ui16Opt1                  = 0;
        private  UInt16                             m_ui16Opt2                  = 0;
        private  UInt16                             m_ui16Opt3                  = 0;
        private  UInt16                             m_ui16Opt4                  = 0;
        private  UInt16                             m_ui16Opt5                  = 0;
        private  UInt16                             m_ui16Opt6                  = 0;
        private  UInt16                             m_ui16Opt7                  = 0;
        private  UInt16                             m_ui16Opt8                  = 0;
        private  string                             m_strPeerAddr               = "";

        private  string                             m_strLabelOpt1              = null;
        private  string                             m_strLabelOpt2              = null;
        private  string                             m_strLabelOpt3              = null;
        private  string                             m_strLabelOpt4              = null;
        private  string                             m_strLabelOpt5              = null;
        private  string                             m_strLabelOpt6              = null;
        private  string                             m_strLabelOpt7              = null;
        private  string                             m_strLabelOpt8              = null;
        private  string                             m_strLabelPeerAddr          = null;





        //=================================================================//
        //                                                                 //
        //      C O D E   S E C T I O N                                    //
        //                                                                 //
        //=================================================================//

        //-----------------------------------------------------------------//
        //                                                                 //
        //      P U B L I C   M E T H O D E S                              //
        //                                                                 //
        //-----------------------------------------------------------------//

        //-------------------------------------------------------------------
        //  STATIC: Get Service GUID
        //-------------------------------------------------------------------
        public  static  Guid  GUID
        {
            get
            {
                return (m_GuidServiceAppRt);
            }
        }



        //-------------------------------------------------------------------
        //  Constructor
        //-------------------------------------------------------------------
        public  BleGattServiceAppRt (GattDeviceService BleGattService_p)
        {

            m_BleGattService = BleGattService_p;
            return;

        }



        //-------------------------------------------------------------------
        //  Dispose()
        //-------------------------------------------------------------------
        public  void  Dispose()
        {

            m_BleGattService.Dispose();
            return;

        }



        //-------------------------------------------------------------------
        //  Property 'Opt1'
        //-------------------------------------------------------------------
        public  UInt16  Opt1
        {
            set
            {
                m_ui16Opt1 = value;
            }
            get
            {
                return (m_ui16Opt1);
            }
        }
        //-------------------------------------------------------------------
        public  String  LabelOpt1
        {
            get
            {
                return (m_strLabelOpt1);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'Opt2'
        //-------------------------------------------------------------------
        public  UInt16  Opt2
        {
            set
            {
                m_ui16Opt2 = value;
            }
            get
            {
                return (m_ui16Opt2);
            }
        }
        //-------------------------------------------------------------------
        public  String  LabelOpt2
        {
            get
            {
                return (m_strLabelOpt2);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'Opt3'
        //-------------------------------------------------------------------
        public  UInt16  Opt3
        {
            set
            {
                m_ui16Opt3 = value;
            }
            get
            {
                return (m_ui16Opt3);
            }
        }
        //-------------------------------------------------------------------
        public  String  LabelOpt3
        {
            get
            {
                return (m_strLabelOpt3);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'Opt4'
        //-------------------------------------------------------------------
        public  UInt16  Opt4
        {
            set
            {
                m_ui16Opt4 = value;
            }
            get
            {
                return (m_ui16Opt4);
            }
        }
        //-------------------------------------------------------------------
        public  String  LabelOpt4
        {
            get
            {
                return (m_strLabelOpt4);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'Opt5'
        //-------------------------------------------------------------------
        public  UInt16  Opt5
        {
            set
            {
                m_ui16Opt5 = value;
            }
            get
            {
                return (m_ui16Opt5);
            }
        }
        //-------------------------------------------------------------------
        public  String  LabelOpt5
        {
            get
            {
                return (m_strLabelOpt5);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'Opt6'
        //-------------------------------------------------------------------
        public  UInt16  Opt6
        {
            set
            {
                m_ui16Opt6 = value;
            }
            get
            {
                return (m_ui16Opt6);
            }
        }
        //-------------------------------------------------------------------
        public  String  LabelOpt6
        {
            get
            {
                return (m_strLabelOpt6);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'Opt7'
        //-------------------------------------------------------------------
        public  UInt16  Opt7
        {
            set
            {
                m_ui16Opt7 = value;
            }
            get
            {
                return (m_ui16Opt7);
            }
        }
        //-------------------------------------------------------------------
        public  String  LabelOpt7
        {
            get
            {
                return (m_strLabelOpt7);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'Opt8'
        //-------------------------------------------------------------------
        public  UInt16  Opt8
        {
            set
            {
                m_ui16Opt8 = value;
            }
            get
            {
                return (m_ui16Opt8);
            }
        }
        //-------------------------------------------------------------------
        public  String  LabelOpt8
        {
            get
            {
                return (m_strLabelOpt8);
            }
        }



        //-------------------------------------------------------------------
        //  Property 'PeerAddr'
        //-------------------------------------------------------------------
        public  string  PeerAddr
        {
            set
            {
                m_strPeerAddr = value;
            }
            get
            {
                return (m_strPeerAddr);
            }
        }
        //-------------------------------------------------------------------
        public  String  LabelPeerAddr
        {
            get
            {
                return (m_strLabelPeerAddr);
            }
        }



        //-------------------------------------------------------------------
        //  EstablishCharacteristics()
        //-------------------------------------------------------------------
        public  async  Task<int>  EstablishCharacteristics()
        {

            DeviceAccessStatus         AccessStatus;
            GattCharacteristicsResult  GattCharacResult;
            GattCharacteristic         Characteristic;
            Guid                       CharacteristicUuid;
            int                        iIdx;


            //---------------------------------------------------------------
            // Step(1): Discover all available Characteristics
            //---------------------------------------------------------------
            m_CharacteristicList = null;
            try
            {
                // ensure access to device
                AccessStatus = await m_BleGattService.RequestAccessAsync();
                if (AccessStatus == DeviceAccessStatus.Allowed)
                {
                    // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only
                    // and the new Async functions to get the characteristics of unpaired devices as well.
                    GattCharacResult = await m_BleGattService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (GattCharacResult.Status == GattCommunicationStatus.Success)
                    {
                        m_CharacteristicList = GattCharacResult.Characteristics;
                    }
                    else
                    {
                        // error accessing service
                        return (-1);
                    }
                }
                else
                {
                    // error accessing service
                    return (-2);
                }
            }
            catch
            {
                // restricted service, can't read characteristics
                // on error, act as if there are no characteristics
                return (-3);
            }


            //---------------------------------------------------------------
            // Step(2): Associate Esp32-specific Characteristics
            //---------------------------------------------------------------
            if (m_CharacteristicList != null)
            {
                for (iIdx=0; iIdx<m_CharacteristicList.Count; iIdx++)
                {
                    Characteristic = m_CharacteristicList[iIdx];
                    CharacteristicUuid = Characteristic.Uuid;

                    if (CharacteristicUuid == m_GuidCharacAppRtOpt1)
                    {
                        m_CharacAppRtOpt1 = Characteristic;
                        await ReadCharacteristicOpt1();
                        await EnquireCharacLabelOpt1();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt2)
                    {
                        m_CharacAppRtOpt2 = Characteristic;
                        await ReadCharacteristicOpt2();
                        await EnquireCharacLabelOpt2();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt3)
                    {
                        m_CharacAppRtOpt3 = Characteristic;
                        await ReadCharacteristicOpt3();
                        await EnquireCharacLabelOpt3();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt4)
                    {
                        m_CharacAppRtOpt4 = Characteristic;
                        await ReadCharacteristicOpt4();
                        await EnquireCharacLabelOpt4();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt5)
                    {
                        m_CharacAppRtOpt5 = Characteristic;
                        await ReadCharacteristicOpt5();
                        await EnquireCharacLabelOpt5();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt6)
                    {
                        m_CharacAppRtOpt6 = Characteristic;
                        await ReadCharacteristicOpt6();
                        await EnquireCharacLabelOpt6();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt7)
                    {
                        m_CharacAppRtOpt7 = Characteristic;
                        await ReadCharacteristicOpt7();
                        await EnquireCharacLabelOpt7();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt8)
                    {
                        m_CharacAppRtOpt8 = Characteristic;
                        await ReadCharacteristicOpt8();
                        await EnquireCharacLabelOpt8();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtPeerAddr)
                    {
                        m_CharacAppRtPeerAddr = Characteristic;
                        await ReadCharacteristicPeerAddr();
                        await EnquireCharacLabelPeerAddr();
                    }
                }
            }


            return (m_CharacteristicList.Count);

        }



        //-------------------------------------------------------------------
        //  FlushCharacteristics()
        //-------------------------------------------------------------------
        public  async  Task<int>  FlushCharacteristics()
        {

            GattCharacteristic  Characteristic;
            Guid                CharacteristicUuid;
            int                 iIdx;

            if (m_CharacteristicList != null)
            {
                for (iIdx=0; iIdx<m_CharacteristicList.Count; iIdx++)
                {
                    Characteristic = m_CharacteristicList[iIdx];
                    CharacteristicUuid = Characteristic.Uuid;

                    if (CharacteristicUuid == m_GuidCharacAppRtOpt1)
                    {
                        await WriteCharacteristicOpt1();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt2)
                    {
                        await WriteCharacteristicOpt2();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt3)
                    {
                        await WriteCharacteristicOpt3();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt4)
                    {
                        await WriteCharacteristicOpt4();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt5)
                    {
                        await WriteCharacteristicOpt5();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt6)
                    {
                        await WriteCharacteristicOpt6();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt7)
                    {
                        await WriteCharacteristicOpt7();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtOpt8)
                    {
                        await WriteCharacteristicOpt8();
                    }
                    if (CharacteristicUuid == m_GuidCharacAppRtPeerAddr)
                    {
                        await WriteCharacteristicPeerAddr();
                    }
                }
            }

            return (m_CharacteristicList.Count);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicOpt1()
        //-------------------------------------------------------------------
        public  async  Task<UInt16>  ReadCharacteristicOpt1()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacAppRtOpt1 != null)
            {
                ReadResult = await m_CharacAppRtOpt1.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_ui16Opt1 = (UInt16)BleUtils.DecodeInt16Value(Value);
                }
            }

            return (m_ui16Opt1);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicOpt1()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt1()
        {

            bool  fRes;

            fRes = await WriteCharacteristicOpt1(m_ui16Opt1);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt1(UInt16 ui16Opt1_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacAppRtOpt1 != null)
            {
                Value = BleUtils.EncodeInt16Value((Int16)ui16Opt1_p);
                WriteResult = await m_CharacAppRtOpt1.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  EnquireCharacLabelOpt1()
        //-------------------------------------------------------------------
        public  async  Task<string>  EnquireCharacLabelOpt1()
        {

            if (m_CharacAppRtOpt1 != null)
            {
                m_strLabelOpt1 = await EnquireCharacteristicLabel(m_CharacAppRtOpt1);
            }

            return (m_strLabelOpt1);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicOpt2()
        //-------------------------------------------------------------------
        public  async  Task<UInt16>  ReadCharacteristicOpt2()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacAppRtOpt2 != null)
            {
                ReadResult = await m_CharacAppRtOpt2.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_ui16Opt2 = (UInt16)BleUtils.DecodeInt16Value(Value);
                }
            }

            return (m_ui16Opt2);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicOpt2()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt2()
        {

            bool  fRes;

            fRes = await WriteCharacteristicOpt2(m_ui16Opt2);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt2(UInt16 ui16Opt2_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacAppRtOpt2 != null)
            {
                Value = BleUtils.EncodeInt16Value((Int16)ui16Opt2_p);
                WriteResult = await m_CharacAppRtOpt2.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  EnquireCharacLabelOpt2()
        //-------------------------------------------------------------------
        public  async  Task<string>  EnquireCharacLabelOpt2()
        {

            if (m_CharacAppRtOpt2 != null)
            {
                m_strLabelOpt2 = await EnquireCharacteristicLabel(m_CharacAppRtOpt2);
            }

            return (m_strLabelOpt2);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicOpt3()
        //-------------------------------------------------------------------
        public  async  Task<UInt16>  ReadCharacteristicOpt3()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacAppRtOpt3 != null)
            {
                ReadResult = await m_CharacAppRtOpt3.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_ui16Opt3 = (UInt16)BleUtils.DecodeInt16Value(Value);
                }
            }

            return (m_ui16Opt3);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicOpt3()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt3()
        {

            bool  fRes;

            fRes = await WriteCharacteristicOpt3(m_ui16Opt3);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt3(UInt16 ui16Opt3_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacAppRtOpt3 != null)
            {
                Value = BleUtils.EncodeInt16Value((Int16)ui16Opt3_p);
                WriteResult = await m_CharacAppRtOpt3.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  EnquireCharacLabelOpt3()
        //-------------------------------------------------------------------
        public  async  Task<string>  EnquireCharacLabelOpt3()
        {

            if (m_CharacAppRtOpt3 != null)
            {
                m_strLabelOpt3 = await EnquireCharacteristicLabel(m_CharacAppRtOpt3);
            }

            return (m_strLabelOpt3);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicOpt4()
        //-------------------------------------------------------------------
        public  async  Task<UInt16>  ReadCharacteristicOpt4()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacAppRtOpt4 != null)
            {
                ReadResult = await m_CharacAppRtOpt4.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_ui16Opt4 = (UInt16)BleUtils.DecodeInt16Value(Value);
                }
            }

            return (m_ui16Opt4);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicOpt4()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt4()
        {

            bool  fRes;

            fRes = await WriteCharacteristicOpt4(m_ui16Opt4);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt4(UInt16 ui16Opt4_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacAppRtOpt4 != null)
            {
                Value = BleUtils.EncodeInt16Value((Int16)ui16Opt4_p);
                WriteResult = await m_CharacAppRtOpt4.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  EnquireCharacLabelOpt4()
        //-------------------------------------------------------------------
        public  async  Task<string>  EnquireCharacLabelOpt4()
        {

            if (m_CharacAppRtOpt4 != null)
            {
                m_strLabelOpt4 = await EnquireCharacteristicLabel(m_CharacAppRtOpt4);
            }

            return (m_strLabelOpt4);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicOpt5()
        //-------------------------------------------------------------------
        public  async  Task<UInt16>  ReadCharacteristicOpt5()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacAppRtOpt5 != null)
            {
                ReadResult = await m_CharacAppRtOpt5.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_ui16Opt5 = (UInt16)BleUtils.DecodeInt16Value(Value);
                }
            }

            return (m_ui16Opt5);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicOpt5()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt5()
        {

            bool  fRes;

            fRes = await WriteCharacteristicOpt5(m_ui16Opt5);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt5(UInt16 ui16Opt5_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacAppRtOpt5 != null)
            {
                Value = BleUtils.EncodeInt16Value((Int16)ui16Opt5_p);
                WriteResult = await m_CharacAppRtOpt5.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  EnquireCharacLabelOpt5()
        //-------------------------------------------------------------------
        public  async  Task<string>  EnquireCharacLabelOpt5()
        {

            if (m_CharacAppRtOpt5 != null)
            {
                m_strLabelOpt5 = await EnquireCharacteristicLabel(m_CharacAppRtOpt5);
            }

            return (m_strLabelOpt5);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicOpt6()
        //-------------------------------------------------------------------
        public  async  Task<UInt16>  ReadCharacteristicOpt6()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacAppRtOpt6 != null)
            {
                ReadResult = await m_CharacAppRtOpt6.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_ui16Opt6 = (UInt16)BleUtils.DecodeInt16Value(Value);
                }
            }

            return (m_ui16Opt6);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicOpt6()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt6()
        {

            bool  fRes;

            fRes = await WriteCharacteristicOpt6(m_ui16Opt6);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt6(UInt16 ui16Opt6_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacAppRtOpt6 != null)
            {
                Value = BleUtils.EncodeInt16Value((Int16)ui16Opt6_p);
                WriteResult = await m_CharacAppRtOpt6.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  EnquireCharacLabelOpt6()
        //-------------------------------------------------------------------
        public  async  Task<string>  EnquireCharacLabelOpt6()
        {

            if (m_CharacAppRtOpt6 != null)
            {
                m_strLabelOpt6 = await EnquireCharacteristicLabel(m_CharacAppRtOpt6);
            }

            return (m_strLabelOpt6);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicOpt7()
        //-------------------------------------------------------------------
        public  async  Task<UInt16>  ReadCharacteristicOpt7()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacAppRtOpt7 != null)
            {
                ReadResult = await m_CharacAppRtOpt7.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_ui16Opt7 = (UInt16)BleUtils.DecodeInt16Value(Value);
                }
            }

            return (m_ui16Opt7);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicOpt7()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt7()
        {

            bool  fRes;

            fRes = await WriteCharacteristicOpt7(m_ui16Opt7);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt7(UInt16 ui16Opt7_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacAppRtOpt7 != null)
            {
                Value = BleUtils.EncodeInt16Value((Int16)ui16Opt7_p);
                WriteResult = await m_CharacAppRtOpt7.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  EnquireCharacLabelOpt7()
        //-------------------------------------------------------------------
        public  async  Task<string>  EnquireCharacLabelOpt7()
        {

            if (m_CharacAppRtOpt7 != null)
            {
                m_strLabelOpt7 = await EnquireCharacteristicLabel(m_CharacAppRtOpt7);
            }

            return (m_strLabelOpt7);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicOpt8()
        //-------------------------------------------------------------------
        public  async  Task<UInt16>  ReadCharacteristicOpt8()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacAppRtOpt8 != null)
            {
                ReadResult = await m_CharacAppRtOpt8.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_ui16Opt8 = (UInt16)BleUtils.DecodeInt16Value(Value);
                }
            }

            return (m_ui16Opt8);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicOpt8()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt8()
        {

            bool  fRes;

            fRes = await WriteCharacteristicOpt8(m_ui16Opt8);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicOpt8(UInt16 ui16Opt8_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacAppRtOpt8 != null)
            {
                Value = BleUtils.EncodeInt16Value((Int16)ui16Opt8_p);
                WriteResult = await m_CharacAppRtOpt8.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  EnquireCharacLabelOpt8()
        //-------------------------------------------------------------------
        public  async  Task<string>  EnquireCharacLabelOpt8()
        {

            if (m_CharacAppRtOpt8 != null)
            {
                m_strLabelOpt8 = await EnquireCharacteristicLabel(m_CharacAppRtOpt8);
            }

            return (m_strLabelOpt8);

        }



        //-------------------------------------------------------------------
        //  ReadCharacteristicPeerAddr()
        //-------------------------------------------------------------------
        public  async  Task<string>  ReadCharacteristicPeerAddr()
        {

            GattReadResult  ReadResult;
            IBuffer         Value;

            if (m_CharacAppRtPeerAddr != null)
            {
                ReadResult = await m_CharacAppRtPeerAddr.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (ReadResult.Status == GattCommunicationStatus.Success)
                {
                    Value = ReadResult.Value;
                    m_strPeerAddr = BleUtils.DecodeStringValue(Value);
                }
            }

            return (m_strPeerAddr);

        }



        //-------------------------------------------------------------------
        //  WriteCharacteristicPeerAddr()
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicPeerAddr()
        {

            bool  fRes;

            fRes = await WriteCharacteristicPeerAddr(m_strPeerAddr);
            return (fRes);

        }
        //-------------------------------------------------------------------
        public  async  Task<bool>  WriteCharacteristicPeerAddr (string strPeerAddr_p)
        {

            GattWriteResult  WriteResult;
            IBuffer          Value;
            bool             fRes;

            fRes = false;
            if (m_CharacAppRtPeerAddr != null)
            {
                Value = BleUtils.EncodeStringValue(strPeerAddr_p);
                WriteResult = await m_CharacAppRtPeerAddr.WriteValueWithResultAsync(Value);
                if (WriteResult.Status == GattCommunicationStatus.Success)
                {
                    fRes = true;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        //  EnquireCharacLabelPeerAddr()
        //-------------------------------------------------------------------
        public  async  Task<string>  EnquireCharacLabelPeerAddr()
        {

            if (m_CharacAppRtPeerAddr != null)
            {
                m_strLabelPeerAddr = await EnquireCharacteristicLabel(m_CharacAppRtPeerAddr);
            }

            return (m_strLabelPeerAddr);

        }



        //-------------------------------------------------------------------
        //  EnquireCharacteristicLabel()
        //-------------------------------------------------------------------
        private  async  Task<String>  EnquireCharacteristicLabel(GattCharacteristic Characteristic_p)
        {

            GattDescriptorsResult  GattDscrptResult;
            GattDescriptor         GattDscrpt;
            GattReadResult         ReadResult;
            Guid                   DescriptorGuid;
            IBuffer                DescriptorValue;
            String                 strDescrptLabel;

            strDescrptLabel = null;

            if (Characteristic_p != null)
            {
                // derive GUID for Descriptor from GUID from Characteristic
                DescriptorGuid = BuildDescriptorGuid(Characteristic_p.Uuid);

                GattDscrptResult = await Characteristic_p.GetDescriptorsForUuidAsync(DescriptorGuid, BluetoothCacheMode.Uncached);
                if (GattDscrptResult.Status == GattCommunicationStatus.Success)
                {
                    if (GattDscrptResult.Descriptors.Count > 0)
                    {
                        GattDscrpt = GattDscrptResult.Descriptors.FirstOrDefault();
                        if (GattDscrpt != null)
                        {
                            ReadResult = await GattDscrpt.ReadValueAsync(BluetoothCacheMode.Uncached);
                            if (ReadResult.Status == GattCommunicationStatus.Success)
                            {
                                DescriptorValue = ReadResult.Value;
                                strDescrptLabel = BleUtils.DecodeStringValue(DescriptorValue);
                            }
                        }
                    }
                }
            }

            return (strDescrptLabel);

        }



        //-------------------------------------------------------------------
        //  BuildDescriptorUuid()
        //-------------------------------------------------------------------
        private  Guid  BuildDescriptorGuid (Guid CharacteristicGuid_p)
        {
            Guid    DescriptorGuid;
            Byte[]  abCharacteristicGuid;
            Byte[]  abDescriptorOffsetGuid;
            int     iIdx;

            // derive GUID for Descriptor from GUID from Characteristic:
            //
            // In a GUID of a Characteristic the 1st group specifies the Characteristic purpose,
            // while the 2nd group is always '0000'. A Descriptor uses in its 1st group the same
            // value as it's assoziated Characteristic, but with a value unequal '0000' in the
            // 2nd group. For the first Descriptor the value '0001' is used, etc.
            //
            // Sample:
            // GUID Characteristic:         "00003100-0000-1000-8000-E776CC14FE69"
            // GUID Offset Descriptor:      "00000000-0001-0000-0000-000000000000"
            //                              --------------------------------------
            // Resulting Descriptor GUID:   "00003100-0001-1000-8000-E776CC14FE69"
            //
            abCharacteristicGuid = CharacteristicGuid_p.ToByteArray();
            abDescriptorOffsetGuid = m_GuidDescriptorOffset.ToByteArray();

            for (iIdx=0; iIdx<abCharacteristicGuid.Length; iIdx++)
            {
                abCharacteristicGuid[iIdx] += abDescriptorOffsetGuid[iIdx];
            }

            DescriptorGuid = new Guid(abCharacteristicGuid);

            return (DescriptorGuid);

        }

    }





    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    //                                                                     //
    //          C L A S S   BleUtils                                       //
    //                                                                     //
    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    public  class  BleUtils
    {

        //-------------------------------------------------------------------
        //  STATIC: DecodeStringValue()
        //-------------------------------------------------------------------
        public  static  string  DecodeStringValue (IBuffer Buffer_p)
        {

            byte[]  abData;
            string  strData;

            CryptographicBuffer.CopyToByteArray(Buffer_p, out abData);
            strData = Encoding.UTF8.GetString(abData);

            return (strData);

        }



        //-------------------------------------------------------------------
        //  STATIC: EncodeStringValue()
        //-------------------------------------------------------------------
        public  static  IBuffer  EncodeStringValue (string strData_p)
        {

            IBuffer  Buffer;

            if (strData_p == null)
            {
                strData_p = new string('\0', 1);
            }
            Buffer = CryptographicBuffer.ConvertStringToBinary(strData_p, BinaryStringEncoding.Utf8);

            return (Buffer);

        }



        //-------------------------------------------------------------------
        //  STATIC: DecodeInt16Value()
        //-------------------------------------------------------------------
        public  static  Int16  DecodeInt16Value (IBuffer Buffer_p)
        {

            byte[]  abData;
            Int16   i16Data;

            CryptographicBuffer.CopyToByteArray(Buffer_p, out abData);
            if (abData.Length != sizeof(Int16))
            {
                throw new ArgumentException(String.Format("IBuffer does not contain an Int16 variable"));
            }
            i16Data = BitConverter.ToInt16(abData, 0);

            return (i16Data);

        }



        //-------------------------------------------------------------------
        //  STATIC: EncodeInt16Value()
        //-------------------------------------------------------------------
        public  static  IBuffer  EncodeInt16Value (Int16 i16Data_p)
        {

            DataWriter  Writer;
            IBuffer     Buffer;

            Writer = new DataWriter();
            Writer.ByteOrder = ByteOrder.LittleEndian;
            Writer.WriteInt16(i16Data_p);
            Buffer = Writer.DetachBuffer();

            return (Buffer);

        }



        //-------------------------------------------------------------------
        //  STATIC: DecodeInt32Value()
        //-------------------------------------------------------------------
        public  static  Int32  DecodeInt32Value (IBuffer Buffer_p)
        {

            byte[]  abData;
            Int32   i32Data;

            CryptographicBuffer.CopyToByteArray(Buffer_p, out abData);
            if (abData.Length != sizeof(Int32))
            {
                throw new ArgumentException(String.Format("IBuffer does not contain an Int32 variable"));
            }
            i32Data = BitConverter.ToInt32(abData, 0);

            return (i32Data);

        }



        //-------------------------------------------------------------------
        //  STATIC: EncodeInt32Value()
        //-------------------------------------------------------------------
        public  static  IBuffer  EncodeInt32Value (Int16 i16Data_p)
        {

            DataWriter  Writer;
            IBuffer     Buffer;

            Writer = new DataWriter();
            Writer.ByteOrder = ByteOrder.LittleEndian;
            Writer.WriteInt32(i16Data_p);
            Buffer = Writer.DetachBuffer();

            return (Buffer);

        }



        //-------------------------------------------------------------------
        //  STATIC: FormatSystemTickCount()
        //-------------------------------------------------------------------
        public  static  String  FormatSystemTickCount (UInt32 ui32SysTickCnt_p)
        {

            UInt32  ui32SysTickCnt;
            String  strSysTickCnt;

            ui32SysTickCnt = ui32SysTickCnt_p / 1000;       // [ms] -> [sec]
            strSysTickCnt = ui32SysTickCnt.ToString();

            return (strSysTickCnt);

        }


    }


}




// EOF
