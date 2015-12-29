// 下列 ifdef 块是创建使从 DLL 导出更简单的
// 宏的标准方法。此 DLL 中的所有文件都是用命令行上定义的 GSENCRYPT_EXPORTS
// 符号编译的。在使用此 DLL 的
// 任何其他项目上不应定义此符号。这样，源文件中包含此文件的任何其他项目都会将
// GSENCRYPT_API 函数视为是从 DLL 导入的，而此 DLL 则将用此宏定义的
// 符号视为是被导出的。
#ifdef GSENCRYPT_EXPORTS
#define GSENCRYPT_API __declspec(dllexport)
#else
#define GSENCRYPT_API __declspec(dllimport)
#endif

#define STDCALL __stdcall


//加密方式，从FIX协议里扩展而来，如有新的修改，请遵守FIX协议约定
enum em_EncryptMode { 
	EM_ENCRYPEMODE_DES_ECB=2,
	EM_ENCRYPEMODE_BLOWFISH=101
};

/*
	报文加密函数，适用于各类密码加密 add by luosj 20111108

	参数说明：
	int pi_iMode                  加密方式, 见em_EncryptMode
	char *pi_pszPasswordIn        密码明文
	int pi_iDataRawSize			  密码明文长度
	char *pi_key                  密钥
	char *po_pszPasswordOut       密码密文
	int pi_iSize                  密码密文缓冲区大小，一般128字节足够

	返回: 0:成功  <0:失败
*/
//extern "C" int __stdcall gsEncrypt(int pi_iMode, char *pi_pszPasswordIn, char *pi_pszKey, char *po_pszPasswordOut, int pi_iSize);
//extern "C" int __stdcall gsEncrypt(int pi_iMode, char *pi_pszDataIn, int pi_iDataInSize, char *pi_pszKey, char *po_pszDataOut, int pi_iSize)
extern "C" int __stdcall gsEncrypt(int pi_iMode, char *pi_pszDataRaw, int pi_iDataRawSize, char *pi_pszKey, char *po_pszDataEncrypt, int pi_iDataEncryptSize);