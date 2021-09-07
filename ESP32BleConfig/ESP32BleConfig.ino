/****************************************************************************

  Copyright (c) 2021 Ronald Sieber

  Project:      ESP32 BLE Config
  Description:  ESP32 Arduino BLE Config Framework

  -------------------------------------------------------------------------

    Arduino IDE Settings:

    Board:              "ESP32 Dev Module"
    Upload Speed:       "115200"
    CPU Frequency:      "240MHz (WiFi/BT)"
    Flash Frequency:    "80Mhz"
    Flash Mode:         "QIO"
    Flash Size:         "4MB (32Mb)"
    Partition Scheme:   "No OTA (2MB APP/2MB SPIFFS)"
    PSRAM:              "Disabled"

  -------------------------------------------------------------------------

  Revision History:

  2021/07/13 -rs:   V1.00 Initial version

****************************************************************************/

// #define DEBUG_DUMP_BUFFER


#include "ESP32BleCfgProfile.h"
#include "ESP32BleAppCfgData.h"





/***************************************************************************/
/*                                                                         */
/*                                                                         */
/*          G L O B A L   D E F I N I T I O N S                            */
/*                                                                         */
/*                                                                         */
/***************************************************************************/

//---------------------------------------------------------------------------
//  Application Configuration
//---------------------------------------------------------------------------

const int       APP_VERSION                         = 1;                // 1.xx
const int       APP_REVISION                        = 0;                // x.00
const char      APP_BUILD_TIMESTAMP[]               = __DATE__ " " __TIME__;

const int       CFG_ENABLE_STATUS_LED               = 1;

// EEPROM Size
#define         APP_EEPROM_SIZE                     512

// Application specific Device Type
#define         APP_DEVICE_TYPE                     1000000             // DeviceType associated with the BLE Profile

// Default/initial values for Application Configuration <tAppCfgData>
#define         APP_CFGDATA_MAGIC_ID                0x45735243          // ASCII 'EsRC' = [Es]p32[R]emote[C]onfig
#define         APP_DEFAULT_DEVICE_NAME             "{ESP32_BLE_DEVICE}"
#define         APP_DEFAULT_WIFI_SSID               "{WIFI SSID Name}"
#define         APP_DEFAULT_WIFI_PASSWD             "{WIFI Password}"
#define         APP_DEFAULT_WIFI_OWNADDR            "0.0.0.0:0"
#define         APP_DEFAULT_WIFI_OWNMODE            WIFI_OPMODE_STA     // WIFI_OPMODE_STA / WIFI_OPMODE_AP
#define         APP_DEFAULT_APP_RT_OPT1             true
#define         APP_DEFAULT_APP_RT_OPT2             false
#define         APP_DEFAULT_APP_RT_OPT3             true
#define         APP_DEFAULT_APP_RT_OPT4             false
#define         APP_DEFAULT_APP_RT_OPT5             false
#define         APP_DEFAULT_APP_RT_OPT6             true
#define         APP_DEFAULT_APP_RT_OPT7             false
#define         APP_DEFAULT_APP_RT_OPT8             true
#define         APP_DEFAULT_APP_RT_PEERADDR         "0.0.0.0:0"

// Application specific Descriptor Text ('Labels') resp. FeatureLists for BLE Characteristics <tAppDescriptData>
#define         APP_DESCRPT_WIFI_OWNMODE_FEATLIST   (WIFI_OPMODE_STA | WIFI_OPMODE_AP)

#define         APP_LABEL_APP_RT_OPT1               "APP Runtime Opt#1"
#define         APP_LABEL_APP_RT_OPT2               "APP Runtime Opt#2"
#define         APP_LABEL_APP_RT_OPT3               "APP Runtime Opt#3"
#define         APP_LABEL_APP_RT_OPT4               "APP Runtime Opt#4"
#define         APP_LABEL_APP_RT_OPT5               "APP Runtime Opt#5"
#define         APP_LABEL_APP_RT_OPT6               "APP Runtime Opt#6"
#define         APP_LABEL_APP_RT_OPT7               "# (not used)"      // Start with '#' -> disable in GUI Config Tool
#define         APP_LABEL_APP_RT_OPT8               "# (not used)"      // Start with '#' -> disable in GUI Config Tool
#define         APP_LABEL_APP_RT_PEERADDR           "Peer Address"



