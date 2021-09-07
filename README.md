# ESP32BleConfig

This framework provides functionalities to configure an ESP32/Arduino application at runtime via Bluetooth. This eliminates the otherwise typical adjustments in the source code, such as entering your own WLAN configuration data or the fixed coding of a target addresses for MQTT brokers directly in the C source code of the sketch. On the one hand, it makes it easier to manage Arduino projects in public repositories without first having to remove private data. On the other hand, one and the same binary can be used for several boards, since the personalization of various parameters only takes place via Bluetooth during runtime.

The framework consists of a code template which is to integrate into the respective ESP32/Arduino project and a configuration tool with a graphical user interface (currently for Windows, possibly later as an Android app). The ESP32/Arduino is the Bluetooth server, the graphical configuration tool acts as a client. The following parameters can be configured with it:

 - WLAN mode (Access Point, Station/Client)
 - Configuration of the WLAN access data (SSID, password)
 - Own IP address of the board (DHCP or static IP)
 - Individual device name of the board
 - Address of a communication partner (e.g. MQTT broker)
 - Up to 8 freely usable runtime options (enable/disable)

The configuration data are persistently stored in the flash of the ESP32 via EEPROM simulation and are therefore retained even after a restart.

![\[Bluetooth Configuration Overview\]](Documentation/ESP32_Bluetooth_Configuration.png)

The [ESP32SmartBoard_MqttSensors_BleCfg](https://github.com/ronaldsieber/ESP32SmartBoard_MqttSensors_BleCfg) project illustrates the integration of the framework into a real ESP32/Arduino application.

## Bluetooth Device Profile

The Configuration Framework is based on a private Bluetooth Device Profile (Generic Attribute Profile, GATT). This includes the 3 services:

 - Device Management
 - WIFI Config
 - App Runtime Options

Each service has several characteristics. These represent the internal states of a BLE device, such as the current values of variables (e.g. SSID name, WLAN password or the uptime of the device). Furthermore, read or write access to a characteristic can also trigger an action, such as saving the configuration data in the EEPROM or causing a restart. Each characteristic can contain one or more descriptors. The descriptors are used to describe the characteristic using appropriate metadata (e.g. textual description of what the characteristic represents). The properties of a characteristic define which operations are permitted for the characteristic (read, write, notify, etc.). The value of a characteristic finally represents the current data of the characteristic.

![\[BLE GATT Service\]](Documentation/BLE GATT Service.png)

All services, characteristics and descriptors each have their own, individual UUID. The complete Bluetooth device profile is described in  [BleProfileDefinition.txt](BleProfileDefinition.txt).

## ESP32/Arduino Part of the Framework

The ESP32/Arduino part of the framework implements the Bluetooth device profile required for the configuration (`class ESP32BleCfgProfile`) and realizes the persistent storage of the configuration data in the EEPROM (`class ESP32BleAppCfgData`). The sketch template [ESP32BleConfig.ino](ESP32BleConfig/ESP32BleConfig.ino) shows the use of the framework in your own applications.

The structure `tAppCfgData AppCfgData_g` defined in the sketch template [ESP32BleConfig.ino](ESP32BleConfig/ESP32BleConfig.ino) initially contains the standard values for the configuration ("factory settings"):

    static tAppCfgData  AppCfgData_g =
    {
        APP_CFGDATA_MAGIC_ID,               // .m_ui32MagicID
    
        APP_DEFAULT_DEVICE_NAME,            // .m_szDevMntDevName
    
        APP_DEFAULT_WIFI_SSID,              // .m_szWifiSSID
        APP_DEFAULT_WIFI_PASSWD,            // .m_szWifiPasswd
        APP_DEFAULT_WIFI_OWNADDR,           // .m_szWifiOwnAddr
        APP_DEFAULT_WIFI_OWNMODE,           // .m_ui8WifiOwnMode
    
        APP_DEFAULT_APP_RT_OPT1,            // .m_fAppRtOpt1 : 1
        APP_DEFAULT_APP_RT_OPT2,            // .m_fAppRtOpt2 : 1
        APP_DEFAULT_APP_RT_OPT3,            // .m_fAppRtOpt3 : 1
        APP_DEFAULT_APP_RT_OPT4,            // .m_fAppRtOpt4 : 1
        APP_DEFAULT_APP_RT_OPT5,            // .m_fAppRtOpt5 : 1
        APP_DEFAULT_APP_RT_OPT6,            // .m_fAppRtOpt6 : 1
        APP_DEFAULT_APP_RT_OPT7,            // .m_fAppRtOpt7 : 1
        APP_DEFAULT_APP_RT_OPT8,            // .m_fAppRtOpt8 : 1
        APP_DEFAULT_APP_RT_PEERADDR         // .m_szAppRtPeerAddr
    };

In the first step, the `setup()` function of the sketch is passing this structure as an in / out parameter to the method `ESP32BleAppCfgData_g.LoadAppCfgDataFromEeprom()`:

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
        Serial.print("-> ERROR: Access to EEPROM failed!");
    }

