/****************************************************************************

  Copyright (c) 2021 Ronald Sieber

  Project:      Project independend / Standard class
  Description:  Class <ESP32BleCfgProfile> Implementation

  -------------------------------------------------------------------------

    Calculation of Handles required for function <BLEServer::createService>:
        1 Handle for the Service itself
      + 2 Handles for each Characteristic
      + 1 Handle for each Despcription

    (see: https://esp32.com/viewtopic.php?t=7452)

  -------------------------------------------------------------------------

  Revision History:

  2021/07/06 -rs:   V1.00 Initial version

****************************************************************************/


#include "Arduino.h"
#include <BLEDevice.h>
#include <BLEServer.h>
#include "ESP32BleCfgProfile.h"

#define DEBUG                                                           // Enable/Disable TRACE
#include "Trace.h"





/***************************************************************************/
/*                                                                         */
/*                                                                         */
/*          CLASS  ESP32BleCfgProfile                                      */
/*                                                                         */
/*                                                                         */
/***************************************************************************/

/////////////////////////////////////////////////////////////////////////////
//                                                                         //
//          P R I V A T E   A T T R I B U T E S                            //
//                                                                         //
/////////////////////////////////////////////////////////////////////////////

//---------------------------------------------------------------------------
//  BLE specific Definitions
//---------------------------------------------------------------------------
//
// CONVENTION:
//
// In a GUID of a Characteristic the 1st group specifies the Characteristic purpose,
// while the 2nd group is always '0000'. A Descriptor uses in its 1st group the same
// value as it's assoziated Characteristic, but with a value unequal '0000' in the
// 2nd group. For the first Descriptor the value '0001' is used. This scheme allows
// multiple Descriptors for one Characteristic in the future by continue numbering
// with '0002' etc. However, this implementation always uses only the first descriptor
// ('001').
//
// Sample:
// GUID Characteristic:         "00003100-0000-1000-8000-E776CC14FE69"
// GUID Offset Descriptor:      "00000000-0001-0000-0000-000000000000"
//                              --------------------------------------
// Resulting Descriptor GUID:   "00003100-0001-1000-8000-E776CC14FE69"
//

static  const int    NUM_HANDLES_DEVMNT_SERVICE             = 16;               // = (1*Service + 2*Characteristics + 1*Descriptions)
static  const char*  BLE_UUID_DEVMNT_SERVICE                = "00001000-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_DEVMNT_DEVTYPE_CHARACTRSTC    = "00001100-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_DEVMNT_DEVTYPE_DSCRPT         = "00001100-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_DEVMNT_SYSTICKCNT_CHARACTRSTC = "00001200-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_DEVMNT_SYSTICKCNT_DSCRPT      = "00001200-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_DEVMNT_DEVNAME_CHARACTRSTC    = "00001300-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_DEVMNT_DEVNAME_DSCRPT         = "00001300-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_DEVMNT_SAVE_CFG_CHARACTRSTC   = "00001400-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_DEVMNT_SAVE_CFG_DSCRPT        = "00001400-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_DEVMNT_RST_DEV_CHARACTRSTC    = "00001500-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_DEVMNT_RST_DEV_DSCRPT         = "00001500-0001-1000-8000-E776CC14FE69";

static  const int    NUM_HANDLES_WIFI_SERVICE               = 14;               // = (1*Service + 2*Characteristics + 1*Descriptions)
static  const char*  BLE_UUID_WIFI_SERVICE                  = "00002000-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_WIFI_SSID_CHARACTRSTC         = "00002100-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_WIFI_SSID_DSCRPT              = "00002100-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_WIFI_PASSWD_CHARACTRSTC       = "00002200-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_WIFI_PASSWD_DSCRPT            = "00002200-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_WIFI_OWNADDR_CHARACTRSTC      = "00002300-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_WIFI_OWNADDR_DSCRPT           = "00002300-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_WIFI_OWNMODE_CHARACTRSTC      = "00002400-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_WIFI_OWNMODE_DSCRPT           = "00002400-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_WIFI_OWNMODE_DSCRPT_FEATLIST  = "00002400-0002-1000-8000-E776CC14FE69";

static  const int    NUM_HANDLES_APP_RT_SERVICE             = 28;               // = (1*Service + 2*Characteristics + 1*Descriptions)
static  const char*  BLE_UUID_APP_RT_SERVICE                = "00003000-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT1_CHARACTRSTC       = "00003100-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT1_DSCRPT            = "00003100-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT2_CHARACTRSTC       = "00003200-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT2_DSCRPT            = "00003200-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT3_CHARACTRSTC       = "00003300-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT3_DSCRPT            = "00003300-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT4_CHARACTRSTC       = "00003400-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT4_DSCRPT            = "00003400-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT5_CHARACTRSTC       = "00003500-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT5_DSCRPT            = "00003500-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT6_CHARACTRSTC       = "00003600-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT6_DSCRPT            = "00003600-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT7_CHARACTRSTC       = "00003700-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT7_DSCRPT            = "00003700-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT8_CHARACTRSTC       = "00003800-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_OPT8_DSCRPT            = "00003800-0001-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_PEERADDR_CHARACTRSTC   = "00003900-0000-1000-8000-E776CC14FE69";
static  const char*  BLE_UUID_APP_RT_PEERADDR_DSCRPT        = "00003900-0001-1000-8000-E776CC14FE69";



//---------------------------------------------------------------------------
//  Module Local Variables
//---------------------------------------------------------------------------

// The main class ESP32BleCfgProfile and the local callback classes declared
// below require common access to the variables created here. For this reason,
// the variables cannot be declared as members of the class ESP32BleCfgProfile,
// but must be created as module local variables.

