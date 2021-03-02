/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

#include "pch.h"

DLL_EXPORT void __stdcall CMYK_Setup(byte* profileBytes, int profileBytesC) {
	auto cmykProfile = cmsOpenProfileFromMem(profileBytes, profileBytesC);
	auto rgbProfile = cmsCreate_sRGBProfile();
	transform = cmsCreateTransform(cmykProfile, TYPE_CMYK_8, rgbProfile, TYPE_RGBA_8, INTENT_PERCEPTUAL, 0);
	cmsCloseProfile(cmykProfile);
	cmsCloseProfile(rgbProfile);
}

DLL_EXPORT void __stdcall CMYK2RGB(byte* input, int pixelC, byte* output) {
	cmsDoTransform(transform, input, output, pixelC);
}

DLL_EXPORT void __stdcall CMYK2RGB_Single(byte c, byte m, byte y, byte k, byte& r, byte& g, byte& b) {
	byte input[]{ c,m,y,k }, output[3];
	cmsDoTransform(transform, input, output, 1);
	r = output[0]; g = output[1]; b = output[2];
}
