/****************************************************************************

  Copyright (c) 2021 Ronald Sieber

  Project:      Esp32Config - UwpApp
  Description:  Implementation of BLE Device Management

  -------------------------------------------------------------------------

  A recommended knowledge base for using the BLE subsystem in
  .NET/UWP applications is the Microsoft "Bluetooth Low Energy sample":

  https://docs.microsoft.com/en-us/samples/microsoft/windows-universal-samples/bluetoothle/

  Note: The BLE subsystem under .NET is only available for
        Universal Windows Platform Apps (UWP), but not for
        classic Windows Form Applications.

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
using System.Text;
using Windows.Devices.Radios;



namespace Esp32ConfigUwp
{

    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    //                                                                     //
    //          C L A S S   BleDevice                                      //
    //                                                                     //
    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    public class BleDevice
    {

        // Attributes
        private string  m_strDeviceId;
        private string  m_strDeviceName;
        private string  m_DeviceAddress;

        //-------------------------------------------------------------------
        public string DeviceId
        {
            set
            {
                m_strDeviceId = value;
            }
            get
            {
                return (m_strDeviceId);
            }
        }

        //-------------------------------------------------------------------
        public string DeviceName
        {
            set
            {
                m_strDeviceName = value;
            }
            get
            {
                return (m_strDeviceName);
            }
        }

        //-------------------------------------------------------------------
        public string DeviceAddress
        {
            set
            {
                m_DeviceAddress = value;
            }
            get
            {
                return (m_DeviceAddress);
            }
        }

    }





    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    //                                                                     //
    //          C L A S S   BleDeviceList                                  //
    //                                                                     //
    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    public class BleDeviceList : ObservableCollection<BleDevice>
    {

        //-------------------------------------------------------------------
        public  BleDeviceList() : base()
        {
            return;
        }



        //-------------------------------------------------------------------
        public  BleDevice  GetDeviceById (string strDeviceId_p)
        {

            BleDevice  Device;
            int        iIdx;

            Device = null;
            for (iIdx=0; iIdx<this.Count(); iIdx++)
            {
                if (this[iIdx].DeviceId == strDeviceId_p)
                {
                    Device = this[iIdx];
                    break;
                }
            }

            return (Device);

        }



        //-------------------------------------------------------------------
        public  BleDevice  GetDeviceByName (string strDeviceName_p)
        {

            BleDevice  Device;
            int        iIdx;

            Device = null;
            for (iIdx=0; iIdx<this.Count(); iIdx++)
            {
                if (this[iIdx].DeviceName == strDeviceName_p)
                {
                    Device = this[iIdx];
                    break;
                }
            }

            return (Device);

        }



        //-------------------------------------------------------------------
        public  bool  Delete (BleDevice Device_p)
        {

            string  strDeviceId;
            bool    fRes;

            strDeviceId = Device_p.DeviceId;
            fRes = Delete(strDeviceId);

            return (fRes);

        }
        //-------------------------------------------------------------------
        public  bool  Delete (string strDeviceId_p)
        {

            int   iIdx;
            bool  fRes;

            fRes = false;
            for (iIdx=0; iIdx<this.Count(); iIdx++)
            {
                if (this[iIdx].DeviceId == strDeviceId_p)
                {
                    RemoveAt(iIdx);
                    fRes = true;
                    break;
                }
            }

            return (fRes);

        }



        //-------------------------------------------------------------------
        public  bool  Update (BleDevice Device_p)
        {

            int   iIdx;
            bool  fRes;

            fRes = false;
            for (iIdx=0; iIdx<this.Count(); iIdx++)
            {
                if (this[iIdx].DeviceId == Device_p.DeviceId)
                {
                    RemoveAt(iIdx);
                    Insert(iIdx, Device_p);
                    fRes = true;
                    break;
                }
            }

            return (fRes);

        }

    }





    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    //                                                                     //
    //          C L A S S   BleDeviceManagement                            //
    //                                                                     //
    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    public class BleDeviceManagement
    {

        //=================================================================//
        //                                                                 //
        //      D A T A   S E C T I O N                                    //
        //                                                                 //
        //=================================================================//

        //-------------------------------------------------------------------
        //  Types
        //-------------------------------------------------------------------

        public  enum  tDeviceOperation
        {
            kAddDevice,
            kRemoveDevice
        }


        public  delegate  void  DlgtBleDeviceScanCompleted (int iBleDeviceCount_p);
        public  delegate  void  DlgtBleDeviceScanAborted (int iBleDeviceCount_p);
        public  delegate  void  DlgtRegisterBleDevice (BleDevice Device_p, tDeviceOperation DeviceOperation_p);
        public  delegate  void  DlgtInfoMessageHandler (String strInfoMessage_p);



        //-------------------------------------------------------------------
        //  Attributes
        //-------------------------------------------------------------------

        private  DeviceWatcher                      m_DeviceWatcher             = null;
        private  BleDeviceList                      m_ActiveBleDevicePool       = null;
        private  BleDeviceList                      m_ZombieBleDevicePool       = null;

        private  BluetoothLEDevice                  m_ConnectedBleDevice        = null;
        private  IReadOnlyList<GattDeviceService>   m_BleDeviceGattServiceList  = null;

        private  DlgtBleDeviceScanCompleted         m_CbBleDeviceScanCompleted  = null;
        private  DlgtBleDeviceScanAborted           m_CbBleDeviceScanAborted    = null;
        private  DlgtRegisterBleDevice              m_CbRegisterBleDevice       = null;
        private  DlgtInfoMessageHandler             m_CbInfoMessageHandler      = null;




        //-------------------------------------------------------------------
        //  Error Codes
        //-------------------------------------------------------------------

        #region Error Codes
        //readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        //readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        //readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
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
        public  BleDeviceManagement (DlgtBleDeviceScanCompleted CbBleDeviceScanCompleted_p,
                                     DlgtBleDeviceScanAborted CbBleDeviceScanAborted_p,
                                     DlgtRegisterBleDevice CbRegisterBleDevice_p,
                                     DlgtInfoMessageHandler CbInfoMessageHandler_p)
        {

            m_ZombieBleDevicePool = new BleDeviceList();
            m_ActiveBleDevicePool = new BleDeviceList();

            m_CbBleDeviceScanCompleted = CbBleDeviceScanCompleted_p;
            m_CbBleDeviceScanAborted   = CbBleDeviceScanAborted_p;
            m_CbRegisterBleDevice      = CbRegisterBleDevice_p;
            m_CbInfoMessageHandler     = CbInfoMessageHandler_p;

            return;

        }



        //-------------------------------------------------------------------
        //  STATIC: IsBleEnabled()
        //-------------------------------------------------------------------
        //  Return:     -1  -> No Bluetooth Adapter available
        //               0  -> Bluetooth Adapter (Radio) turned off
        //               1  -> Bluetooth available
        //-------------------------------------------------------------------
        public  static  async Task<int>  IsBleEnabled()
        {

            BluetoothAdapter  BtAdapter;
            Radio             BtRadio;

            BtAdapter = await BluetoothAdapter.GetDefaultAsync();
            if (BtAdapter == null)
            {
                // no BT adapter found
                return (-1);
            }
            if ( !BtAdapter.IsCentralRoleSupported )
            {
                // no active BT adapter found
                return (-1);
            }

            BtRadio = await BtAdapter.GetRadioAsync();
            if (BtRadio == null)
            {
                // probably BT adapter just removed
                return (-1);
            }
            
            if (BtRadio.State == RadioState.On)
            {
                // BT adapter is ON
                return (1);
            }

            // BT adapter is OFF
            return (0);

        }



        //-------------------------------------------------------------------
        //  BleScanStart()
        //-------------------------------------------------------------------
        public  bool  BleScanStart()
        {

            string[]  astrRequestedProperties;
            string    strAllBluetoothLEDevices;

            // List of required properties
            // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
            astrRequestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

            // ProtocolId is used to filter only for required device types (Standard BT, BLE, ...)
            // More details see here https://docs.microsoft.com/de-de/windows/uwp/devices-sensors/aep-service-class-ids
            // Bluetooth-Protocol - ID:    { e0cbf06c - cd8b - 4647 - bb8a - 263b43f0F974}
            // Bluetooth-LE-Protocol - ID: { bb7bb05e - 5972 - 42b5 - 94fc - 76eaa7084d49}
            //                                                            |---- Bluetooth-LE-Protocol - ID ----|
            strAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

            m_DeviceWatcher = DeviceInformation.CreateWatcher(strAllBluetoothLEDevices,           // set to Bluetooth-LE-Protocol ID
                                                              astrRequestedProperties,
                                                              DeviceInformationKind.AssociationEndpoint);

            // register event handlers before starting the watcher
            m_DeviceWatcher.Added                += DeviceWatcher_Added;
            m_DeviceWatcher.Updated              += DeviceWatcher_Updated;
            m_DeviceWatcher.Removed              += DeviceWatcher_Removed;
            m_DeviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            m_DeviceWatcher.Stopped              += DeviceWatcher_Stopped;

            // start with empty device lists
            m_ActiveBleDevicePool.Clear();
            m_ZombieBleDevicePool.Clear();

            // Start the watcher. Active enumeration is limited to approximately 30 seconds.
            m_DeviceWatcher.Start();

            return (true);

        }



        //-------------------------------------------------------------------
        //  BleScanStop()
        //-------------------------------------------------------------------
        public  bool  BleScanStop()
        {

            // check if watcher is running (and thus stopable)
            if (m_DeviceWatcher.Status != DeviceWatcherStatus.Started)
            {
                return (false);
            }

            // stop watcher
            m_DeviceWatcher.Stop();

            // remove all handlers from watcher
            m_DeviceWatcher.Added                -= DeviceWatcher_Added;
            m_DeviceWatcher.Updated              -= DeviceWatcher_Updated;
            m_DeviceWatcher.Removed              -= DeviceWatcher_Removed;
            m_DeviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
            m_DeviceWatcher.Stopped              -= DeviceWatcher_Stopped;

            return (true);

        }



        //-------------------------------------------------------------------
        //  GetBleDevicePool()
        //-------------------------------------------------------------------
        public  BleDeviceList  GetBleDevicePool()
        {

            return (m_ActiveBleDevicePool);

        }



        //-------------------------------------------------------------------
        //  BleDeviceConnect()
        //-------------------------------------------------------------------
        public  async  Task<int>  BleDeviceConnect (BleDevice Device_p)
        {

            GattDeviceServicesResult  BleGattServicesRes;
            int                       iGattServiceCount;


            //---------------------------------------------------------------
            // Step(1): Connect to BLE Device
            //---------------------------------------------------------------
            try
            {
                // IMPORTANT: This code needs to enable Capability "Bluetooth" in the Application Manfest
                //            -> ProjectExplorer -> Doubleclick Properties -> Button PackageManifest...
                //               Tab Capabilities -> Check Bluetooth
                //            See https://stackoverflow.com/questions/39479747/bluetoothledevice-fromidasync-returning-null
                //
                //                                   "BluetoothLE#BluetoothLE00:1a:7d:da:71:15-30:ae:a4:6c:4b:ea"
                //                                                                  |
                //                                                                  v
                m_ConnectedBleDevice = await BluetoothLEDevice.FromIdAsync(Device_p.DeviceId);
                if (m_ConnectedBleDevice == null)
                {
                    InfoMessageHandler("ERROR: Failed to connect to Device\n");
                    return (-1);
                }
            }
            catch (Exception ExceptionInfo_p)
            {
                if (ExceptionInfo_p.HResult == E_DEVICE_NOT_AVAILABLE)
                {
                    InfoMessageHandler("ERROR: Bluetooth Radio is not on\n");
                }
                else
                {
                    InfoMessageHandler("ERROR: Bluetooth Error while connecting to Device\n");
                }
                return (-2);
            }


            //---------------------------------------------------------------
            // Step(2): Discover all available Servives of BLE Device
            //---------------------------------------------------------------
            iGattServiceCount = 0;
            if (m_ConnectedBleDevice != null)
            {
                // GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
                BleGattServicesRes = await m_ConnectedBleDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                if (BleGattServicesRes.Status == GattCommunicationStatus.Success)
                {
                    m_BleDeviceGattServiceList = BleGattServicesRes.Services;
                    iGattServiceCount = m_BleDeviceGattServiceList.Count;
                }
            }

            return (iGattServiceCount);

        }



        //-------------------------------------------------------------------
        //  BleDeviceDisonnect()
        //-------------------------------------------------------------------
        public  bool  BleDeviceDisconnect (BleDevice Device_p)
        {

            if (m_ConnectedBleDevice != null)
            {
                m_ConnectedBleDevice.Dispose();
            }

            m_ConnectedBleDevice = null;

            return (true);

        }



        //-------------------------------------------------------------------
        //  GetConnectedBleDevice()
        //-------------------------------------------------------------------
        public  BluetoothLEDevice  GetConnectedBleDevice()
        {

            return (m_ConnectedBleDevice);

        }



        //-------------------------------------------------------------------
        //  GetConnectedBleDeviceServices()
        //-------------------------------------------------------------------
        public  IReadOnlyList<GattDeviceService>  GetConnectedBleDeviceServices()
        {

            return (m_BleDeviceGattServiceList);

        }





        //-----------------------------------------------------------------//
        //                                                                 //
        //      P R I V A T E   M E T H O D S                              //
        //                                                                 //
        //-----------------------------------------------------------------//

        //-------------------------------------------------------------------
        //  DeviceWatcher_Added()
        //-------------------------------------------------------------------
        private  void  DeviceWatcher_Added (DeviceWatcher SenderDeviceWatcher_p, DeviceInformation DeviceInfo_p)
        {

            // Protect against race condition if the task runs after the app stopped the deviceWatcher
            if (SenderDeviceWatcher_p == m_DeviceWatcher)
            {
                AddBleDeviceToPool(DeviceInfo_p);
            }

            return;

        }



        //-------------------------------------------------------------------
        //  DeviceWatcher_Updated()
        //-------------------------------------------------------------------
        private  void  DeviceWatcher_Updated (DeviceWatcher SenderDeviceWatcher_p, DeviceInformationUpdate DeviceInfoUpdate_p)
        {

            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (SenderDeviceWatcher_p == m_DeviceWatcher)
            {
                UpdateBleDeviceInPool (DeviceInfoUpdate_p);
            }

            return;

        }



        //-------------------------------------------------------------------
        //  DeviceWatcher_Removed()
        //-------------------------------------------------------------------
        private  void  DeviceWatcher_Removed (DeviceWatcher SenderDeviceWatcher_p, DeviceInformationUpdate DeviceInfoUpdate_p)
        {

            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (SenderDeviceWatcher_p == m_DeviceWatcher)
            {
                DeleteBleDeviceFromPool (DeviceInfoUpdate_p);
            }

            return;

        }



        //-------------------------------------------------------------------
        //  DeviceWatcher_EnumerationCompleted()
        //-------------------------------------------------------------------
        private  void  DeviceWatcher_EnumerationCompleted (DeviceWatcher SenderDeviceWatcher_p, object EventArgs_p)
        {

            int  iBleDeviceCount;

            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (SenderDeviceWatcher_p == m_DeviceWatcher)
            {
                if (m_CbBleDeviceScanCompleted != null)
                {
                    iBleDeviceCount = m_ActiveBleDevicePool.Count;
                    m_CbBleDeviceScanCompleted (iBleDeviceCount);
                }
            }

            return;

        }



        //-------------------------------------------------------------------
        //  DeviceWatcher_Stopped()
        //-------------------------------------------------------------------
        private  void  DeviceWatcher_Stopped (DeviceWatcher SenderDeviceWatcher_p, object EventArgs_p)
        {

            int  iBleDeviceCount;

            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (SenderDeviceWatcher_p == m_DeviceWatcher)
            {
                if (m_CbBleDeviceScanAborted != null)
                {
                    iBleDeviceCount = m_ActiveBleDevicePool.Count;
                    m_CbBleDeviceScanAborted (iBleDeviceCount);
                }
            }

            return;

        }



        //-------------------------------------------------------------------
        //  AddBleDeviceToPool()
        //-------------------------------------------------------------------
        private  void  AddBleDeviceToPool (DeviceInformation DeviceInfo_p)
        {

            BleDevice  Device;
            String     strAddrPrefix = "BluetoothLE#BluetoothLE";
            String     strDeviceId;
            String     strDeviceName;
            String     strDeviceAddress;
            bool       fIsConnected;
            bool       fIsConnectable;


            // get Device Name
            strDeviceName = DeviceInfo_p.Name;

            // get Device ID (extended address identifier) and derive (simplify) pure address from it
            strDeviceId = DeviceInfo_p.Id;
            if ( strDeviceId.StartsWith(strAddrPrefix) )
            {
                strDeviceAddress = strDeviceId.Remove(0, strAddrPrefix.Length);
            }
            else
            {
                strDeviceAddress = strDeviceId;
            }

            // create new Device object
            Device = new BleDevice();
            Device.DeviceId = strDeviceId;
            Device.DeviceName = strDeviceName;
            Device.DeviceAddress = strDeviceAddress;

            // check if Device is connectable
            // yes: insert Device in active device list
            // no:  insert Device in zombie device list
            fIsConnected = (bool?)DeviceInfo_p.Properties["System.Devices.Aep.IsConnected"] == true;
            fIsConnectable = (bool?)DeviceInfo_p.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] == true;
            if (fIsConnected || fIsConnectable)
            {
                InfoMessageHandler(String.Format("Insert new Device to ActiveDevicePool:\nID: {0}\nName: {1}\nIsConnected: {2}\nIsConnectable: {3}\n", strDeviceId, strDeviceName, fIsConnected, fIsConnectable));
                m_ActiveBleDevicePool.Add(Device);

                if (m_CbRegisterBleDevice != null)
                {
                    m_CbRegisterBleDevice(Device, tDeviceOperation.kAddDevice);
                }
            }
            else
            {
                InfoMessageHandler(String.Format("Insert new Device to ZombieDevicePool:\nID: {0}\nName: {1}\nIsConnected: {2}\nIsConnectable: {3}\n", strDeviceId, strDeviceName, fIsConnected, fIsConnectable));
                m_ZombieBleDevicePool.Add(Device);
            }

            return;

        }



        //-------------------------------------------------------------------
        //  UpdateBleDeviceInPool()
        //-------------------------------------------------------------------
        private  void  UpdateBleDeviceInPool (DeviceInformationUpdate DeviceInfoUpdate_p)
        {

            BleDevice  Device;
            bool       fIsConnectable;

            // check if Device in active device list
            Device = m_ActiveBleDevicePool.GetDeviceById(DeviceInfoUpdate_p.Id);
            if (Device == null)
            {
                fIsConnectable = (bool?)DeviceInfoUpdate_p.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] == true;
                if ( !fIsConnectable )
                {
                    // Device is still not connectable -> ignore it
                    return;
                }

                // check if Device in zombie device list
                Device = m_ZombieBleDevicePool.GetDeviceById(DeviceInfoUpdate_p.Id);
                if (Device == null)
                {
                    // unknown Device -> ignore it
                    return;
                }

                // move Device from zombie device list to active device list
                InfoMessageHandler(String.Format("Move updated Device to ActiveDevicePool:\nID: {0}\nIsConnectable: {1}\n", DeviceInfoUpdate_p.Id, fIsConnectable));
                m_ZombieBleDevicePool.Delete(Device);
                m_ActiveBleDevicePool.Add(Device);

                if (m_CbRegisterBleDevice != null)
                {
                    m_CbRegisterBleDevice(Device, tDeviceOperation.kAddDevice);
                }

            }

            return;

        }



        //-------------------------------------------------------------------
        //  DeleteBleDeviceFromPool()
        //-------------------------------------------------------------------
        private  void  DeleteBleDeviceFromPool (DeviceInformationUpdate DeviceInfoUpdate_p)
        {

            string     strDeviceId;
            BleDevice  Device;

            strDeviceId = DeviceInfoUpdate_p.Id;
            InfoMessageHandler(String.Format("Delete Device from DevicePools:\nID: {0}\n", strDeviceId));

            // inform the higher-level logic about removing of Device
            if (m_CbRegisterBleDevice != null)
            {
                Device = m_ActiveBleDevicePool.GetDeviceById(strDeviceId);
                if (Device != null)
                {
                    m_CbRegisterBleDevice(Device, tDeviceOperation.kRemoveDevice);
                }
            }

            // make sure that the Device is no longer present in any list
            m_ActiveBleDevicePool.Delete(strDeviceId);
            m_ZombieBleDevicePool.Delete(strDeviceId);

            return;

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

}



// EOF