static  bool                fBleClientConnected_g           = false;

static  tCbHdlrSaveConfig   pfnAppCbHdlrSaveConfig_g        = NULL;
static  tCbHdlrRestartDev   pfnAppCbHdlrRestartDev_g        = NULL;
static  tCbHdlrConStatChg   pfnAppCbHdlrConStatChg_g        = NULL;

static  BLEServer*          pBleServer_g                    = NULL;

static  BLEService*         pBleServiceDevMnt_g             = NULL;
static  BLECharacteristic*  pBleCharacDevMntDevType_g       = NULL;
static  BLECharacteristic*  pBleCharacDevMntSysTickCnt_g    = NULL;
static  BLECharacteristic*  pBleCharacDevMntDevName_g       = NULL;
static  BLECharacteristic*  pBleCharacDevMntSaveCfg_g       = NULL;
static  BLECharacteristic*  pBleCharacDevMntRstDev_g        = NULL;

static  BLEService*         pBleServiceWifi_g               = NULL;
static  BLECharacteristic*  pBleCharacWifiSSID_g            = NULL;
static  BLECharacteristic*  pBleCharacWifiPasswd_g          = NULL;
static  BLECharacteristic*  pBleCharacWifiOwnAddr_g         = NULL;
static  BLECharacteristic*  pBleCharacWifiOwnMode_g         = NULL;

static  BLEService*         pBleServiceAppRt_g              = NULL;
static  BLECharacteristic*  pBleCharacAppRtOpt1_g           = NULL;
static  BLECharacteristic*  pBleCharacAppRtOpt2_g           = NULL;
static  BLECharacteristic*  pBleCharacAppRtOpt3_g           = NULL;
static  BLECharacteristic*  pBleCharacAppRtOpt4_g           = NULL;
static  BLECharacteristic*  pBleCharacAppRtOpt5_g           = NULL;
static  BLECharacteristic*  pBleCharacAppRtOpt6_g           = NULL;
static  BLECharacteristic*  pBleCharacAppRtOpt7_g           = NULL;
static  BLECharacteristic*  pBleCharacAppRtOpt8_g           = NULL;
static  BLECharacteristic*  pBleCharacAppRtPeerAddr_g       = NULL;


static  uint32_t            ui32DevMntDevType_g             = 0;
static  uint32_t            ui32DevMntSysTickCnt_g          = 0;
static  char                szDevMntDevName_g[32]           = { '\0' };     // "{ESP32_BLE_DEVICE}"
static  uint16_t            ui16DevMntSaveCfg_g             = 0;
static  uint16_t            ui16DevMntRstDev_g              = 0;

static  char                szWifiSSID_g[32]                = { '\0' };     // "{Enter WIFI SSID Name}"
static  char                szWifiPasswd_g[64]              = { '\0' };     // "{Enter WIFI Password}"
static  char                szWifiOwnAddr_g[24]             = { '\0' };     // "{0.0.0.0:0}"
static  uint16_t            ui16WifiOwnMode_g               = 0;            // WIFI_OPMODE_STA / WIFI_OPMODE_AP

static  uint16_t            ui16AppRtOpt1_g                 = 0;
static  uint16_t            ui16AppRtOpt2_g                 = 0;
static  uint16_t            ui16AppRtOpt3_g                 = 0;
static  uint16_t            ui16AppRtOpt4_g                 = 0;
static  uint16_t            ui16AppRtOpt5_g                 = 0;
static  uint16_t            ui16AppRtOpt6_g                 = 0;
static  uint16_t            ui16AppRtOpt7_g                 = 0;
static  uint16_t            ui16AppRtOpt8_g                 = 0;
static  char                szAppRtPeerAddr_g[24]           = { '\0' };     // "{0.0.0.0:0}"





//=========================================================================//
//                                                                         //
//          B L E   C A L L B A C K   C L A S S E S                        //
//                                                                         //
//=========================================================================//

//---------------------------------------------------------------------------
//  Class BleServerAppCallbacks
//---------------------------------------------------------------------------

class  BleServerAppCallbacks : public BLEServerCallbacks
{

    void onConnect(BLEServer* pBleServer_p)
    {
        fBleClientConnected_g = true;

        if (pfnAppCbHdlrConStatChg_g != NULL)
        {
            pfnAppCbHdlrConStatChg_g(fBleClientConnected_g);
        }
        return;
    };

    //-------------------------------------------------------------------
    void onDisconnect(BLEServer* pBleServer_p)
    {
        fBleClientConnected_g = false;

        if (pfnAppCbHdlrConStatChg_g != NULL)
        {
            pfnAppCbHdlrConStatChg_g(fBleClientConnected_g);
        }
        return;
    };

};



//---------------------------------------------------------------------------
//  Class BleCharacteristicDevMntSaveConfigCallbacks
//---------------------------------------------------------------------------

class  BleCharacteristicDevMntSaveConfigCallbacks : public BLECharacteristicCallbacks
{

    void onWrite(BLECharacteristic* pBleCharacteristic_p)
    {

        tAppCfgData  AppCfgData;
        bool         fSuccess;
        int          iRes;

        fSuccess = ESP32BleCfgProfile::ReadDataFromBleCharacterisics();
        if ( fSuccess )
        {
            if (pfnAppCbHdlrSaveConfig_g != NULL)
            {
                iRes = ESP32BleCfgProfile::ExportInstanceWorkspace(&AppCfgData);
                if (iRes >= 0)
                {
                    pfnAppCbHdlrSaveConfig_g(&AppCfgData);
                }
                else
                {
                    pfnAppCbHdlrSaveConfig_g(NULL);
                }
            }
        }

        return;

    }

};