//---------------------------------------------------------------------------
//  Hardware/Pin Configuration
//---------------------------------------------------------------------------

const int       PIN_KEY_BLE_CFG                     = 36;               // PIN_KEY_BLE_CFG      (GPIO36 -> Pin02)
const int       PIN_STATUS_LED                      =  2;               // On-board LED (blue)  (GPIO02 -> Pin19)



//---------------------------------------------------------------------------
//  Local Variables
//---------------------------------------------------------------------------

static tAppCfgData  AppCfgData_g =
{

    APP_CFGDATA_MAGIC_ID,                           // .m_ui32MagicID

    APP_DEFAULT_DEVICE_NAME,                        // .m_szDevMntDevName

    APP_DEFAULT_WIFI_SSID,                          // .m_szWifiSSID
    APP_DEFAULT_WIFI_PASSWD,                        // .m_szWifiPasswd
    APP_DEFAULT_WIFI_OWNADDR,                       // .m_szWifiOwnAddr
    APP_DEFAULT_WIFI_OWNMODE,                       // .m_ui8WifiOwnMode

    APP_DEFAULT_APP_RT_OPT1,                        // .m_fAppRtOpt1 : 1
    APP_DEFAULT_APP_RT_OPT2,                        // .m_fAppRtOpt2 : 1
    APP_DEFAULT_APP_RT_OPT3,                        // .m_fAppRtOpt3 : 1
    APP_DEFAULT_APP_RT_OPT4,                        // .m_fAppRtOpt4 : 1
    APP_DEFAULT_APP_RT_OPT5,                        // .m_fAppRtOpt5 : 1
    APP_DEFAULT_APP_RT_OPT6,                        // .m_fAppRtOpt6 : 1
    APP_DEFAULT_APP_RT_OPT7,                        // .m_fAppRtOpt7 : 1
    APP_DEFAULT_APP_RT_OPT8,                        // .m_fAppRtOpt8 : 1
    APP_DEFAULT_APP_RT_PEERADDR                     // .m_szAppRtPeerAddr

};

static tAppDescriptData  AppDescriptData_g =
{

    APP_DESCRPT_WIFI_OWNMODE_FEATLIST,              // .m_ui16OwnModeFeatList

    APP_LABEL_APP_RT_OPT1,                          // .m_pszLabelOpt1
    APP_LABEL_APP_RT_OPT2,                          // .m_pszLabelOpt2
    APP_LABEL_APP_RT_OPT3,                          // .m_pszLabelOpt3
    APP_LABEL_APP_RT_OPT4,                          // .m_pszLabelOpt4
    APP_LABEL_APP_RT_OPT5,                          // .m_pszLabelOpt5
    APP_LABEL_APP_RT_OPT6,                          // .m_pszLabelOpt6
    APP_LABEL_APP_RT_OPT7,                          // .m_pszLabelOpt7
    APP_LABEL_APP_RT_OPT8,                          // .m_pszLabelOpt8

    APP_LABEL_APP_RT_PEERADDR                       // .m_pszLabelPeerAddr

};


static  ESP32BleCfgProfile  ESP32BleCfgProfile_g;
static  ESP32BleAppCfgData  ESP32BleAppCfgData_g(APP_EEPROM_SIZE);

static  IPAddress       WifiOwnIpAddress_g          = IPAddress(0,0,0,0);
static  char            szWifiOwnIpAddress_g[16]    = "";
static  uint16_t        ui16WifiOwnPortNum_g        = 0;

static  IPAddress       AppRtPeerIpAddress_g        = IPAddress(0,0,0,0);
static  char            szAppRtPeerIpAddress_g[16]  = "";
static  uint16_t        ui16AppRtPeerPortNum_g      = 0;

static  bool            fStateBleCfg_g;
static  bool            fBleClientConnected_g       = false;

static  String          strChipID_g;
static  unsigned int    uiMainLoopProcStep_g        = 0;



//---------------------------------------------------------------------------
//  Local Functions
//---------------------------------------------------------------------------











//=========================================================================//
//                                                                         //
//          S K E T C H   P U B L I C   F U N C T I O N S                  //
//                                                                         //
//=========================================================================//

