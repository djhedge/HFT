// gsencrypt.cpp : 定义 DLL 应用程序的导出函数。
//

#include "stdafx.h"
#include "gsencrypt.h"

#include "des.h"
#include "blowfish.h"

#pragma comment(lib,"libeay32MD.lib")

//-----------------------------------------------------------------------------
char * SerializeData(const char *pszDataSrc, int iSizeSrc, char *pszDataDest, int iSizeDest)
{//序列化
    /*
    static int iSizeLast = 0;
    if (iSizeLast==0)
        iSizeLast = iSizeSrc;

    if (iSizeLast!=iSizeSrc)
    {
        printf("长度错误！%d:%d:%s\n", iSizeLast, iSizeSrc, pszDataSrc);
    }
    */

    memset(pszDataDest, 0, iSizeDest);
    for (int i=0; i<iSizeSrc; i++)
    {
        char szValue[32] = {0};
        _snprintf(szValue, sizeof(szValue)-1, "%02x", (unsigned char)pszDataSrc[i]);
        strcat(pszDataDest, szValue);
        //sscanf("%i",&nNum);//输入0x开头的十六进制数试试
    }

    return pszDataDest;
}
//-----------------------------------------------------------------------------
int gsEncryptBuffer(int pi_iMode, char *pi_pszDataRaw, int pi_iDataRawSize, char *pi_pszKey, char *po_pszDataEncrypt, int pi_iDataEncryptSize)
{//对长度为pi_iDataRawSize的缓冲区pi_pszDataRaw进行加密，密钥是pi_pszKey, 密文放在po_pszDataEncrypt的缓冲区里
    if (pi_iDataEncryptSize<=pi_iDataRawSize) //输出缓冲不足以放下密文
        return -1;

    char szBuffer[8192*2] = {0};
    char *pszDataRaw = NULL;

    if (pi_iDataRawSize<=sizeof(szBuffer)-8-1)
    {//临时缓冲区够入所有待加密数据
        pszDataRaw = szBuffer;
    }
    else
    {
        pszDataRaw = new char[pi_iDataRawSize+1+8];
        if (pszDataRaw==NULL)
            return -2;

        memset(pszDataRaw, 0, pi_iDataRawSize+1+8);
    }

    memcpy(pszDataRaw, pi_pszDataRaw, min(pi_iDataRawSize, sizeof(szBuffer)-1));
    
    char szKey[1024] = {0};
    strncpy(szKey, pi_pszKey, sizeof(szKey)-1);

    int iRetSize = 0;
	if (pi_iMode==EM_ENCRYPEMODE_DES_ECB)
	{//des加密

		DES_cblock stKey = {0};
		DES_string_to_key(szKey, &stKey);

		DES_key_schedule stSchedule = {0};
		if(DES_set_key_checked(&stKey, &stSchedule)!=0)
        {
            printf("DES_set_key_checked error!!\n");
            if (pszDataRaw!=szBuffer)
                delete []pszDataRaw;
            return -3;
        }

        //	注意, DES_ecb_encrypt
        //在 OpenSSL 中 ECB 操作模式对应的函数是 DES_ecb_encrypt() ，该函数把一个 8 字节明文分组 input 加密成为一个 8 字节密文分组 output 。参数中密钥结构 ks 是用函数 DES_set_key() 准备好的，而密钥 key 是用随机数算法产生的 64 个随机比特。参数 enc 指示是加密还是解密。该函数每次只加密一个分组，因此用来加密很多数据时不方便使用。
        //void DES_ecb_encrypt(const_DES_cblock *input,DES_cblock *output, DES_key_schedule *ks,int enc);
        //int DES_set_key(const_DES_cblock *key,DES_key_schedule *schedule);
        //
        //1.加密的密文必须是8位的
        //2.只能加密8位，如果有多组，需要反复调用DES_ecb_encrypt
        int iBlockCount;
        if (pi_iDataRawSize%8==0)
            iBlockCount = pi_iDataRawSize/8;
        else
            iBlockCount = (pi_iDataRawSize / 8)+1;
        for (int i=0; i<iBlockCount; i++)
        {
    		DES_ecb_encrypt((DES_cblock *)(pszDataRaw+i*8), (DES_cblock *)(po_pszDataEncrypt+i*8), &stSchedule, DES_ENCRYPT);
        }

        iRetSize = iBlockCount*8;
    }
	else if (pi_iMode==EM_ENCRYPEMODE_BLOWFISH)
	{//blowfish 加密
		BF_KEY stBF_Key = {0};                
		char ivec[32] = {0};

		BF_set_key(&stBF_Key, strlen(szKey), (unsigned char *)szKey);
		int nNum = 0;
		BF_cfb64_encrypt((unsigned char *)pszDataRaw, (unsigned char *)po_pszDataEncrypt, pi_iDataRawSize, 
			&stBF_Key, (unsigned char *)&ivec, &nNum, BF_ENCRYPT);

        iRetSize = pi_iDataRawSize;
	}
	else
    {
        if (pszDataRaw!=szBuffer)
            delete []pszDataRaw;
		return -1; //不支持的加密方式
    }

    if (pszDataRaw!=szBuffer)
        delete []pszDataRaw;

    return iRetSize;
}
//-----------------------------------------------------------------------------
extern "C" int __stdcall gsEncrypt(int pi_iMode, char *pi_pszDataRaw, int pi_iDataRawSize, char *pi_pszKey, char *po_pszDataEncrypt, int pi_iDataEncryptSize)
{//对长度为pi_iDataRawSize的缓冲区pi_pszDataRaw进行加密，密钥是pi_pszKey, 密文放在po_pszDataEncrypt的缓冲区里
    if (pi_iDataEncryptSize<=pi_iDataRawSize*2) //输出缓冲不足以放下密文
        return -1;

    char szBuffer[8192*2] = {0};
    char *pszDataRaw = NULL;
    int iDataSize = 0;

    if (pi_iDataRawSize<=sizeof(szBuffer)-8-1)
    {//临时缓冲区够入所有待加密数据
        pszDataRaw = szBuffer;
        iDataSize = sizeof(szBuffer);
    }
    else
    {
        iDataSize = pi_iDataRawSize+1+8;
        pszDataRaw = new char[iDataSize];
        if (pszDataRaw==NULL)
            return -2;

        memset(pszDataRaw, 0, iDataSize);
    }

    memset(po_pszDataEncrypt, 0, pi_iDataEncryptSize);
    int iRetSize = gsEncryptBuffer(pi_iMode, pi_pszDataRaw, pi_iDataRawSize, pi_pszKey, pszDataRaw, iDataSize);
    if (iRetSize>0)
    {
        //序列化
        SerializeData(pszDataRaw, iRetSize, po_pszDataEncrypt, pi_iDataEncryptSize);
    }

    if (pszDataRaw!=szBuffer)
        delete []pszDataRaw;

    return 0;
}
//-----------------------------------------------------------------------------
