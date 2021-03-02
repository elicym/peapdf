/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

#include "pch.h"

DLL_EXPORT void __stdcall DecodeDCT(byte* input, int inputC, byte* output) {

	jpeg_decompress_struct info{};
	jpeg_error_mgr jerr{};
	info.err = jpeg_std_error(&jerr);
	jpeg_create_decompress(&info);
	jpeg_mem_src(&info, input, inputC);
	jpeg_read_header(&info, TRUE);
	jpeg_start_decompress(&info);
	unsigned int row_stride = info.output_width * info.output_components,
		pixelC = info.output_height * info.output_width;
#pragma warning( push )
#pragma warning( disable : 6001 6385 )
	byte* decoded = new byte[info.output_height * row_stride];
	while (info.output_scanline < info.output_height) {
		auto rowPtr = decoded + info.output_scanline * row_stride;
		jpeg_read_scanlines(&info, &rowPtr, 1);
	}
	jpeg_finish_decompress(&info);
	switch (info.output_components) {
	case 1:
		for (uint i = 0; i < pixelC; i++)
		{
			output[i * 4 + 0] = decoded[i];
			output[i * 4 + 1] = decoded[i];
			output[i * 4 + 2] = decoded[i];
			output[i * 4 + 3] = 255;
		}
		break;
	case 3:
		for (uint i = 0; i < pixelC; i++)
		{
			output[i * 4 + 0] = decoded[i * 3 + 0];
			output[i * 4 + 1] = decoded[i * 3 + 1];
			output[i * 4 + 2] = decoded[i * 3 + 2];
			output[i * 4 + 3] = 255;
		}
		break;
	case 4:
		cmsDoTransform(transform, decoded, output, pixelC);
		for (uint i = 0; i < pixelC; i++)
		{
			output[i * 4 + 3] = 255;
		}
		break;
	}
#pragma warning( pop )
	jpeg_destroy_decompress(&info);
}