//---------------------------------------------------------------------------
//  Application Setup
//---------------------------------------------------------------------------

void setup()
{

char  szTextBuff[64];
int   iResult;


    // Serial console
    Serial.begin(115200);
    Serial.println();
    Serial.println();
    Serial.println("======== APPLICATION START ========");
    Serial.println();
    Serial.flush();


    // Application Version Information
    snprintf(szTextBuff, sizeof(szTextBuff), "App Version:      %u.%02u", APP_VERSION, APP_REVISION);
    Serial.println(szTextBuff);
    snprintf(szTextBuff, sizeof(szTextBuff), "Build Timestamp:  %s", APP_BUILD_TIMESTAMP);
    Serial.println(szTextBuff);
    Serial.println();
    Serial.flush();


    // Device Identification
    strChipID_g = GetChipID();
    Serial.print("Unique ChipID:    ");
    Serial.println(strChipID_g);
    Serial.println();
    Serial.flush();


    // Initialize Workspace
    fBleClientConnected_g = false;


    //-------------------------------------------------------------------
    //  Step(1): Get Configuration Data
    //-------------------------------------------------------------------
    //           Try to get Data from EEPROM, otherwise keep default
    //           values untouched.
    //-------------------------------------------------------------------
    Serial.println("Configuration Data Block Size: " + String(sizeof(tAppCfgData)) + " Bytes");
    Serial.println("Get Configuration Data...");
    iResult = ESP32BleAppCfgData_g.LoadAppCfgDataFromEeprom(&AppCfgData_g);
    if (iResult == 1)
    {
        Serial.print("-> Use saved Data read from EEPROM");
    }
    else if (iResult == 0)
    {
        Serial.println("-> Keep default data untouched");
    }
    else
    {
        Serial.print("-> ERROR: Access to EEPROM failed! (ErrorCode=");
        Serial.print(iResult);
        Serial.println(")");
    }
    Serial.println("Configuration Data Setup:");
    AppPrintConfigData(&AppCfgData_g);

    iResult = AppSplitNetAddress (AppCfgData_g.m_szWifiOwnAddr, &WifiOwnIpAddress_g, &ui16WifiOwnPortNum_g);
    if (iResult >= 0)
    {
        strncpy(szWifiOwnIpAddress_g, WifiOwnIpAddress_g.toString().c_str(), WifiOwnIpAddress_g.toString().length());
        Serial.print("WifiOwnIpAddr:    ");     Serial.println(szWifiOwnIpAddress_g);
        Serial.print("WifiOwnPortNum:   ");     Serial.println(ui16WifiOwnPortNum_g);
    }
    else
    {
        Serial.print("-> ERROR: SplitNetAddress for 'WifiOwnIpAddress' failed! (ErrorCode=");
        Serial.print(iResult);
        Serial.println(")");
    }

    iResult = AppSplitNetAddress (AppCfgData_g.m_szAppRtPeerAddr, &AppRtPeerIpAddress_g, &ui16AppRtPeerPortNum_g);
    if (iResult >= 0)
    {
        strncpy(szAppRtPeerIpAddress_g, AppRtPeerIpAddress_g.toString().c_str(), AppRtPeerIpAddress_g.toString().length());
        Serial.print("AppRtPeerIpAddr:  ");     Serial.println(szAppRtPeerIpAddress_g);
        Serial.print("AppRtPeerPortNum: ");     Serial.println(ui16AppRtPeerPortNum_g);
    }
    else
    {
        Serial.print("-> ERROR: SplitNetAddress for 'AppRtPeerIpAddress' failed! (ErrorCode=");
        Serial.print(iResult);
        Serial.println(")");
    }


    //-------------------------------------------------------------------
    //  Step(2): Setup BLE Profile
    //-------------------------------------------------------------------
    // Determine Working Mode (BLE Config or Normal Operation)
    pinMode(PIN_KEY_BLE_CFG, INPUT);
    delay(10);
    fStateBleCfg_g = !digitalRead(PIN_KEY_BLE_CFG);                         // Keys are inverted (1=off, 0=on)
    if ( fStateBleCfg_g )
    {
        //-----------------------------------------------------------
        // BLE Config Mode -> Setup BLE Profile
        //-----------------------------------------------------------
        if ( CFG_ENABLE_STATUS_LED )
        {
            pinMode(PIN_STATUS_LED, OUTPUT);
        }

        Serial.println("Setup BLE Profile...");
        iResult = ESP32BleCfgProfile_g.ProfileSetup(APP_DEVICE_TYPE, &AppCfgData_g, &AppDescriptData_g, AppCbHdlrSaveConfig, AppCbHdlrRestartDev, AppCbHdlrConStatChg);
        if (iResult >= 0)
        {
            Serial.println("-> BLE Server started successfully");
        }
        else
        {
            Serial.print("-> ERROR: BLE Server start failed! (ErrorCode=");
            Serial.print(iResult);
            Serial.println(")");
        }
    }
    else
    {
        //-----------------------------------------------------------
        // Normal Operation Mode -> User/Application specific Setup
        //-----------------------------------------------------------
        //
        //  ...
        //  <User/Application specific Startup Code here>
        //  ...
        //
    }


    return;

}



