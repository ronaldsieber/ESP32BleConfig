/****************************************************************************

  Copyright (c) 2021 Ronald Sieber

  Project:      Esp32Config - UwpApp
  Description:  Application Main Page

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
using Windows.UI.Popups;
using System.Data;
using System.Collections.ObjectModel;
using Windows.Devices.Bluetooth;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Text;
using Windows.UI.ViewManagement;
using System.Reflection;



namespace Esp32ConfigUwp
{

    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////
    //                                                                     //
    //          C L A S S   MainPage                                       //
    //                                                                     //
    /////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////

    public  sealed  partial  class  MainPage : Page
    {

        //=================================================================//
        //                                                                 //
        //      D A T A   S E C T I O N                                    //
        //                                                                 //
        //=================================================================//

        //-------------------------------------------------------------------
        //  Application Information
        //-------------------------------------------------------------------

        private  const  String                      APP_NAME                    = "Esp32ConfigUwp";
        private  const  uint                        APP_VER_DOMAIN              = 1;                // V1.xx
        private  const  uint                        APP_VER_REVISION            = 0;                // Vx.00



        //-------------------------------------------------------------------
        //  Attributes
        //-------------------------------------------------------------------

        private  BleDeviceManagement                m_BleDeviceManagement       = null;
        private  BleDeviceList                      m_BleDevicePool             = null;
        private  Esp32BleAppCfgProfile              m_Esp32BleAppCfgProfile     = null;
        private  bool                               m_fConnectedToDevice        = false;





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
        //  MainPage()
        //-------------------------------------------------------------------
        public  MainPage()
        {

            BleDeviceManagement.DlgtBleDeviceScanCompleted  CbBleDeviceScanCompleted;
            BleDeviceManagement.DlgtBleDeviceScanAborted    CbBleDeviceScanAborted;
            BleDeviceManagement.DlgtRegisterBleDevice       CbRegisterBleDevice;
            BleDeviceManagement.DlgtInfoMessageHandler      CbInfoMessageHandler;
            String  strBuildTimeStamp;


            this.InitializeComponent();

            // set default MainWindow size
            ApplicationView.PreferredLaunchViewSize = new Size(1024, 768);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            // set initial state of GUI elements
            SetGuiInitState(true);
            SetDeviceValueInitState();
            strBuildTimeStamp = GetBuildTimestamp();
            PrintInfoInLogConsole(String.Format("{0} - Version {1:d}.{2:d02}\n", APP_NAME, APP_VER_DOMAIN, APP_VER_REVISION));
            #if DEBUG
                PrintInfoInLogConsole("[Debug Version]\n");
            #endif
            PrintInfoInLogConsole(strBuildTimeStamp + "\n");
            PrintInfoInLogConsole("Application Ready.\n");

            // create BleDeviceManagement runtime object
            CbBleDeviceScanCompleted = AsyncBleDeviceScanCompleted;
            CbBleDeviceScanAborted   = AsyncBleDeviceScanAborted;
            CbRegisterBleDevice      = AsyncRegisterBleDevice;
            CbInfoMessageHandler     = AsyncInfoMessageHandler;
            m_BleDeviceManagement = new BleDeviceManagement(CbBleDeviceScanCompleted, CbBleDeviceScanAborted, CbRegisterBleDevice, CbInfoMessageHandler);

            // create DeviceDataGrid including associated DeviceList
            m_BleDevicePool = new BleDeviceList();
            m_DataGrid_BleDevicePool.ItemsSource = m_BleDevicePool;
            m_DataGrid_BleDevicePool.SelectionChanged += OnSelectionChanged_DataGridBleDevicePool;

            return;

        }



        //-------------------------------------------------------------------
        //  OnClick_Btn_BleScanStart()
        //-------------------------------------------------------------------
        private  async  void  OnClick_Btn_BleScanStart (object Sender_p, RoutedEventArgs EventArgs_p)
        {

            MessageDialog  Dialog;
            int            iBleDeviceStatus;
            bool           fResult;

            PrintInfoInLogConsole("\nStart BLE Scan...\n");

            // clear profile
            m_Esp32BleAppCfgProfile = null;

            // start with an empty device list
            m_BleDevicePool.Clear();

            // check if Bluetooth adapter is available and active
            iBleDeviceStatus = await BleDeviceManagement.IsBleEnabled();
            switch (iBleDeviceStatus)
            {
                case 1:
                {
                    // everything great -> Bluetooth adapter available and active
                    break;
                }
                case 0:
                {
                    // Bluetooth adapter found, but radio is tured off
                    PrintInfoInLogConsole("\nERROR: Bluetooth Radio is turned OFF!\n");
                    Dialog = new MessageDialog("ERROR: Bluetooth Radio is turned OFF", "Bluetooth Error");
                    await Dialog.ShowAsync();
                    break;
                }
                case -1:
                {
                    // no Bluetooth device found
                    PrintInfoInLogConsole("\nERROR: No Bluetooth Adapter found!\n");
                    Dialog = new MessageDialog("ERROR: No Bluetooth Adapter found!", "Bluetooth Error");
                    await Dialog.ShowAsync();
                    break;
                }
            }

            // start BLE Device scan
            fResult = m_BleDeviceManagement.BleScanStart();
            if ( fResult )
            {
                m_Btn_BleScanStart.IsEnabled = false;
                m_Btn_BleScanStop.IsEnabled = true;
            }

            return;

        }



        //-------------------------------------------------------------------
        //  OnClick_Btn_BleScanStop()
        //-------------------------------------------------------------------
        private  void  OnClick_Btn_BleScanStop (object Sender_p, RoutedEventArgs EventArgs_p)
        {

            bool  fResult;

            PrintInfoInLogConsole("\nStop BLE Scan...\n");

            fResult = m_BleDeviceManagement.BleScanStop();
            if ( fResult )
            {
                m_Btn_BleScanStart.IsEnabled = true;
                m_Btn_BleScanStop.IsEnabled = false;
            }

            return;

        }



        //-------------------------------------------------------------------
        //  OnSelectionChanged_DataGridBleDevicePool()
        //-------------------------------------------------------------------
        private  void  OnSelectionChanged_DataGridBleDevicePool (object Sender_p, SelectionChangedEventArgs EventArgs_p)
        {

            Microsoft.Toolkit.Uwp.UI.Controls.DataGrid  DataGrid_BleDevicePool;
            BleDevice  Device;
            int        iSelectedIdx;

            // get index of selected grid row
            DataGrid_BleDevicePool = (Microsoft.Toolkit.Uwp.UI.Controls.DataGrid)Sender_p;
            iSelectedIdx = DataGrid_BleDevicePool.SelectedIndex;
            if (iSelectedIdx >= 0)
            {
                Device = m_BleDevicePool[iSelectedIdx];
                m_TxtBlck_SelectedDevice.Text = Device.DeviceName;
                PrintInfoInLogConsole(String.Format("Select Device '{0}' ({1})\n", Device.DeviceName, Device.DeviceAddress));
                m_Btn_ConnectDevice.IsEnabled = true;
            }
            else
            {
                m_Btn_ConnectDevice.IsEnabled = false;
            }

            return;

        }



        //-------------------------------------------------------------------
        //  OnClick_Btn_ConnectDevice()
        //-------------------------------------------------------------------
        private  async  void  OnClick_Btn_ConnectDevice (object Sender_p, RoutedEventArgs EventArgs_p)
        {

            MessageDialog  Dialog;

            //---------------------------------------------------------------
            //  Connect to selected Device
            //---------------------------------------------------------------
            if ( !m_fConnectedToDevice )
            {
                m_Btn_ConnectDevice.IsEnabled = false;

                try
                {
                    // connect to device
                    await OnEventConnectDevice(Sender_p, EventArgs_p);
                }
                catch
                {
                    Dialog = new MessageDialog("ERROR: Connecting to Device failed!", "BLE Connection Error");
                    await Dialog.ShowAsync();

                    m_Btn_ConnectDevice.IsEnabled = true;
                    return;
                }

                m_Btn_ConnectDevice.IsEnabled = true;
                m_Btn_ConnectDevice.Content = "Disconnect";
                m_fConnectedToDevice = true;
            }

            //---------------------------------------------------------------
            //  Disconnect from selected Device
            //---------------------------------------------------------------
            else
            {
                m_Btn_ConnectDevice.IsEnabled = false;

                try
                {
                    // disconnect from device
                    await OnEventDisconnectDevice(Sender_p, EventArgs_p);
                }
                catch
                {
                    Dialog = new MessageDialog("ERROR: Disconnecting from Device failed!", "BLE Connection Error");
                    await Dialog.ShowAsync();

                    m_Btn_ConnectDevice.IsEnabled = true;
                    return;
                }

                m_Btn_ConnectDevice.IsEnabled = true;
                m_Btn_ConnectDevice.Content = "Connect";
                m_fConnectedToDevice = false;
            }


            return;

        }



        //-------------------------------------------------------------------
        //  OnClick_Btn_RefreshSysTickCnt()
        //-------------------------------------------------------------------
        private  async  void  OnClick_Btn_RefreshSysTickCnt (object Sender_p, RoutedEventArgs EventArgs_p)
        {

            UInt32  ui32SysTickCnt;
            String  strSysTickCnt;

            if (m_Esp32BleAppCfgProfile != null)
            {
                PrintInfoInLogConsole("Refresh SysTickCnt... ");
                ui32SysTickCnt = await m_Esp32BleAppCfgProfile.GetCurrentSysTickCnt();
                PrintInfoInLogConsole("done.\n");

                strSysTickCnt = BleUtils.FormatSystemTickCount(ui32SysTickCnt);
                m_TxtBlck_SysTickCnt.Text = strSysTickCnt;
            }

            return;

        }



        //-------------------------------------------------------------------
        //  OnClick_Btn_SaveCfg()
        //-------------------------------------------------------------------
        private  async  void  OnClick_Btn_SaveCfg (object Sender_p, RoutedEventArgs EventArgs_p)
        {

            Esp32BleAppCfgData  AppCfgData;

            //---------------------------------------------------------------
            // Step(1): Get values to write from GUI elements
            //---------------------------------------------------------------
            AppCfgData = new Esp32BleAppCfgData();

            AppCfgData.m_strDevName  = m_TxtBox_DevName.Text;

            AppCfgData.m_strSSID     = m_TxtBox_SSID.Text;
            AppCfgData.m_strPasswd   = m_TxtBox_Passwd.Text;
            AppCfgData.m_strOwnAddr  = m_TxtBox_OwnAddr.Text;
            AppCfgData.m_ui16OwnMode = CmbBoxOwnModeQuerySelection();

            AppCfgData.m_ui16Opt1 = (UInt16) ((m_ChckBox_AppRtCfgOpt1.IsChecked == true) ? 1 : 0);
            AppCfgData.m_ui16Opt2 = (UInt16) ((m_ChckBox_AppRtCfgOpt2.IsChecked == true) ? 1 : 0);
            AppCfgData.m_ui16Opt3 = (UInt16) ((m_ChckBox_AppRtCfgOpt3.IsChecked == true) ? 1 : 0);
            AppCfgData.m_ui16Opt4 = (UInt16) ((m_ChckBox_AppRtCfgOpt4.IsChecked == true) ? 1 : 0);
            AppCfgData.m_ui16Opt5 = (UInt16) ((m_ChckBox_AppRtCfgOpt5.IsChecked == true) ? 1 : 0);
            AppCfgData.m_ui16Opt6 = (UInt16) ((m_ChckBox_AppRtCfgOpt6.IsChecked == true) ? 1 : 0);
            AppCfgData.m_ui16Opt7 = (UInt16) ((m_ChckBox_AppRtCfgOpt7.IsChecked == true) ? 1 : 0);
            AppCfgData.m_ui16Opt8 = (UInt16) ((m_ChckBox_AppRtCfgOpt8.IsChecked == true) ? 1 : 0);
            AppCfgData.m_strPeerAddr = m_TxtBox_AppRtCfgPeerAddr.Text;

            m_Esp32BleAppCfgProfile.SetAppCfgData(AppCfgData);


            //---------------------------------------------------------------
            // Step(2): Save values to Device
            //---------------------------------------------------------------
            PrintInfoInLogConsole("Save values to Device... ");
            await m_Esp32BleAppCfgProfile.SaveConfig();
            PrintInfoInLogConsole("done.\n");

            return;

        }



        //-------------------------------------------------------------------
        //  OnClick_Btn_RstDev()
        //-------------------------------------------------------------------
        private  async  void  OnClick_Btn_RstDev (object sender, RoutedEventArgs EventArgs_p)
        {

            PrintInfoInLogConsole("Restart Device... ");
            await m_Esp32BleAppCfgProfile.RestartDevice();
            PrintInfoInLogConsole("done.\n");

            SetGuiInitState(false);
            SetDeviceValueInitState();

            return;

        }





        //-----------------------------------------------------------------//
        //                                                                 //
        //      C A L L B A C K   M E T H O D E S                          //
        //                                                                 //
        //-----------------------------------------------------------------//

        //-------------------------------------------------------------------
        //  AsyncBleDeviceScanCompleted()
        //-------------------------------------------------------------------
        private  async  void  AsyncBleDeviceScanCompleted (int iBleDeviceCount_p)
        {

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    PrintInfoInLogConsole(String.Format("BLE Device Scan completetd\nDevices found: {0}\n", iBleDeviceCount_p));

                    m_Btn_BleScanStart.IsEnabled = true;
                    m_Btn_BleScanStop.IsEnabled = false;
                }
            });

            return;

        }



        //-------------------------------------------------------------------
        //  AsyncBleDeviceScanAborted()
        //-------------------------------------------------------------------
        private  async  void  AsyncBleDeviceScanAborted (int iBleDeviceCount_p)
        {

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    PrintInfoInLogConsole(String.Format("BLE Device Scan aborted\nDevices found: {0}\n", iBleDeviceCount_p));

                    m_Btn_BleScanStart.IsEnabled = true;
                    m_Btn_BleScanStop.IsEnabled = false;
                }
            });

            return;

        }



        //-------------------------------------------------------------------
        //  AsyncRegisterBleDevice()
        //-------------------------------------------------------------------
        private  async  void  AsyncRegisterBleDevice (BleDevice Device_p, BleDeviceManagement.tDeviceOperation DeviceOperation_p)
        {

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    if (Device_p != null)
                    {
                        switch (DeviceOperation_p)
                        {
                            case BleDeviceManagement.tDeviceOperation.kAddDevice:
                            {
                                m_BleDevicePool.Add(Device_p);
                                break;
                            }

                            case BleDeviceManagement.tDeviceOperation.kRemoveDevice:
                            {
                                m_BleDevicePool.Delete(Device_p);
                                break;
                            }
                        }
                    }
                }
            });

            return;

        }



        //-------------------------------------------------------------------
        //  AsyncInfoMessageHandler()
        //-------------------------------------------------------------------
        private  async  void  AsyncInfoMessageHandler (String strInfo_p)
        {

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    PrintInfoInLogConsole(strInfo_p);
                }
            });

            return;

        }





        //-----------------------------------------------------------------//
        //                                                                 //
        //      P R I V A T E   M E T H O D S                              //
        //                                                                 //
        //-----------------------------------------------------------------//

        //-------------------------------------------------------------------
        //  OnEventConnectDevice()
        //-------------------------------------------------------------------
        private  async  Task<bool>  OnEventConnectDevice (object Sender_p, RoutedEventArgs EventArgs_p)
        {

            Esp32BleAppCfgData  AppCfgData;
            string              strDeviceName;
            BleDevice           Device;
            bool                fProfileValid;


            //---------------------------------------------------------------
            // Step(1): Connect to selected Device
            //---------------------------------------------------------------
            // cancel a possibly still running device scan
            OnClick_Btn_BleScanStop(Sender_p, EventArgs_p);
            await Task.Delay(TimeSpan.FromMilliseconds(1000));      // sleep until cancelling finsished

            // get Device from active device list
            strDeviceName = m_TxtBlck_SelectedDevice.Text;
            Device = m_BleDevicePool.GetDeviceByName(strDeviceName);
            if (Device == null)
            {
                PrintInfoInLogConsole("ERROR: Can't find Device\n");
                return (false);
            }

            // connect to selected Device
            PrintInfoInLogConsole(String.Format("Connect to Device '{0}'...\n", Device.DeviceName));
            fProfileValid = await ConnectBleDevice(Device);
            if ( !fProfileValid )
            {
                PrintInfoInLogConsole("ERROR: Can't connect to Device\n");
                return (false);
            }


            //---------------------------------------------------------------
            // Step(2): Set read values from Device on GUI elements
            //---------------------------------------------------------------
            AppCfgData = m_Esp32BleAppCfgProfile.GetAppCfgData();

            m_TxtBlck_DevType.Text = AppCfgData.m_ui32DevType.ToString();
            m_TxtBlck_SysTickCnt.Text = BleUtils.FormatSystemTickCount(AppCfgData.m_ui32SysTickCnt);
            m_TxtBox_DevName.Text = AppCfgData.m_strDevName;

            m_TxtBox_SSID.Text = AppCfgData.m_strSSID;
            m_TxtBox_Passwd.Text = AppCfgData.m_strPasswd;
            m_TxtBox_OwnAddr.Text = AppCfgData.m_strOwnAddr;
            CmbBoxOwnModePopulate(AppCfgData.m_ui16OwnModeFeatList, AppCfgData.m_ui16OwnMode);

            m_ChckBox_AppRtCfgOpt1.IsChecked = (bool) (AppCfgData.m_ui16Opt1 != 0);
            m_ChckBox_AppRtCfgOpt2.IsChecked = (bool) (AppCfgData.m_ui16Opt2 != 0);
            m_ChckBox_AppRtCfgOpt3.IsChecked = (bool) (AppCfgData.m_ui16Opt3 != 0);
            m_ChckBox_AppRtCfgOpt4.IsChecked = (bool) (AppCfgData.m_ui16Opt4 != 0);
            m_ChckBox_AppRtCfgOpt5.IsChecked = (bool) (AppCfgData.m_ui16Opt5 != 0);
            m_ChckBox_AppRtCfgOpt6.IsChecked = (bool) (AppCfgData.m_ui16Opt6 != 0);
            m_ChckBox_AppRtCfgOpt7.IsChecked = (bool) (AppCfgData.m_ui16Opt7 != 0);
            m_ChckBox_AppRtCfgOpt8.IsChecked = (bool) (AppCfgData.m_ui16Opt8 != 0);
            m_TxtBox_AppRtCfgPeerAddr.Text = AppCfgData.m_strPeerAddr;

            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt1, AppCfgData.m_strLabelOpt1);
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt2, AppCfgData.m_strLabelOpt2);
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt3, AppCfgData.m_strLabelOpt3);
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt4, AppCfgData.m_strLabelOpt4);
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt5, AppCfgData.m_strLabelOpt5);
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt6, AppCfgData.m_strLabelOpt6);
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt7, AppCfgData.m_strLabelOpt7);
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt8, AppCfgData.m_strLabelOpt8);
            CustomizeOptionControl(m_TxtBlck_PeerAddress, AppCfgData.m_strLabelPeerAddr, m_TxtBox_AppRtCfgPeerAddr);


            //---------------------------------------------------------------
            // Step(3): Set enable state of GUI elements
            //---------------------------------------------------------------
            m_Btn_RefreshSysTickCnt.IsEnabled = true;
            m_Btn_SaveCfg.IsEnabled = true;
            m_Btn_RstDev.IsEnabled = true;

            return (true);

        }



        //-------------------------------------------------------------------
        //  OnEventDisconnectDevice()
        //-------------------------------------------------------------------
        private  async Task<bool>  OnEventDisconnectDevice (object Sender_p, RoutedEventArgs EventArgs_p)
        {

            string     strDeviceName;
            BleDevice  Device;


            //---------------------------------------------------------------
            // Step(1): Disconnect from selected Device
            //---------------------------------------------------------------
            // get Device from active device list
            strDeviceName = m_TxtBlck_SelectedDevice.Text;
            Device = m_BleDevicePool.GetDeviceByName(strDeviceName);
            if (Device == null)
            {
                PrintInfoInLogConsole("ERROR: Can't find Device\n");
                return (false);
            }

            // disconnect from selected Device
            PrintInfoInLogConsole(String.Format("Disconnect from Device '{0}'...\n", Device.DeviceName));
            DisconnectBleDevice(Device);


            //---------------------------------------------------------------
            // Step(2): Clear read values from Device on GUI elements
            //---------------------------------------------------------------
            SetDeviceValueInitState();


            //---------------------------------------------------------------
            // Step(3): Set enable state of GUI elements
            //---------------------------------------------------------------
            SetGuiInitState(false);

            
            //---------------------------------------------------------------
            // Step(4): Set application default state
            //---------------------------------------------------------------
            // await until background proesses has been finished
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            PrintInfoInLogConsole("Disconnect done.\n");

            // set focus back to DeviceDataGrid
            m_DataGrid_BleDevicePool.Focus(FocusState.Programmatic);
            m_DataGrid_BleDevicePool.SelectedIndex = 0;

            return (true);

        }



        //-------------------------------------------------------------------
        //  ConnectBleDevice()
        //-------------------------------------------------------------------
        private  async  Task<bool>  ConnectBleDevice (BleDevice Device_p)
        {

            Esp32BleAppCfgProfile.DlgtInfoMessageHandler  CbInfoMessageHandler;
            IReadOnlyList<GattDeviceService>              BleDeviceGattServiceList;
            bool  fProfileValid;
            int   iResult;

            // connect to BLE Device
            iResult = await m_BleDeviceManagement.BleDeviceConnect(Device_p);
            if (iResult < 0)
            {
                return (false);
            }

            // establish Esp32-specific BLE Profile
            CbInfoMessageHandler = AsyncInfoMessageHandler;
            BleDeviceGattServiceList = m_BleDeviceManagement.GetConnectedBleDeviceServices();
            m_Esp32BleAppCfgProfile = new Esp32BleAppCfgProfile(BleDeviceGattServiceList, CbInfoMessageHandler);
            fProfileValid = await m_Esp32BleAppCfgProfile.EstablishProfile();

            return (fProfileValid);

        }



        //-------------------------------------------------------------------
        //  DisconnectBleDevice()
        //-------------------------------------------------------------------
        private  void  DisconnectBleDevice (BleDevice Device_p)
        {

            // dispose Esp32-specific BLE Profile
            if (m_Esp32BleAppCfgProfile != null)
            {
                m_Esp32BleAppCfgProfile.DisposeProfile();
            }

            // disconnect from BLE Device
            if (m_BleDeviceManagement != null)
            {
                m_BleDeviceManagement.BleDeviceDisconnect(Device_p);
            }

            return;

        }



        //-------------------------------------------------------------------
        //  SetGuiInitState()
        //-------------------------------------------------------------------
        private  void  SetGuiInitState(bool fClrLogConsole_p)
        {

            if ( fClrLogConsole_p )
            {
                m_TxtBox_LogConsole.Text = "";
            }

            m_Btn_BleScanStart.IsEnabled = true;
            m_Btn_BleScanStop.IsEnabled = false;

            m_TxtBlck_SelectedDevice.Text = "";

            m_fConnectedToDevice = false;
            m_Btn_ConnectDevice.Content = "Connect";
            m_Btn_ConnectDevice.IsEnabled = false;

            m_Btn_RefreshSysTickCnt.IsEnabled = false;
            m_Btn_SaveCfg.IsEnabled = false;
            m_Btn_RstDev.IsEnabled = false;

            m_DataGrid_BleDevicePool.SelectedItem = null;

            return;

        }



        //-------------------------------------------------------------------
        //  SetDeviceValueInitState()
        //-------------------------------------------------------------------
        private  void  SetDeviceValueInitState()
        {

            m_TxtBlck_DevType.Text = "?";
            m_TxtBlck_SysTickCnt.Text = "?";
            m_TxtBox_DevName.Text = "";

            m_TxtBox_SSID.Text = "";
            m_TxtBox_Passwd.Text = "";
            m_TxtBox_OwnAddr.Text = "";
            m_CmbBox_OwnMode.Items.Clear();                         // empty/clear ComboBox (remove all items)

            m_ChckBox_AppRtCfgOpt1.IsChecked = false;
            m_ChckBox_AppRtCfgOpt2.IsChecked = false;
            m_ChckBox_AppRtCfgOpt3.IsChecked = false;
            m_ChckBox_AppRtCfgOpt4.IsChecked = false;
            m_ChckBox_AppRtCfgOpt5.IsChecked = false;
            m_ChckBox_AppRtCfgOpt6.IsChecked = false;
            m_ChckBox_AppRtCfgOpt7.IsChecked = false;
            m_ChckBox_AppRtCfgOpt8.IsChecked = false;
            m_TxtBox_AppRtCfgPeerAddr.Text = "";

            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt1, "Runtime Option #1");
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt2, "Runtime Option #2");
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt3, "Runtime Option #3");
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt4, "Runtime Option #4");
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt5, "Runtime Option #5");
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt6, "Runtime Option #6");
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt7, "Runtime Option #7");
            CustomizeOptionControl(m_ChckBox_AppRtCfgOpt8, "Runtime Option #8");
            CustomizeOptionControl(m_TxtBlck_PeerAddress,  "Peer Address:", m_TxtBox_AppRtCfgPeerAddr);

            return;

        }



        //-------------------------------------------------------------------
        //  CmbBoxOwnModePopulate()
        //-------------------------------------------------------------------
        private  void  CmbBoxOwnModePopulate(UInt16 ui16OwnModeFeatList_p, UInt16 ui16OwnMode_p)
        {

            // Definitions of WIFI Operation Modes
            const int WIFI_MODE_STA = (1<<0);                       // WIFI Station/Client Mode
            const int WIFI_MODE_AP  = (1<<1);                       // WIFI AccessPoint Mode

            ComboBoxItem  CmbBoxItem;

            m_CmbBox_OwnMode.Items.Clear();                         // empty/clear ComboBox (remove all items)

            if ((ui16OwnModeFeatList_p & WIFI_MODE_STA) != 0)
            {
                CmbBoxItem = new ComboBoxItem() { Content = "Station (Client)", Tag = WIFI_MODE_STA };
                if ((ui16OwnMode_p & WIFI_MODE_STA) != 0)
                {
                    CmbBoxItem.IsSelected = true;
                }
                m_CmbBox_OwnMode.Items.Add(CmbBoxItem);
            }

            if ((ui16OwnModeFeatList_p & WIFI_MODE_AP) != 0)
            {
                CmbBoxItem = new ComboBoxItem() { Content = "Access Point", Tag = WIFI_MODE_AP };
                if ((ui16OwnMode_p & WIFI_MODE_AP) != 0)
                {
                    CmbBoxItem.IsSelected = true;
                }
                m_CmbBox_OwnMode.Items.Add(CmbBoxItem);
            }

            return;

        }



        //-------------------------------------------------------------------
        //  CmbBoxOwnModeQuerySelection()
        //-------------------------------------------------------------------
        private  UInt16  CmbBoxOwnModeQuerySelection()
        {

            ComboBoxItem  CmbBoxItem;
            UInt16        ui16OwnMode;

            ui16OwnMode = 0;

            CmbBoxItem = (ComboBoxItem) m_CmbBox_OwnMode.SelectedItem;
            if (CmbBoxItem != null)
            {
                ui16OwnMode = (UInt16)((int)CmbBoxItem.Tag);
            }

            return (ui16OwnMode);

        }



        //-------------------------------------------------------------------
        //  CustomizeOptionControl()
        //-------------------------------------------------------------------
        private  void  CustomizeOptionControl(CheckBox OptionControl_p, string strLabel_p)
        {

            bool  fEnableState;

            if (strLabel_p == null)
            {
                return;
            }

            fEnableState = !strLabel_p.StartsWith("#");
            if ( !fEnableState )
            {
                strLabel_p = strLabel_p.Remove(0,1);
                strLabel_p = strLabel_p.Trim();
            }

            OptionControl_p.Content = strLabel_p;
            OptionControl_p.IsEnabled = fEnableState;

            return;

        }
        //-------------------------------------------------------------------
        private  void  CustomizeOptionControl(TextBlock OptionControlLabel_p, string strLabel_p, TextBox OptionControlValue_p)
        {

            bool  fEnableState;

            if (strLabel_p == null)
            {
                return;
            }

            fEnableState = !strLabel_p.StartsWith("#");
            if ( !fEnableState )
            {
                strLabel_p = strLabel_p.Remove(0,1);
                strLabel_p = strLabel_p.Trim();
            }

            if ( !strLabel_p.EndsWith(":") )
            {
                strLabel_p += ":";
            }

            OptionControlLabel_p.Text = strLabel_p;
            OptionControlValue_p.IsEnabled = fEnableState;
            
            return;

        }



        //-------------------------------------------------------------------
        //  PrintInfoInLogConsole()
        //-------------------------------------------------------------------
        private  void  PrintInfoInLogConsole (String strInfo_p)
        {

            m_TxtBox_LogConsole.Text += strInfo_p;
            TextBoxScrollToBottom(m_TxtBox_LogConsole);

            return;

        }



        //-------------------------------------------------------------------
        //  TextBoxScrollToBottom()
        //-------------------------------------------------------------------
        //  Source Origin: https://stackoverflow.com/questions/40114620/uwp-c-sharp-scroll-to-the-bottom-of-textbox
        //-------------------------------------------------------------------
        private void TextBoxScrollToBottom (TextBox TextBox_p)
        {

            DependencyObject  ChildObjectGrid;
            DependencyObject  ChildObject;
            int               iIdx;

            ChildObjectGrid = (Grid)VisualTreeHelper.GetChild(TextBox_p, 0);
            if (ChildObjectGrid == null)
            {
                return;
            }

            for (iIdx=0; iIdx<=VisualTreeHelper.GetChildrenCount(ChildObjectGrid)-1; iIdx++)
            {
                ChildObject = VisualTreeHelper.GetChild(ChildObjectGrid, iIdx);
                if ( !(ChildObject is ScrollViewer) )
                {
                    continue;
                }

                ((ScrollViewer)ChildObject).ChangeView(0.0f, ((ScrollViewer)ChildObject).ExtentHeight, 1.0f, true);
                break;
            }

            return;

        }



        //-------------------------------------------------------------------
        //  GetBuildTimestamp()
        //-------------------------------------------------------------------
        //
        //  Requirements:
        //  -------------
        //
        //  (1) PreBuildStep:
        //          echo %date% > "$(ProjectDir)BuildTimeStamp.txt"
        //          echo %time% >> "$(ProjectDir)BuildTimeStamp.txt"
        //
        //  (2) BuildTimeStamp.txt Property:
        //          Embedded Resource
        //
        //-------------------------------------------------------------------

        public  static  String  GetBuildTimestamp()
        {

            Assembly      RuntimeAssembly;
            Stream        strmRessource;
            StreamReader  strmrdrRessource;
            String        strResourceBuildTimeStamp;
            String[]      astrResourceBuildTimeStamp;
            String[]      astrResourceDate;
            String[]      astrResourceTime;
            uint          uiDay;
            uint          uiMonth;
            uint          uiYear;
            uint          uiHour;
            uint          uiMinute;
            uint          uiSecond;
            String        strBuildTimeStamp;


            try
            {
                RuntimeAssembly = Assembly.GetEntryAssembly();

                strmRessource = RuntimeAssembly.GetManifestResourceStream("Esp32ConfigUwp.BuildTimeStamp.txt");
                strmrdrRessource = new StreamReader(strmRessource);

                strResourceBuildTimeStamp = strmrdrRessource.ReadToEnd();
                astrResourceBuildTimeStamp = strResourceBuildTimeStamp.Split('\n');                     // split into Date and Time

                astrResourceBuildTimeStamp[0] = astrResourceBuildTimeStamp[0].Trim();                   // [0] -> Date
                astrResourceBuildTimeStamp[1] = astrResourceBuildTimeStamp[1].Trim();                   // [1] -> Time
                
                astrResourceDate = astrResourceBuildTimeStamp[0].Split('.');                            // split Date into [dd] [mm] [YYYY]
                uint.TryParse(astrResourceDate[0], out uiDay);
                uint.TryParse(astrResourceDate[1], out uiMonth);
                uint.TryParse(astrResourceDate[2], out uiYear);

                astrResourceTime = astrResourceBuildTimeStamp[1].Split(':');                            // split Time into [HH] [MM] [SS,ms]
                astrResourceTime[2] = astrResourceTime[2].Substring(0, astrResourceTime[2].Length-3);   // remove Millis from Seconds
                uint.TryParse(astrResourceTime[0], out uiHour);
                uint.TryParse(astrResourceTime[1], out uiMinute);
                uint.TryParse(astrResourceTime[2], out uiSecond);

                strBuildTimeStamp = String.Format("{0:d4}-{1:d02}-{2:d02} / {3:d02}:{4:d02}:{5:d02}", uiYear, uiMonth, uiDay, uiHour, uiMinute, uiSecond);
            }
            catch
            {
                strBuildTimeStamp = "???";
            }

            return (strBuildTimeStamp);

        }

    }

}



// EOF