//---------------------------------------------------------------------------
//  Class BleCharacteristicDevMntRestartDevCallbacks
//---------------------------------------------------------------------------

class  BleCharacteristicDevMntRestartDevCallbacks : public BLECharacteristicCallbacks
{

    void onWrite(BLECharacteristic* pBleCharacteristic_p)
    {

        if (pfnAppCbHdlrRestartDev_g != NULL)
        {
            pfnAppCbHdlrRestartDev_g();
        }

        return;

    }

};





/////////////////////////////////////////////////////////////////////////////
//                                                                         //
//          C O N S T R U C T O R   /   D E S T R U C T O R                //
//                                                                         //
/////////////////////////////////////////////////////////////////////////////

//---------------------------------------------------------------------------
//  Constructor
//---------------------------------------------------------------------------

ESP32BleCfgProfile::ESP32BleCfgProfile ()
{

    fBleClientConnected_g           = false;

    pfnAppCbHdlrSaveConfig_g        = NULL;
    pfnAppCbHdlrRestartDev_g        = NULL;

    pBleServer_g                    = NULL;

    pBleServiceDevMnt_g             = NULL;
    pBleCharacDevMntDevType_g       = NULL;
    pBleCharacDevMntSysTickCnt_g    = NULL;
    pBleCharacDevMntDevName_g       = NULL;
    pBleCharacDevMntSaveCfg_g       = NULL;
    pBleCharacDevMntRstDev_g        = NULL;

    pBleServiceWifi_g               = NULL;
    pBleCharacWifiSSID_g            = NULL;
    pBleCharacWifiPasswd_g          = NULL;
    pBleCharacWifiOwnAddr_g         = NULL;
    pBleCharacWifiOwnMode_g         = NULL;

    pBleServiceAppRt_g              = NULL;
    pBleCharacAppRtOpt1_g           = NULL;
    pBleCharacAppRtOpt2_g           = NULL;
    pBleCharacAppRtOpt3_g           = NULL;
    pBleCharacAppRtOpt4_g           = NULL;
    pBleCharacAppRtOpt5_g           = NULL;
    pBleCharacAppRtOpt6_g           = NULL;
    pBleCharacAppRtOpt7_g           = NULL;
    pBleCharacAppRtOpt8_g           = NULL;
    pBleCharacAppRtPeerAddr_g       = NULL;

    return;

}



//---------------------------------------------------------------------------
//  Destructor
//---------------------------------------------------------------------------

ESP32BleCfgProfile::~ESP32BleCfgProfile()
{

    return;

}





/////////////////////////////////////////////////////////////////////////////
//                                                                         //
//          P U B L I C    M E T H O D E S                                 //
//                                                                         //
/////////////////////////////////////////////////////////////////////////////

//---------------------------------------------------------------------------
//  ProfileSetup()
//---------------------------------------------------------------------------