The method `LoadAppCfgDataFromEeprom()` checks whether the EEPROM already contains valid configuration data. If this is the case (valid signature and CRC), this data is moved to the `AppCfgData_g` structure and returned to the application. If no configuration has yet been carried out (no valid data in the EEPROM), the standard values ("factory settings") are retained.

In the second step, the `setup()` function of the sketch checks whether the configuration mode should be started (here controlled by the flag `fStateBleCfg_g`). If this is the case, the method `ESP32BleCfgProfile_g.ProfileSetup()` creates the corresponding Bluetooth Device Profile and starts the GATT service.


    if ( fStateBleCfg_g )
    {
        ESP32BleCfgProfile_g.ProfileSetup(APP_DEVICE_TYPE,
                                          &AppCfgData_g, &AppDescriptData_g,
                                          AppCbHdlrSaveConfig,
                                          AppCbHdlrRestartDev,
                                          AppCbHdlrConStatChg);
    }

The callback handlers passed to `ProfileSetup()` are called under the following conditions:

| Callback Handler| Meaning |
|--|--|
| AppCbHdlrSaveConfig() | The action *"Save configuration"* was triggered via the Bluetooth Device Profile (write access to Characteristic *"BLE_UUID_DEVMNT_SAVE_CFG_CHARACTRSTC"* |
| AppCbHdlrRestartDev() | The action *"Restart Device"* was triggered via the Bluetooth Device Profile (write access to Characteristic *"BLE_UUID_DEVMNT_RST_DEV_CHARACTRSTC"* |
| AppCbHdlrConStatChg() | The connection status of a client (configuration tool, GUI) has changed (connect / disconnect) |

In the `loop()` function of the sketch, the method `ESP32BleCfgProfile_g.ProfileLoop()` ensures the cyclical allocation of CPU time to the GATT service:

    if ( fStateBleCfg_g )
    {
        ESP32BleCfgProfile_g.ProfileLoop();
    }

## Integration of the Framework in own ESP32/Arduino Applications

To integrate the ESP32/Arduino part of the framework into own applications, the following steps are required:

1. Copy the following source code files into the ESP32/Arduino project:  
- ESP32BleAppCfgData.h  
- ESP32BleAppCfgData.cpp  
- ESP32BleCfgProfile.h  
- ESP32BleCfgProfile.cpp  
  If the line `#define DEBUG` is active in [ESP32BleCfgProfile.cpp](ESP32BleConfig/ESP32BleCfgProfile.cpp), the following two source code files are also required in the ESP32/Arduino project:  
- Trace.h  
- Trace.cpp

2. Include the following header files in the sketch file:  
_#include "ESP32BleCfgProfile.h"  
#include "ESP32BleAppCfgData.h"_

3. Copy the content of the sketch template *ESP32BleConfig.ino* into the own application  
~ or ~
Use the sketch template *ESP32BleConfig.ino* as a starting point for own applications

To enable the configuration via Bluetooth, an explicit activation is required. The flag `fStateBleCfg_g` is used for this in the *ESP32BleConfig.ino* sketch template. In a real sketch, the flag can be set e.g. by querying a button when the system is started. On the *ESP32SmartBoard* (see hardware project [ESP32SmartBoard_PCB](https://github.com/ronaldsieber/ESP32SmartBoard_PCB)), the button _BLE_CFG_ is provided for this:

    const int  PIN_KEY_BLE_CFG = 36;                 // GPIO36 -> Pin02

    pinMode(PIN_KEY_BLE_CFG, INPUT);
    fStateBleCfg_g = !digitalRead(PIN_KEY_BLE_CFG);  // Keys are inverted (1=off, 0=on)
    if ( fStateBleCfg_g )
    {
        ESP32BleCfgProfile_g.ProfileSetup(APP_DEVICE_TYPE,
                                          &AppCfgData_g, &AppDescriptData_g,
                                          AppCbHdlrSaveConfig,
                                          AppCbHdlrRestartDev,
                                          AppCbHdlrConStatChg);
    }

To activate the Bluetooth configuration mode for the example shown above, the following steps are necessary:

1. Press *BLE_CFG* on the *ESP32SmartBoard* and keep it pressed until step 2
2. Press and release the Reset button on the ESP32DevKit
3. Release *BLE_CFG*

The sketch template *ESP32BleConfig.ino* contains code to signal the Bluetooth configuration and connection status by flashing the blue LED on the ESP32DevKit. The code sections are enabled by the configuration section at the beginning of the sketch:

    const int CFG_ENABLE_STATUS_LED = 1;

If the code sections are enabled, the blue LED of the ESP32DevKit flashes slowly to indicate that the device is in configuration mode (`fStateBleCfg_g == TRUE`). After connecting a client (graphical configuration tool), the device changes to flashing quickly.

## Graphical Configuration Tool

The Graphical Configuration Tool acts as a client and connects to the ESP32/Arduino operating as a server via Bluetooth. The tool is implemented as a UWP application (Universal Windows Platform) in Visual Studio 2019. A native Bluetooth subsystem integrated in .NET only exists for UWP. On the other hand, classic Windows Form Applications with Bluetooth are dependent on 3rd party components.

A recommended knowledge base for using the BLE subsystem in .NET/UWP applications is the Microsoft "Bluetooth Low Energy sample":

[https://docs.microsoft.com/en-us/samples/microsoft/windows-universal-samples/bluetoothle/](https://docs.microsoft.com/en-us/samples/microsoft/windows-universal-samples/bluetoothle/)

The PC used for the Configuration Tool must have a suitable Bluetooth interface. Such an interface is typically integrated in laptops. A USB Bluetooth adapter dongle (e.g., TP-Link UB400 Nano) is suitable for desktop PCs.

The implementation of the Graphical Configuration Tool is essentially divided into the following source code files:

 - [Esp32ConfigUwp\MainPage.xaml.cs](Esp32ConfigUwp\MainPage.xaml.cs):
Functionalities of the graphical user interface as well as their connection to the backend

 - [Esp32ConfigUwp\BleDeviceManagement.cs](Esp32ConfigUwp\BleDeviceManagement.cs):
BLE device management functionalities such as scanning of available devices, connect and disconnect

 - [Esp32ConfigUwp\Esp32BleGattServices.cs](Esp32ConfigUwp\Esp32BleGattServices.cs):
Implementation of the specific Bluetooth device profile (`class Esp32BleAppCfgProfile`) with the 3 services *"Device Management"* (`class BleGattServiceDevMnt`), *"WIFI Config"* (`class BleGattServiceWifi`) and *"App Runtime Options"* (`class BleGattServiceAppRt`)

**Deployment for UWP Applications**

On the development PC on which a UWP Application was created with Visual Studio, the generated executable can be started just as easily as any other classic Windows application. In contrast, the distribution of UWP Applications to other devices is somewhat more complex than the distribution of classic Windows Applications. To do this, the UWP Application must be packaged in an installer (*"Project -> Publish -> Create App Packages… -> Sideloading"*, tutorial see [https://docs.microsoft.com/en-us/windows/msix/package/packaging-uwp-apps](https://docs.microsoft.com/en-us/windows/msix/package/packaging-uwp-apps)). This step requires a suitable certificate, in the simplest case a temporary certificate generated by the Visual Studio wizard. Since such a certificate cannot be checked for authenticity by any Root Certificate Authority (CA), the certificate (*.pfx file) must be imported manually into the Windows certificate store in the branch *"Trustworthy People"* on the respective target computer (using the Windows tool *"certlm"*).

![\[Esp32ConfigUwp_ESP32_BLE_Device\]](Documentation/Screenshot_Esp32ConfigUwp_ESP32_BLE_Device_marked.png)

First, the ESP32/Arduino has to be set to Bluetooth configuration mode (for *ESP32SmartBoard* see example above). Then carry out the operating steps described below in the Graphical Configuration Tool:

1. Start the BLE Scan using the *"Start BLE Scan"* button (1). All devices found are listed in the GridView (2).

2. Mark the device to be configured in the GridView (2)

3. Click the *"Connect"* button (3) to connect the Configuration Tool (client) to the ESP32/Arduino (server) to be configured. The current configuration data is read from the Arduino and displayed in the GUI (4).

4. The several parameters can be individually adjusted by editing the entries in the configuration elements (4) accordingly.

5. The modified configuration can then be written back to the Arduino using the *"Save Config"* button (5).

6. With the *"Restart Device"* button (6), a restart of the ESP32/Arduino can be triggered with the new configuration data.

## Customizing the Bluetooth Device Profile

The Configuration Tool reads and displays not only the configuration parameters themselves (value of a characteristic), but for the configuration elements in the *"App Runtime Options"* service (`BLE_UUID_APP_RT_SERVICE`) also the descriptors are read from the ESP32/Arduino. This allows the identifiers of the associated elements in the graphical user interface to be adapted using appropriate strings in the source code of the ESP32/Arduino (customizing). The identifier assigned to the descriptors contains the structure `tAppDescriptData AppDescriptData_g` defined in the sketch template *ESP32BleConfig.ino*:

    static tAppDescriptData  AppDescriptData_g =
    {
        APP_DESCRPT_WIFI_OWNMODE_FEATLIST,          // .m_ui16OwnModeFeatList
    
        APP_LABEL_APP_RT_OPT1,                      // .m_pszLabelOpt1
        APP_LABEL_APP_RT_OPT2,                      // .m_pszLabelOpt2
        APP_LABEL_APP_RT_OPT3,                      // .m_pszLabelOpt3
        APP_LABEL_APP_RT_OPT4,                      // .m_pszLabelOpt4
        APP_LABEL_APP_RT_OPT5,                      // .m_pszLabelOpt5
        APP_LABEL_APP_RT_OPT6,                      // .m_pszLabelOpt6
        APP_LABEL_APP_RT_OPT7,                      // .m_pszLabelOpt7
        APP_LABEL_APP_RT_OPT8,                      // .m_pszLabelOpt8
    
        APP_LABEL_APP_RT_PEERADDR                   // .m_pszLabelPeerAddr
    };

The field `m_ui16OwnModeFeatList` is a bit mask that defines which WIFI modes the ESP32/Arduino can support (`WIFI_MODE_STA` = Station/Client, `WIFI_MODE_AP` = Access Point):

    #define  APP_DESCRPT_WIFI_OWNMODE_FEATLIST  (WIFI_MODE_STA | WIFI_MODE_AP)

If an identifier in the ESP32/Arduino sketch starts with a *'#'* character, then the associated element in the Graphical Configuration Tool is disabled.

    #define  APP_LABEL_APP_RT_OPTx  "# (not used)"  // Start with '#' -> disable in GUI

![\[Customizing_GUI_Label\]](Documentation/Customizing_GUI_Label.png)


## Used Third Party Components

No third-party components are used. Both BLE and EEPROM support are installed along with the Arduino ESP32 add-on.


## Practice Notes

Changes, extensions or adjustments of the Bluetooth Device Profile usually affect both the server and the client side. In practice, it has proven useful to first modify and test the server implementation of the ESP32/Arduino. The free Android App *"nRFConnect"* from Nordic Semiconductor is suitable as a universal Bluetooth client for the test:

[https://github.com/NordicSemiconductor/Android-nRF-Connect](https://github.com/NordicSemiconductor/Android-nRF-Connect)