//---------------------------------------------------------------------------
//  Application Main Loop
//---------------------------------------------------------------------------

void loop()
{

unsigned int  uiProcStep;


    // Determine Working Mode (BLE Config or Normal Operation)
    if ( fStateBleCfg_g )
    {
        //-----------------------------------------------------------
        // BLE Config Mode -> Run BLE Profile specific Loop Code
        //-----------------------------------------------------------
        ESP32BleCfgProfile_g.ProfileLoop();

        // signal BLE Config Mode on Status LED
        if ( CFG_ENABLE_STATUS_LED )
        {
            uiProcStep = (fBleClientConnected_g) ? (uiMainLoopProcStep_g++ % 15) : (uiMainLoopProcStep_g++ % 45);
            switch (uiProcStep)
            {
                case 0:
                case 1:
                {
                    digitalWrite(PIN_STATUS_LED, HIGH);
                    break;
                }
    
                default:
                {
                    digitalWrite(PIN_STATUS_LED, LOW);
                    break;
                }
            }
        }
    }
    else
    {
        //-----------------------------------------------------------
        // Normal Operation Mode -> User/Application specific Loop
        //-----------------------------------------------------------
        //
        //  ...
        //  <User/Application specific Loop Code here>
        //  ...
        //
    }


    delay(50);


    return;

}





//=========================================================================//
//                                                                         //
//          S K E T C H   P R I V A T E   F U N C T I O N S                //
//                                                                         //
//=========================================================================//

//---------------------------------------------------------------------------
//  Application Callback Handler: Save Configuration Data Block
//---------------------------------------------------------------------------

void  AppCbHdlrSaveConfig(const tAppCfgData* pAppCfgData_p)
{

int  iResult;

    Serial.println();
    Serial.println("Save Configuration Data:");

    if (pAppCfgData_p != NULL)
    {
        memcpy(&AppCfgData_g, pAppCfgData_p, sizeof(AppCfgData_g));
        AppPrintConfigData(&AppCfgData_g);

        iResult = ESP32BleAppCfgData_g.SaveAppCfgDataToEeprom(&AppCfgData_g);
        if (iResult >= 0)
        {
            Serial.println("-> Configuration Data saved successfully");
        }
        else
        {
            Serial.print("ERROR: Saving Configuration Data failed! (ErrorCode=");
            Serial.print(iResult);
            Serial.println(")");
        }
    }
    else
    {
        Serial.println("ERROR: Configuration Failed!");
    }

    return;

}



//---------------------------------------------------------------------------
//  Application Callback Handler: Restart Device
//---------------------------------------------------------------------------

void  AppCbHdlrRestartDev()
{

    Serial.println();
    Serial.println("Restart Device:");
    Serial.println("-> REBOOT System now...");
    Serial.println();

    ESP.restart();

    return;

}



//---------------------------------------------------------------------------
//  Application Callback Handler: Inform about BLE Client Connect Status
//---------------------------------------------------------------------------

void  AppCbHdlrConStatChg (bool fBleClientConnected_p)
{

    fBleClientConnected_g = fBleClientConnected_p;

    if ( fBleClientConnected_g )
    {
        Serial.println();
        Serial.println("Client connected");
        Serial.println();
    }
    else
    {
        Serial.println();
        Serial.println("Client disconnected");
        Serial.println();
    }

    return;

}