int  ESP32BleCfgProfile::ProfileSetup (
        uint32_t ui32DeviceType_p,
        const tAppCfgData* pAppCfgData_p,
        const tAppDescriptData* pAppDescriptData_p,
        tCbHdlrSaveConfig pfnAppCbHdlrSaveConfig_p,
        tCbHdlrRestartDev pfnAppCbHdlrRestartDev_p,
        tCbHdlrConStatChg pfnAppCbHdlrConStatChg_p)
{

BLEDescriptor*  pBleDescriptor;
uint16_t        ui16OwnModeFeatList;
int             iRes;


    TRACE1("+ 'ProfileSetup()': ui32DeviceType_p=%d\n", ui32DeviceType_p);

    // Import configuration data given by Application into class instance workspace
    ui32DevMntDevType_g = ui32DeviceType_p;
    iRes = ImportInstanceWorkspace(pAppCfgData_p);
    if (iRes < 0)
    {
        return (-1);
    }

    // Save Pointer to Application Callback Handlers for 'SaveCfg' and 'RestartDev' as well as optional Handler 'ConnectionStatusChanged'
    pfnAppCbHdlrSaveConfig_g = pfnAppCbHdlrSaveConfig_p;
    pfnAppCbHdlrRestartDev_g = pfnAppCbHdlrRestartDev_p;
    pfnAppCbHdlrConStatChg_g = pfnAppCbHdlrConStatChg_p;
    if ((pfnAppCbHdlrSaveConfig_g == NULL) || (pfnAppCbHdlrRestartDev_g == NULL))
    {
        return (-2);
    }


    //****************[ SERVER ]****************
    BLEDevice::init(szDevMntDevName_g);             // e.g. "ESP32-BEACON"
    pBleServer_g = BLEDevice::createServer();
    pBleServer_g->setCallbacks(new BleServerAppCallbacks());


    // ======= [ SERVICE #1 [Device Management] ] =======
    {
        TRACE0("   SERVICE #1 [Device Management]\n");
        pBleServiceDevMnt_g = pBleServer_g->createService(BLEUUID(BLE_UUID_DEVMNT_SERVICE), NUM_HANDLES_DEVMNT_SERVICE, 0);

        // ---- [ CHARACTERISTIC #1 [DevMnt/DevType] ] ----
        {
            TRACE0("     CHARACTERISTIC #1 [DevMnt/DevType]\n");
            pBleCharacDevMntDevType_g = pBleServiceDevMnt_g->createCharacteristic(
                                                            BLE_UUID_DEVMNT_DEVTYPE_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ
                                                        );
            pBleCharacDevMntDevType_g->setValue(ui32DevMntDevType_g);
            pBleDescriptor = new BLEDescriptor(BLE_UUID_DEVMNT_DEVTYPE_DSCRPT);
            pBleDescriptor->setValue("Device Type");
            pBleCharacDevMntDevType_g->addDescriptor(pBleDescriptor);
        }

        // ---- [ CHARACTERISTIC #2 [DevMnt/SysTickCnt] ] ----
        {
            TRACE0("     CHARACTERISTIC #2 [DevMnt/SysTickCnt]\n");
            pBleCharacDevMntSysTickCnt_g = pBleServiceDevMnt_g->createCharacteristic(
                                                            BLE_UUID_DEVMNT_SYSTICKCNT_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacDevMntSysTickCnt_g->setValue(ui32DevMntSysTickCnt_g);
            pBleDescriptor = new BLEDescriptor(BLE_UUID_DEVMNT_SYSTICKCNT_DSCRPT);
            pBleDescriptor->setValue("System Tick Count");
            pBleCharacDevMntSysTickCnt_g->addDescriptor(pBleDescriptor);
        }

        // ---- [ CHARACTERISTIC #3 [DevMnt/DevName] ] ----
        {
            TRACE0("     CHARACTERISTIC #3 [DevMnt/DevName]\n");
            pBleCharacDevMntDevName_g = pBleServiceDevMnt_g->createCharacteristic(
                                                            BLE_UUID_DEVMNT_DEVNAME_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacDevMntDevName_g->setValue(szDevMntDevName_g);
            pBleDescriptor = new BLEDescriptor(BLE_UUID_DEVMNT_DEVNAME_DSCRPT);
            pBleDescriptor->setValue("Device Name");
            pBleCharacDevMntDevName_g->addDescriptor(pBleDescriptor);
        }

        // ---- [ CHARACTERISTIC #4 [DevMnt/SaveConfig] ] ----
        {
            TRACE0("     CHARACTERISTIC #4 [DevMnt/SaveConfig]\n");
            pBleCharacDevMntSaveCfg_g = pBleServiceDevMnt_g->createCharacteristic(
                                                            BLE_UUID_DEVMNT_SAVE_CFG_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_WRITE
                                                        );
            pBleDescriptor = new BLEDescriptor(BLE_UUID_DEVMNT_SAVE_CFG_DSCRPT);
            pBleDescriptor->setValue("Save Conig");
            pBleCharacDevMntSaveCfg_g->addDescriptor(pBleDescriptor);
            pBleCharacDevMntSaveCfg_g->setCallbacks(new BleCharacteristicDevMntSaveConfigCallbacks());
        }

        // ---- [ CHARACTERISTIC #5 [DevMnt/RstDev] ] ----
        {
            TRACE0("     CHARACTERISTIC #5 [DevMnt/RstDev]\n");
            pBleCharacDevMntRstDev_g = pBleServiceDevMnt_g->createCharacteristic(
                                                            BLE_UUID_DEVMNT_RST_DEV_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_WRITE
                                                        );
            pBleDescriptor = new BLEDescriptor(BLE_UUID_DEVMNT_RST_DEV_DSCRPT);
            pBleDescriptor->setValue("Restart Device");
            pBleCharacDevMntRstDev_g->addDescriptor(pBleDescriptor);
            pBleCharacDevMntRstDev_g->setCallbacks(new BleCharacteristicDevMntRestartDevCallbacks());
        }

        pBleServiceDevMnt_g->start();
    }


    // ======= [ SERVICE #2 [WIFI Config] ] =======
    {
        TRACE0("   SERVICE #2 [WIFI Config]\n");
        pBleServiceWifi_g = pBleServer_g->createService(BLEUUID(BLE_UUID_WIFI_SERVICE), NUM_HANDLES_WIFI_SERVICE, 0);

        // ---- [ CHARACTERISTIC #1 [Wifi/SSID] ] ----
        {
            TRACE0("     CHARACTERISTIC #1 [Wifi/SSID]\n");
            pBleCharacWifiSSID_g = pBleServiceWifi_g->createCharacteristic(
                                                            BLE_UUID_WIFI_SSID_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacWifiSSID_g->setValue(szWifiSSID_g);
            pBleDescriptor = new BLEDescriptor(BLE_UUID_WIFI_SSID_DSCRPT);
            pBleDescriptor->setValue("WIFI SSID");
            pBleCharacWifiSSID_g->addDescriptor(pBleDescriptor);
        }

        // ---- [ CHARACTERISTIC #2 [Wifi/Passwd] ] ----
        {
            TRACE0("     CHARACTERISTIC #2 [Wifi/Passwd]\n");
            pBleCharacWifiPasswd_g = pBleServiceWifi_g->createCharacteristic(
                                                            BLE_UUID_WIFI_PASSWD_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacWifiPasswd_g->setValue(szWifiPasswd_g);
            pBleDescriptor = new BLEDescriptor(BLE_UUID_WIFI_PASSWD_DSCRPT);
            pBleDescriptor->setValue("WIFI PASSWD");
            pBleCharacWifiPasswd_g->addDescriptor(pBleDescriptor);
        }

        // ---- [ CHARACTERISTIC #3 [Wifi/OwnAddr] ] ----
        {
            TRACE0("     CHARACTERISTIC #3 [Wifi/OwnAddr]\n");
            pBleCharacWifiOwnAddr_g = pBleServiceWifi_g->createCharacteristic(
                                                            BLE_UUID_WIFI_OWNADDR_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacWifiOwnAddr_g->setValue(szWifiOwnAddr_g);
            pBleDescriptor = new BLEDescriptor(BLE_UUID_WIFI_OWNADDR_DSCRPT);
            pBleDescriptor->setValue("Own Address");
            pBleCharacWifiOwnAddr_g->addDescriptor(pBleDescriptor);
        }

        // ---- [ CHARACTERISTIC #4 [Wifi/OwnMode] ] ----
        {
            TRACE0("     CHARACTERISTIC #4 [Wifi/OwnMode]\n");
            pBleCharacWifiOwnMode_g = pBleServiceWifi_g->createCharacteristic(
                                                            BLE_UUID_WIFI_OWNMODE_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacWifiOwnMode_g->setValue((uint16_t&)ui16WifiOwnMode_g);
            pBleDescriptor = new BLEDescriptor(BLE_UUID_WIFI_OWNMODE_DSCRPT);
            pBleDescriptor->setValue("Own Mode");
            pBleCharacWifiOwnMode_g->addDescriptor(pBleDescriptor);
            if (pAppDescriptData_p != NULL)
            {
                // set descriptor with supported WIFI modes (Station/Client, AccessPoint)
                ui16OwnModeFeatList = (uint16_t) pAppDescriptData_p->m_ui8OwnModeFeatList;
                pBleDescriptor = new BLEDescriptor(BLE_UUID_WIFI_OWNMODE_DSCRPT_FEATLIST);
                pBleDescriptor->setValue((uint8_t*)&ui16OwnModeFeatList, sizeof(ui16OwnModeFeatList));
                pBleCharacWifiOwnMode_g->addDescriptor(pBleDescriptor);
            }
        }

        pBleServiceWifi_g->start();
    }


    // ======= [ SERVICE #3 [APP RT Config] ] =======
    {
        TRACE0("   SERVICE #3 [APP RT Config]\n");
        pBleServiceAppRt_g = pBleServer_g->createService(BLEUUID(BLE_UUID_APP_RT_SERVICE), NUM_HANDLES_APP_RT_SERVICE, 0);

        // ---- [ CHARACTERISTIC #1 [AppRt/Opt1] ] ----
        {
            TRACE0("     CHARACTERISTIC #1 [AppRt/Opt1]\n");
            pBleCharacAppRtOpt1_g = pBleServiceAppRt_g->createCharacteristic(
                                                            BLE_UUID_APP_RT_OPT1_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacAppRtOpt1_g->setValue((uint16_t&)ui16AppRtOpt1_g);
            if ( (pAppDescriptData_p != NULL) && (pAppDescriptData_p->m_pszLabelOpt1 != NULL) )
            {
                pBleDescriptor = new BLEDescriptor(BLE_UUID_APP_RT_OPT1_DSCRPT);
                pBleDescriptor->setValue(pAppDescriptData_p->m_pszLabelOpt1);
                pBleCharacAppRtOpt1_g->addDescriptor(pBleDescriptor);
            }
        }

        // ---- [ CHARACTERISTIC #2 [AppRt/Opt2] ] ----
        {
            TRACE0("     CHARACTERISTIC #2 [AppRt/Opt2]\n");
            pBleCharacAppRtOpt2_g = pBleServiceAppRt_g->createCharacteristic(
                                                            BLE_UUID_APP_RT_OPT2_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacAppRtOpt2_g->setValue((uint16_t&)ui16AppRtOpt2_g);
            if ( (pAppDescriptData_p != NULL) && (pAppDescriptData_p->m_pszLabelOpt2 != NULL) )
            {
                pBleDescriptor = new BLEDescriptor(BLE_UUID_APP_RT_OPT2_DSCRPT);
                pBleDescriptor->setValue(pAppDescriptData_p->m_pszLabelOpt2);
                pBleCharacAppRtOpt2_g->addDescriptor(pBleDescriptor);
            }
        }

        // ---- [ CHARACTERISTIC #3 [AppRt/Opt3] ] ----
        {
            TRACE0("     CHARACTERISTIC #3 [AppRt/Opt3]\n");
            pBleCharacAppRtOpt3_g = pBleServiceAppRt_g->createCharacteristic(
                                                            BLE_UUID_APP_RT_OPT3_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacAppRtOpt3_g->setValue((uint16_t&)ui16AppRtOpt3_g);
            if ( (pAppDescriptData_p != NULL) && (pAppDescriptData_p->m_pszLabelOpt3 != NULL) )
            {
                pBleDescriptor = new BLEDescriptor(BLE_UUID_APP_RT_OPT3_DSCRPT);
                pBleDescriptor->setValue(pAppDescriptData_p->m_pszLabelOpt3);
                pBleCharacAppRtOpt3_g->addDescriptor(pBleDescriptor);
            }
        }

        // ---- [ CHARACTERISTIC #4 [AppRt/Opt4] ] ----
        {
            TRACE0("     CHARACTERISTIC #4 [AppRt/Opt4]\n");
            pBleCharacAppRtOpt4_g = pBleServiceAppRt_g->createCharacteristic(
                                                            BLE_UUID_APP_RT_OPT4_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacAppRtOpt4_g->setValue((uint16_t&)ui16AppRtOpt4_g);
            if ( (pAppDescriptData_p != NULL) && (pAppDescriptData_p->m_pszLabelOpt4 != NULL) )
            {
                pBleDescriptor = new BLEDescriptor(BLE_UUID_APP_RT_OPT4_DSCRPT);
                pBleDescriptor->setValue(pAppDescriptData_p->m_pszLabelOpt4);
                pBleCharacAppRtOpt4_g->addDescriptor(pBleDescriptor);
            }
        }

        // ---- [ CHARACTERISTIC #5[AppRt/Opt5] ] ----
        {
            TRACE0("     CHARACTERISTIC #5 [AppRt/Opt5]\n");
            pBleCharacAppRtOpt5_g = pBleServiceAppRt_g->createCharacteristic(
                                                            BLE_UUID_APP_RT_OPT5_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacAppRtOpt5_g->setValue((uint16_t&)ui16AppRtOpt5_g);
            if ( (pAppDescriptData_p != NULL) && (pAppDescriptData_p->m_pszLabelOpt5 != NULL) )
            {
                pBleDescriptor = new BLEDescriptor(BLE_UUID_APP_RT_OPT5_DSCRPT);
                pBleDescriptor->setValue(pAppDescriptData_p->m_pszLabelOpt5);
                pBleCharacAppRtOpt5_g->addDescriptor(pBleDescriptor);
            }
        }

        // ---- [ CHARACTERISTIC #6 [AppRt/Opt6] ] ----
        {
            TRACE0("     CHARACTERISTIC #6 [AppRt/Opt6]\n");
            pBleCharacAppRtOpt6_g = pBleServiceAppRt_g->createCharacteristic(
                                                            BLE_UUID_APP_RT_OPT6_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacAppRtOpt6_g->setValue((uint16_t&)ui16AppRtOpt6_g);
            if ( (pAppDescriptData_p != NULL) && (pAppDescriptData_p->m_pszLabelOpt6 != NULL) )
            {
                pBleDescriptor = new BLEDescriptor(BLE_UUID_APP_RT_OPT6_DSCRPT);
                pBleDescriptor->setValue(pAppDescriptData_p->m_pszLabelOpt6);
                pBleCharacAppRtOpt6_g->addDescriptor(pBleDescriptor);
            }
        }

        // ---- [ CHARACTERISTIC #7 [AppRt/Opt7] ] ----
        {
            TRACE0("     CHARACTERISTIC #7 [AppRt/Opt7]\n");
            pBleCharacAppRtOpt7_g = pBleServiceAppRt_g->createCharacteristic(
                                                            BLE_UUID_APP_RT_OPT7_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacAppRtOpt7_g->setValue((uint16_t&)ui16AppRtOpt7_g);
            if ( (pAppDescriptData_p != NULL) && (pAppDescriptData_p->m_pszLabelOpt7 != NULL) )
            {
                pBleDescriptor = new BLEDescriptor(BLE_UUID_APP_RT_OPT7_DSCRPT);
                pBleDescriptor->setValue(pAppDescriptData_p->m_pszLabelOpt7);
                pBleCharacAppRtOpt7_g->addDescriptor(pBleDescriptor);
            }
        }

        // ---- [ CHARACTERISTIC #8 [AppRt/Opt8] ] ----
        {
            TRACE0("     CHARACTERISTIC #8 [AppRt/Opt8]\n");
            pBleCharacAppRtOpt8_g = pBleServiceAppRt_g->createCharacteristic(
                                                            BLE_UUID_APP_RT_OPT8_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacAppRtOpt8_g->setValue((uint16_t&)ui16AppRtOpt8_g);
            if ( (pAppDescriptData_p != NULL) && (pAppDescriptData_p->m_pszLabelOpt8 != NULL) )
            {
                pBleDescriptor = new BLEDescriptor(BLE_UUID_APP_RT_OPT8_DSCRPT);
                pBleDescriptor->setValue(pAppDescriptData_p->m_pszLabelOpt8);
                pBleCharacAppRtOpt8_g->addDescriptor(pBleDescriptor);
            }
        }

        // ---- [ CHARACTERISTIC #9 [AppRt/PeerAddr] ] ----
        {
            TRACE0("     CHARACTERISTIC #9 [AppRt/PeerAddr]\n");
            pBleCharacAppRtPeerAddr_g = pBleServiceAppRt_g->createCharacteristic(
                                                            BLE_UUID_APP_RT_PEERADDR_CHARACTRSTC,
                                                            BLECharacteristic::PROPERTY_READ  |
                                                            BLECharacteristic::PROPERTY_WRITE |
                                                            BLECharacteristic::PROPERTY_NOTIFY
                                                        );
            pBleCharacAppRtPeerAddr_g->setValue(szAppRtPeerAddr_g);
            if ( (pAppDescriptData_p != NULL) && (pAppDescriptData_p->m_pszLabelPeerAddr != NULL) )
            {
                pBleDescriptor = new BLEDescriptor(BLE_UUID_APP_RT_PEERADDR_DSCRPT);
                pBleDescriptor->setValue(pAppDescriptData_p->m_pszLabelPeerAddr);
                pBleCharacAppRtPeerAddr_g->addDescriptor(pBleDescriptor);
            }
        }

        pBleServiceAppRt_g->start();
    }


    //---- Start Server ----
    TRACE0("   BleServer_g->startAdvertising()\n");
    pBleServer_g->startAdvertising();

    TRACE0("- 'ProfileSetup()'\n");

    return (1);

};



//---------------------------------------------------------------------------
//  ProfileLoop()
//---------------------------------------------------------------------------

bool  ESP32BleCfgProfile::ProfileLoop ()
{

unsigned long  ulCurrTick;
bool           fBleNotify;

    fBleNotify = false;

    if ( fBleClientConnected_g )
    {
        ulCurrTick = millis();
        if ((ulCurrTick - ui32DevMntSysTickCnt_g) >= 1000)
        {
            ui32DevMntSysTickCnt_g = ulCurrTick;
            pBleCharacDevMntSysTickCnt_g->setValue(ui32DevMntSysTickCnt_g);
            pBleCharacDevMntSysTickCnt_g->notify();

            fBleNotify = true;
        }
    }

    return (fBleNotify);

}



//---------------------------------------------------------------------------
//  IsBleClientConnected()
//---------------------------------------------------------------------------

bool  ESP32BleCfgProfile::IsBleClientConnected ()
{

    return ( fBleClientConnected_g );

}



//---------------------------------------------------------------------------
//  STATIC: ReadDataFromBleCharacterisics
//---------------------------------------------------------------------------

bool  ESP32BleCfgProfile::ReadDataFromBleCharacterisics ()
{

std::string  stdstrData;
String       strData;
uint8_t*     pui8Data;
int          iRes;
bool         fSuccess;


    TRACE0("+ 'ReadDataFromBleCharacterisics()...'\n");

    fSuccess = true;


    // ---- [DevMnt/DevName] ----
    stdstrData = pBleCharacDevMntDevName_g->getValue();
    strData = String(stdstrData.c_str());
    strncpy(szDevMntDevName_g, strData.c_str(), sizeof(szDevMntDevName_g));


    // ---- [Wifi/SSID] ----
    stdstrData = pBleCharacWifiSSID_g->getValue();
    strData = String(stdstrData.c_str());
    strncpy(szWifiSSID_g, strData.c_str(), sizeof(szWifiSSID_g));

    // ---- [Wifi/Passwd] ----
    stdstrData = pBleCharacWifiPasswd_g->getValue();
    strData = String(stdstrData.c_str());
    strncpy(szWifiPasswd_g, strData.c_str(), sizeof(szWifiPasswd_g));

    // ---- [Wifi/OwnAddr] ----
    stdstrData = pBleCharacWifiOwnAddr_g->getValue();
    strData = String(stdstrData.c_str());
    strncpy(szWifiOwnAddr_g, strData.c_str(), sizeof(szWifiOwnAddr_g));

    // ---- [Wifi/OwnMode] ----
    pui8Data = pBleCharacWifiOwnMode_g->getData();
    if (pui8Data != NULL)
    {
        ui16WifiOwnMode_g = (uint16_t)(*pui8Data);
    }


    // ---- [AppRt/Opt1] ----
    pui8Data = pBleCharacAppRtOpt1_g->getData();
    if (pui8Data != NULL)
    {
        ui16AppRtOpt1_g = (*pui8Data > 0) ? 1 : 0;
    }

    // ---- [AppRt/Opt2] ----
    pui8Data = pBleCharacAppRtOpt2_g->getData();
    if (pui8Data != NULL)
    {
        ui16AppRtOpt2_g = (*pui8Data > 0) ? 1 : 0;
    }

    // ---- [AppRt/Opt3] ----
    pui8Data = pBleCharacAppRtOpt3_g->getData();
    if (pui8Data != NULL)
    {
        ui16AppRtOpt3_g = (*pui8Data > 0) ? 1 : 0;
    }

    // ---- [AppRt/Opt4] ----
    pui8Data = pBleCharacAppRtOpt4_g->getData();
    if (pui8Data != NULL)
    {
        ui16AppRtOpt4_g = (*pui8Data > 0) ? 1 : 0;
    }

    // ---- [AppRt/Opt5] ----
    pui8Data = pBleCharacAppRtOpt5_g->getData();
    if (pui8Data != NULL)
    {
        ui16AppRtOpt5_g = (*pui8Data > 0) ? 1 : 0;
    }

    // ---- [AppRt/Opt6] ----
    pui8Data = pBleCharacAppRtOpt6_g->getData();
    if (pui8Data != NULL)
    {
        ui16AppRtOpt6_g = (*pui8Data > 0) ? 1 : 0;
    }

    // ---- [AppRt/Opt7] ----
    pui8Data = pBleCharacAppRtOpt7_g->getData();
    if (pui8Data != NULL)
    {
        ui16AppRtOpt7_g = (*pui8Data > 0) ? 1 : 0;
    }

    // ---- [AppRt/Opt8] ----
    pui8Data = pBleCharacAppRtOpt8_g->getData();
    if (pui8Data != NULL)
    {
        ui16AppRtOpt8_g = (*pui8Data > 0) ? 1 : 0;
    }

    // ---- [AppRt/PeerAddr] ----
    stdstrData = pBleCharacAppRtPeerAddr_g->getValue();
    strData = String(stdstrData.c_str());
    strncpy(szAppRtPeerAddr_g, strData.c_str(), sizeof(szAppRtPeerAddr_g));

    TRACE0("- 'ReadDataFromBleCharacterisics()'\n");

    return (fSuccess);

}



//---------------------------------------------------------------------------
//  STATIC: ImportInstanceWorkspace()
//---------------------------------------------------------------------------

int  ESP32BleCfgProfile::ImportInstanceWorkspace (
        const tAppCfgData* pAppCfgData_p)
{

    TRACE0("+ 'ImportInstanceWorkspace()...'\n");

    if (pAppCfgData_p == NULL)
    {
        return (-1);
    }

    if (sizeof(szDevMntDevName_g) < sizeof(pAppCfgData_p->m_szDevMntDevName))
    {
        return (-2);
    }
    memcpy(szDevMntDevName_g, pAppCfgData_p->m_szDevMntDevName, sizeof(pAppCfgData_p->m_szDevMntDevName));

    if (sizeof(szWifiSSID_g) < sizeof(pAppCfgData_p->m_szWifiSSID))
    {
        return (-3);
    }
    memcpy(szWifiSSID_g, pAppCfgData_p->m_szWifiSSID, sizeof(pAppCfgData_p->m_szWifiSSID));

    if (sizeof(szWifiPasswd_g) < sizeof(pAppCfgData_p->m_szWifiPasswd))
    {
        return (-4);
    }
    memcpy(szWifiPasswd_g, pAppCfgData_p->m_szWifiPasswd, sizeof(pAppCfgData_p->m_szWifiPasswd));

    if (sizeof(szWifiOwnAddr_g) < sizeof(pAppCfgData_p->m_szWifiOwnAddr))
    {
        return (-5);
    }
    memcpy(szWifiOwnAddr_g, pAppCfgData_p->m_szWifiOwnAddr, sizeof(pAppCfgData_p->m_szWifiOwnAddr));

    ui16WifiOwnMode_g = (uint16_t) pAppCfgData_p->m_ui8WifiOwnMode;

    ui16AppRtOpt1_g = (uint16_t) (pAppCfgData_p->m_fAppRtOpt1 ? 1 : 0);
    ui16AppRtOpt2_g = (uint16_t) (pAppCfgData_p->m_fAppRtOpt2 ? 1 : 0);
    ui16AppRtOpt3_g = (uint16_t) (pAppCfgData_p->m_fAppRtOpt3 ? 1 : 0);
    ui16AppRtOpt4_g = (uint16_t) (pAppCfgData_p->m_fAppRtOpt4 ? 1 : 0);
    ui16AppRtOpt5_g = (uint16_t) (pAppCfgData_p->m_fAppRtOpt5 ? 1 : 0);
    ui16AppRtOpt6_g = (uint16_t) (pAppCfgData_p->m_fAppRtOpt6 ? 1 : 0);
    ui16AppRtOpt7_g = (uint16_t) (pAppCfgData_p->m_fAppRtOpt7 ? 1 : 0);
    ui16AppRtOpt8_g = (uint16_t) (pAppCfgData_p->m_fAppRtOpt8 ? 1 : 0);

    if (sizeof(szAppRtPeerAddr_g) < sizeof(pAppCfgData_p->m_szAppRtPeerAddr))
    {
        return (-6);
    }
    memcpy(szAppRtPeerAddr_g, pAppCfgData_p->m_szAppRtPeerAddr, sizeof(pAppCfgData_p->m_szAppRtPeerAddr));

    TRACE0("- 'ImportInstanceWorkspace()'\n");

    return (1);

}



//---------------------------------------------------------------------------
//  STATIC: ExportInstanceWorkspace()
//---------------------------------------------------------------------------

int  ESP32BleCfgProfile::ExportInstanceWorkspace (
        tAppCfgData* pAppCfgData_p)
{

    TRACE0("+ 'ExportInstanceWorkspace()...'\n");

    if (pAppCfgData_p == NULL)
    {
        return (-1);
    }

    memset(pAppCfgData_p, 0x00, sizeof(pAppCfgData_p));

    if (sizeof(pAppCfgData_p->m_szDevMntDevName) < sizeof(szDevMntDevName_g))
    {
        return (-2);
    }
    memcpy(pAppCfgData_p->m_szDevMntDevName, szDevMntDevName_g, sizeof(pAppCfgData_p->m_szDevMntDevName));

    if (sizeof(pAppCfgData_p->m_szWifiSSID) < sizeof(szWifiSSID_g))
    {
        return (-3);
    }
    memcpy(pAppCfgData_p->m_szWifiSSID, szWifiSSID_g, sizeof(pAppCfgData_p->m_szWifiSSID));

    if (sizeof(pAppCfgData_p->m_szWifiPasswd) < sizeof(szWifiPasswd_g))
    {
        return (-4);
    }
    memcpy(pAppCfgData_p->m_szWifiPasswd, szWifiPasswd_g, sizeof(pAppCfgData_p->m_szWifiPasswd));

    if (sizeof(pAppCfgData_p->m_szWifiOwnAddr) < sizeof(szWifiOwnAddr_g))
    {
        return (-5);
    }
    memcpy(pAppCfgData_p->m_szWifiOwnAddr, szWifiOwnAddr_g, sizeof(pAppCfgData_p->m_szWifiOwnAddr));

    pAppCfgData_p->m_ui8WifiOwnMode = (uint8_t)ui16WifiOwnMode_g;

    pAppCfgData_p->m_fAppRtOpt1 = (ui16AppRtOpt1_g == 0) ? false : true;
    pAppCfgData_p->m_fAppRtOpt2 = (ui16AppRtOpt2_g == 0) ? false : true;
    pAppCfgData_p->m_fAppRtOpt3 = (ui16AppRtOpt3_g == 0) ? false : true;
    pAppCfgData_p->m_fAppRtOpt4 = (ui16AppRtOpt4_g == 0) ? false : true;
    pAppCfgData_p->m_fAppRtOpt5 = (ui16AppRtOpt5_g == 0) ? false : true;
    pAppCfgData_p->m_fAppRtOpt6 = (ui16AppRtOpt6_g == 0) ? false : true;
    pAppCfgData_p->m_fAppRtOpt7 = (ui16AppRtOpt7_g == 0) ? false : true;
    pAppCfgData_p->m_fAppRtOpt8 = (ui16AppRtOpt8_g == 0) ? false : true;

    if (sizeof(pAppCfgData_p->m_szAppRtPeerAddr) < sizeof(szAppRtPeerAddr_g))
    {
        return (-6);
    }
    memcpy(pAppCfgData_p->m_szAppRtPeerAddr, szAppRtPeerAddr_g, sizeof(pAppCfgData_p->m_szAppRtPeerAddr));

    TRACE0("- 'ExportInstanceWorkspace()'\n");

    return (1);

}





/////////////////////////////////////////////////////////////////////////////
//                                                                         //
//          P R I V A T E    M E T H O D E S                               //
//                                                                         //
/////////////////////////////////////////////////////////////////////////////




//  EOF
