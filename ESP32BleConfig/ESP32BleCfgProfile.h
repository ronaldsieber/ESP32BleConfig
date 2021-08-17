/****************************************************************************

  Copyright (c) 2021 Ronald Sieber

  Project:      Project independend / Standard class
  Description:  Class <ESP32BleCfgProfile> Declaration

  -------------------------------------------------------------------------

  Revision History:

  2021/07/06 -rs:   V1.00 Initial version

****************************************************************************/

#ifndef _ESP32BLECFGPROFILE_H_
#define _ESP32BLECFGPROFILE_H_





//---------------------------------------------------------------------------
//  Type Definitions
//---------------------------------------------------------------------------

// Definitions of WIFI Operation Modes
#define WIFI_OPMODE_STA     (1<<0)              // WIFI Station/Client Mode
#define WIFI_OPMODE_AP      (1<<1)              // WIFI AccessPoint Mode


// Data structure to store Configuration Data in EEPROM.
//
// Since the full EEPROM size for the whole application is limited to
// 512 bytes only, the configuration data structure is packed into the
// smallest possible footprint.
//
typedef struct __attribute__((packed))          // sizeof(tAppCfgData) = 185
{

    uint32_t        m_ui32MagicID;

    char            m_szDevMntDevName[32];

    char            m_szWifiSSID[32];           // WIFI Spec: max. Length AP Name: 32 char
    char            m_szWifiPasswd[64];         // WIFI Spec: max. Length Password: 64 char
    char            m_szWifiOwnAddr[24];        // {"192.168.xxx.xxx:12345"}
    uint8_t         m_ui8WifiOwnMode;           // WIFI_OPMODE_STA / WIFI_OPMODE_AP

    struct
    {
        uint8_t     m_fAppRtOpt1 : 1;
        uint8_t     m_fAppRtOpt2 : 1;
        uint8_t     m_fAppRtOpt3 : 1;
        uint8_t     m_fAppRtOpt4 : 1;
        uint8_t     m_fAppRtOpt5 : 1;
        uint8_t     m_fAppRtOpt6 : 1;
        uint8_t     m_fAppRtOpt7 : 1;
        uint8_t     m_fAppRtOpt8 : 1;
    };
    char            m_szAppRtPeerAddr[24];      // {"192.168.xxx.xxx:12345"}

    uint32_t        m_ui32Crc32;

} tAppCfgData;



// Data structure for assigning Descriptor Text ('Labels') resp. FeatureLists to the BLE Characteristics
typedef struct
{

    uint8_t         m_ui8OwnModeFeatList;
    const char*     m_pszLabelOpt1;
    const char*     m_pszLabelOpt2;
    const char*     m_pszLabelOpt3;
    const char*     m_pszLabelOpt4;
    const char*     m_pszLabelOpt5;
    const char*     m_pszLabelOpt6;
    const char*     m_pszLabelOpt7;
    const char*     m_pszLabelOpt8;
    const char*     m_pszLabelPeerAddr;

} tAppDescriptData;


// Application Callback Handler used by BLE Profile Implementation
typedef  void  (*tCbHdlrSaveConfig) (const tAppCfgData* pAppCfgData_p);
typedef  void  (*tCbHdlrRestartDev) ();
typedef  void  (*tCbHdlrConStatChg) (bool fBleClientConnected_p);





/***************************************************************************/
/*                                                                         */
/*                                                                         */
/*          CLASS  ESP32BleCfgProfile                                      */
/*                                                                         */
/*                                                                         */
/***************************************************************************/

class  ESP32BleCfgProfile
{

    //-----------------------------------------------------------------------
    //  Definitions
    //-----------------------------------------------------------------------

    public:



    //-----------------------------------------------------------------------
    //  Private Attributes
    //-----------------------------------------------------------------------

    private:



    //-----------------------------------------------------------------------
    //  Public Methodes
    //-----------------------------------------------------------------------

    public:

        ESP32BleCfgProfile();
        ~ESP32BleCfgProfile();

        int   ProfileSetup(uint32_t ui32DeviceType_p, const tAppCfgData* pAppCfgData_p, const tAppDescriptData* pAppDescriptData_p, tCbHdlrSaveConfig pfnAppCbHdlrSaveConfig_p, tCbHdlrRestartDev pfnAppCbHdlrRestartDev_p, tCbHdlrConStatChg pfnAppCbHdlrConStatChg_p);
        bool  ProfileLoop();
        bool  IsBleClientConnected();

        static  bool  ReadDataFromBleCharacterisics();
        static  int   ImportInstanceWorkspace(const tAppCfgData* pAppCfgData_p);
        static  int   ExportInstanceWorkspace(tAppCfgData* pAppCfgData_p);



    //-----------------------------------------------------------------------
    //  Private Methodes
    //-----------------------------------------------------------------------

    private:




};



#endif  // _ESP32BLECFGPROFILE_H_