//---------------------------------------------------------------------------
//  Print Configuration Data Block
//---------------------------------------------------------------------------

void  AppPrintConfigData (const tAppCfgData* pAppCfgData_p)
{

    if (pAppCfgData_p != NULL)
    {
        Serial.print("  DevMntDevName:  ");     Serial.println(pAppCfgData_p->m_szDevMntDevName);   Serial.flush();
        Serial.print("  WifiSSID:       ");     Serial.println(pAppCfgData_p->m_szWifiSSID);        Serial.flush();
        Serial.print("  WifiPasswd:     ");     Serial.println(pAppCfgData_p->m_szWifiPasswd);      Serial.flush();
        Serial.print("  WifiOwnAddr:    ");     Serial.println(pAppCfgData_p->m_szWifiOwnAddr);     Serial.flush();
        Serial.print("  WifiOwnMode:    ");     Serial.println(pAppCfgData_p->m_ui8WifiOwnMode);    Serial.flush();
        Serial.print("  AppRtOpt1:      ");     Serial.println(pAppCfgData_p->m_fAppRtOpt1);        Serial.flush();
        Serial.print("  AppRtOpt2:      ");     Serial.println(pAppCfgData_p->m_fAppRtOpt2);        Serial.flush();
        Serial.print("  AppRtOpt3:      ");     Serial.println(pAppCfgData_p->m_fAppRtOpt3);        Serial.flush();
        Serial.print("  AppRtOpt4:      ");     Serial.println(pAppCfgData_p->m_fAppRtOpt4);        Serial.flush();
        Serial.print("  AppRtOpt5:      ");     Serial.println(pAppCfgData_p->m_fAppRtOpt5);        Serial.flush();
        Serial.print("  AppRtOpt6:      ");     Serial.println(pAppCfgData_p->m_fAppRtOpt6);        Serial.flush();
        Serial.print("  AppRtOpt7:      ");     Serial.println(pAppCfgData_p->m_fAppRtOpt7);        Serial.flush();
        Serial.print("  AppRtOpt8:      ");     Serial.println(pAppCfgData_p->m_fAppRtOpt8);        Serial.flush();
        Serial.print("  AppRtPeerAddr:  ");     Serial.println(pAppCfgData_p->m_szAppRtPeerAddr);   Serial.flush();
    }

    return;

}



//---------------------------------------------------------------------------
//  Split Network Address String into IpAddress and PortNumber
//---------------------------------------------------------------------------

int  AppSplitNetAddress (const char* pszNetAddr_p, IPAddress* pIpAddress_p, uint16_t* pui16PortNum_p)
{

String     strNetAddr;
String     strIpAddr;
String     strPortNum;
IPAddress  IpAddr;
long       lPortNum;
int        iIdx;
bool       fSuccess;

    // process IPAddress
    strNetAddr = pszNetAddr_p;
    iIdx = strNetAddr.indexOf(':');
    if (iIdx > 0)
    {
        strIpAddr = strNetAddr.substring(0, iIdx);
    }
    else
    {
        strIpAddr = strNetAddr;
    }

    fSuccess = IpAddr.fromString(strIpAddr);
    if ( fSuccess )
    {
        if (pIpAddress_p != NULL)
        {
            *pIpAddress_p = IpAddr;
        }
    }
    else
    {
        return (-1);
    }

    // process PortNumber
    if (iIdx > 0)
    {
        strPortNum = strNetAddr.substring(iIdx+1);
        lPortNum = strPortNum.toInt();
        if ((lPortNum >= 0) && (lPortNum <= 0xFFFF))
        {
            if (pui16PortNum_p != NULL)
            {
                *pui16PortNum_p = (uint16_t)lPortNum;
            }
        }
        else
        {
            return (-2);
        }
    }

    return (0);

}





//=========================================================================//
//                                                                         //
//          P R I V A T E   G E N E R I C   F U N C T I O N S              //
//                                                                         //
//=========================================================================//

//---------------------------------------------------------------------------
//  Get Unique Client Name
//---------------------------------------------------------------------------

