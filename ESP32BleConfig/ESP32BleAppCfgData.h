/****************************************************************************

  Copyright (c) 2021 Ronald Sieber

  Project:      Project independend / Standard class
  Description:  Class <ESP32BleAppCfgData> Declaration

  -------------------------------------------------------------------------

  Revision History:

  2021/07/06 -rs:   V1.00 Initial version

****************************************************************************/

#ifndef _ESP32BLEAPPCFGDATA_H_
#define _ESP32BLEAPPCFGDATA_H_





/***************************************************************************/
/*                                                                         */
/*                                                                         */
/*          CLASS  ESP32BleAppCfgData                                      */
/*                                                                         */
/*                                                                         */
/***************************************************************************/

class  ESP32BleAppCfgData
{

    //-----------------------------------------------------------------------
    //  Definitions
    //-----------------------------------------------------------------------

    public:



    //-----------------------------------------------------------------------
    //  Private Attributes
    //-----------------------------------------------------------------------

    private:

    unsigned int    m_uiEepromSize;



    //-----------------------------------------------------------------------
    //  Public Methodes
    //-----------------------------------------------------------------------

    public:

    ESP32BleAppCfgData (unsigned int uiEepromSize_p);
    ~ESP32BleAppCfgData ();

    int  LoadAppCfgDataFromEeprom (tAppCfgData* pAppCfgData_p);
    int  SaveAppCfgDataToEeprom (tAppCfgData* pAppCfgData_p);
    int  ClearAppCfgDataInEeprom ();



    //-----------------------------------------------------------------------
    //  Private Methodes
    //-----------------------------------------------------------------------

    private:

    static  uint32_t  CalulateCrc32 (const void* pDataBuff_p, int iDataSize_p);



};



#endif  // _ESP32BLEAPPCFGDATA_H_
