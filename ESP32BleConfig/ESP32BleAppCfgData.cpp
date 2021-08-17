/****************************************************************************

  Copyright (c) 2021 Ronald Sieber

  Project:      Project independend / Standard class
  Description:  Class <ESP32BleAppCfgData> Implementation

  -------------------------------------------------------------------------

  Revision History:

  2021/07/06 -rs:   V1.00 Initial version

****************************************************************************/


#include "Arduino.h"
#include "EEPROM.h"
#include "ESP32BleCfgProfile.h"         // -> typedef struct tAppCfgData
#include "ESP32BleAppCfgData.h"





/***************************************************************************/
/*                                                                         */
/*                                                                         */
/*          CLASS  ESP32BleAppCfgData                                      */
/*                                                                         */
/*                                                                         */
/***************************************************************************/

/////////////////////////////////////////////////////////////////////////////
//                                                                         //
//          C O N S T R U C T O R   /   D E S T R U C T O R                //
//                                                                         //
/////////////////////////////////////////////////////////////////////////////

//---------------------------------------------------------------------------
//  Constructor
//---------------------------------------------------------------------------

ESP32BleAppCfgData::ESP32BleAppCfgData (
        unsigned int uiEepromSize_p)
{

    m_uiEepromSize = uiEepromSize_p;
    return;

}



//---------------------------------------------------------------------------
//  Destructor
//---------------------------------------------------------------------------

ESP32BleAppCfgData::~ESP32BleAppCfgData ()
{

    return;

}





/////////////////////////////////////////////////////////////////////////////
//                                                                         //
//          P U B L I C    M E T H O D E N                                 //
//                                                                         //
/////////////////////////////////////////////////////////////////////////////

//---------------------------------------------------------------------------
//  LoadAppCfgDataFromEeprom()
//---------------------------------------------------------------------------
//  Return:      1 -> return the previously saved user data
//               0 -> keep default data untouched
//              -1 -> Error (invalid parameter, EEPROM access error)
//---------------------------------------------------------------------------

int  ESP32BleAppCfgData::LoadAppCfgDataFromEeprom (
        tAppCfgData* pAppCfgData_p)
{

tAppCfgData  AppCfgData;
int          iEepromAddr;
uint32_t     ui32AppCfgDataCrc;
uint32_t     ui32Crc;
bool         fCrcMatch;
bool         fRes;
int          iResult;

    if (pAppCfgData_p == NULL)
    {
        return (-1);
    }

    // init EEPROM access
    fRes = EEPROM.begin(m_uiEepromSize);
    if ( !fRes )
    {
        return (-1);
    }

    // try to get Configuration Data from EEPROM
    iEepromAddr = 0;
    memset(&AppCfgData, 0x00, sizeof(AppCfgData));
    EEPROM.get(iEepromAddr, AppCfgData);

    // calculate CRC for data read from EEPROM and compare with saved CRC value
    ui32AppCfgDataCrc = AppCfgData.m_ui32Crc32;
    AppCfgData.m_ui32Crc32 = 0;                                   // restore same state as when calulated CRC for saving data
    ui32Crc = CalulateCrc32(&AppCfgData, sizeof(AppCfgData));
    fCrcMatch = (ui32AppCfgDataCrc == ui32Crc) ? true : false;

    // Configuration Data read from EEPROM are valid?
    // yes -> return the previously saved user data
    // no  -> keep default data untouched
    if ( fCrcMatch )
    {
        memcpy(pAppCfgData_p, &AppCfgData, sizeof(AppCfgData));
        iResult = 1;
    }
    else
    {
        iResult = 0;
    }

    return (iResult);

}



//---------------------------------------------------------------------------
//  SaveAppCfgDataToEeprom()
//---------------------------------------------------------------------------
//  Return:      1 -> user data saved
//              -1 -> Error (invalid parameter, EEPROM access error)
//---------------------------------------------------------------------------

int  ESP32BleAppCfgData::SaveAppCfgDataToEeprom (
        tAppCfgData* pAppCfgData_p)
{

int       iEepromAddr;
uint32_t  ui32Crc;
bool      fRes;

    if (pAppCfgData_p == NULL)
    {
        return (-1);
    }

    // init EEPROM access
    fRes = EEPROM.begin(m_uiEepromSize);
    if ( !fRes )
    {
        return (-1);
    }

    // calculate CRC for data write to EEPROM
    pAppCfgData_p->m_ui32Crc32 = 0;
    ui32Crc = CalulateCrc32(pAppCfgData_p, sizeof(*pAppCfgData_p));
    pAppCfgData_p->m_ui32Crc32 = ui32Crc;

    // save Configuration Data to EEPROM
    iEepromAddr = 0;
    EEPROM.put(iEepromAddr, *pAppCfgData_p);
    EEPROM.commit();

    return (1);

}



//---------------------------------------------------------------------------
//  ClearAppCfgDataInEeprom()
//---------------------------------------------------------------------------
//  Return:      1 -> user data cleared
//              -1 -> Error (invalid parameter, EEPROM access error)
//---------------------------------------------------------------------------

int  ESP32BleAppCfgData::ClearAppCfgDataInEeprom ()
{

tAppCfgData  AppCfgData;
int          iEepromAddr;
bool         fRes;

    // init EEPROM access
    fRes = EEPROM.begin(m_uiEepromSize);
    if ( !fRes )
    {
        return (-1);
    }

    // clear Configuration Data in EEPROM
    memset(&AppCfgData, 0xFF, sizeof(AppCfgData));
    iEepromAddr = 0;
    EEPROM.put(iEepromAddr, AppCfgData);
    EEPROM.commit();

    return (1);

}





/////////////////////////////////////////////////////////////////////////////
//                                                                         //
//          P R I V A T E    M E T H O D E S                               //
//                                                                         //
/////////////////////////////////////////////////////////////////////////////

//---------------------------------------------------------------------------
//  CalulateCrc32
//---------------------------------------------------------------------------
//  Code Orgin: https://lxp32.github.io/docs/a-simple-example-crc32-calculation/
//---------------------------------------------------------------------------

uint32_t  ESP32BleAppCfgData::CalulateCrc32 (
        const void* pDataBuff_p,
        int iDataSize_p)
{

uint32_t  ui32Crc;
char      cDataByte;
uint32_t  fDataBit;
int       iBuffIdx;
int       iBitIdx;

    ui32Crc = 0xFFFFFFFF;

    for (iBuffIdx=0; iBuffIdx<iDataSize_p; iBuffIdx++)
    {
        cDataByte = ((const char*)pDataBuff_p)[iBuffIdx];
        for (iBitIdx=0; iBitIdx<8; iBitIdx++)
        {
            fDataBit = (cDataByte ^ ui32Crc) & 1;
            ui32Crc >>= 1;
            if ( fDataBit )
            {
                ui32Crc = ui32Crc ^ 0xEDB88320;
            }
            cDataByte >>= 1;
        }
    }

    return (~ui32Crc);

}




//  EOF