String  GetUniqueClientName (const char* pszClientPrefix_p)
{

String  strChipID;
String  strClientName;


    // Create a unique client name, based on ChipID (the ChipID is essentially its 6byte MAC address)
    strChipID = GetChipID();
    strClientName  = pszClientPrefix_p;
    strClientName += strChipID;

    return (strClientName);

}



//---------------------------------------------------------------------------
//  Get ChipID as String
//---------------------------------------------------------------------------

String  GetChipID()
{

String  strChipID;


    strChipID = GetEsp32MacId(false);

    return (strChipID);

}



//---------------------------------------------------------------------------
//  Get ChipMAC as String
//---------------------------------------------------------------------------

String  GetChipMAC()
{

String  strChipMAC;


    strChipMAC = GetEsp32MacId(true);

    return (strChipMAC);

}



//---------------------------------------------------------------------------
//  Get GetEsp32MacId as String
//---------------------------------------------------------------------------

String  GetEsp32MacId (bool fUseMacFormat_p)
{

uint64_t  ui64MacID;
String    strMacID;
byte      bDigit;
char      acDigit[2];
int       iIdx;


    ui64MacID = ESP.getEfuseMac();
    strMacID = "";
    for (iIdx=0; iIdx<6; iIdx++)
    {
        bDigit = (byte) (ui64MacID >> (iIdx * 8));
        sprintf(acDigit, "%02X", bDigit);
        strMacID += String(acDigit);

        if (fUseMacFormat_p && (iIdx<5))
        {
            strMacID += ":";
        }
    }

    strMacID.toUpperCase();

    return (strMacID);

}





//=========================================================================//
//                                                                         //
//          D E B U G   F U N C T I O N S                                  //
//                                                                         //
//=========================================================================//

//---------------------------------------------------------------------------
//  DEBUG: Dump Buffer
//---------------------------------------------------------------------------

#ifdef DEBUG_DUMP_BUFFER

void  DebugDumpBuffer (String strBuffer_p)
{

int            iBufferLen = strBuffer_p.length();
unsigned char  abDataBuff[iBufferLen];

    strBuffer_p.getBytes(abDataBuff, iBufferLen);
    DebugDumpBuffer(abDataBuff, strBuffer_p.length());

    return;

}

//---------------------------------------------------------------------------

void  DebugDumpBuffer (const void* pabDataBuff_p, unsigned int uiDataBuffLen_p)
{

#define COLUMNS_PER_LINE    16

const unsigned char*  pabBuffData;
unsigned int          uiBuffSize;
char                  szLineBuff[128];
unsigned char         bData;
int                   nRow;
int                   nCol;

    // get pointer to buffer and length of buffer
    pabBuffData = (const unsigned char*)pabDataBuff_p;
    uiBuffSize  = (unsigned int)uiDataBuffLen_p;


    // dump buffer contents
    for (nRow=0; ; nRow++)
    {
        sprintf(szLineBuff, "\n%04lX:   ", (unsigned long)(nRow*COLUMNS_PER_LINE));
        Serial.print(szLineBuff);

        for (nCol=0; nCol<COLUMNS_PER_LINE; nCol++)
        {
            if ((unsigned int)nCol < uiBuffSize)
            {
                sprintf(szLineBuff, "%02X ", (unsigned int)*(pabBuffData+nCol));
                Serial.print(szLineBuff);
            }
            else
            {
                Serial.print("   ");
            }
        }

        Serial.print(" ");

        for (nCol=0; nCol<COLUMNS_PER_LINE; nCol++)
        {
            bData = *pabBuffData++;
            if ((unsigned int)nCol < uiBuffSize)
            {
                if ((bData >= 0x20) && (bData < 0x7F))
                {
                    sprintf(szLineBuff, "%c", bData);
                    Serial.print(szLineBuff);
                }
                else
                {
                    Serial.print(".");
                }
            }
            else
            {
                Serial.print(" ");
            }
        }

        if (uiBuffSize > COLUMNS_PER_LINE)
        {
            uiBuffSize -= COLUMNS_PER_LINE;
        }
        else
        {
            break;
        }

        Serial.flush();     // give serial interface time to flush data
    }

    Serial.print("\n");

    return;

}

#endif




// EOF
