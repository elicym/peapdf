#pragma once

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <windows.h>
#include "../lcms/include/lcms2.h"

typedef unsigned char byte;
typedef unsigned int uint;

#define DLL_EXPORT extern "C" __declspec(dllexport)

extern cmsHTRANSFORM transform;