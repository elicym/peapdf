/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */


#include "pch.h"

//The following code allows an array to be used as the JPX source, some was adapted from https://groups.google.com/g/openjpeg/c/8cebr0u7JgY/m/hc5k6r_LDAAJ.

typedef struct
{
	OPJ_UINT8* pData;
	OPJ_SIZE_T dataSize;
	OPJ_SIZE_T offset;
} opj_input_memory_stream;

static OPJ_SIZE_T opj_input_memory_stream_read(void* p_buffer, OPJ_SIZE_T p_nb_bytes, void* p_user_data)
{
	opj_input_memory_stream* l_stream = (opj_input_memory_stream*)p_user_data;
	OPJ_SIZE_T l_nb_bytes_read = p_nb_bytes;

	if (l_stream->offset >= l_stream->dataSize) {
		return (OPJ_SIZE_T)-1;
	}
	if (p_nb_bytes > (l_stream->dataSize - l_stream->offset)) {
		l_nb_bytes_read = l_stream->dataSize - l_stream->offset;
	}
	memcpy(p_buffer, &(l_stream->pData[l_stream->offset]), l_nb_bytes_read);
	l_stream->offset += l_nb_bytes_read;
	return l_nb_bytes_read;
}

static OPJ_OFF_T opj_input_memory_stream_skip(OPJ_OFF_T p_nb_bytes, void* p_user_data)
{
	opj_input_memory_stream* l_stream = (opj_input_memory_stream*)p_user_data;

	if (p_nb_bytes < 0) {
		return -1;
	}

	l_stream->offset += (OPJ_SIZE_T)p_nb_bytes;

	return p_nb_bytes;
}

static OPJ_BOOL opj_input_memory_stream_seek(OPJ_OFF_T p_nb_bytes, void* p_user_data)
{
	opj_input_memory_stream* l_stream = (opj_input_memory_stream*)p_user_data;

	if (p_nb_bytes < 0) {
		return OPJ_FALSE;
	}

	l_stream->offset = (OPJ_SIZE_T)p_nb_bytes;

	return OPJ_TRUE;
}

static void opj_input_memory_stream_free(void* p_user_data)
{
}

DLL_EXPORT void __stdcall DecodeJPX(byte* input, int inputC, byte* output) {

	//the following code is adapted from openjpeg's opj_decompress project
	opj_image_t* image = NULL;
	opj_stream_t* l_stream = NULL;              /* Stream */
	opj_codec_t* l_codec = NULL;                /* Handle to a decompressor */
	opj_codestream_index_t* cstr_index = NULL;

	l_stream = opj_stream_default_create(TRUE);
	opj_stream_set_read_function(l_stream, opj_input_memory_stream_read);
	opj_stream_set_seek_function(l_stream, opj_input_memory_stream_seek);
	opj_stream_set_skip_function(l_stream, opj_input_memory_stream_skip);
	opj_input_memory_stream streamUserData{ input, (OPJ_SIZE_T)inputC };
	opj_stream_set_user_data(l_stream, &streamUserData, opj_input_memory_stream_free);
	opj_stream_set_user_data_length(l_stream, streamUserData.dataSize);

	/* Get a decoder handle */
	l_codec = opj_create_decompress(OPJ_CODEC_JP2);

	/* Setup the decoder decoding parameters using user parameters */
	opj_dparameters_t parameters{};
	if (!opj_setup_decoder(l_codec, &parameters)) {
		fprintf(stderr, "ERROR -> opj_decompress: failed to setup the decoder\n");
		opj_stream_destroy(l_stream);
		opj_destroy_codec(l_codec);
		return;
	}

	/* Read the main header of the codestream and if necessary the JP2 boxes*/
	if (!opj_read_header(l_stream, l_codec, &image)) {
		fprintf(stderr, "ERROR -> opj_decompress: failed to read the header\n");
		opj_stream_destroy(l_stream);
		opj_destroy_codec(l_codec);
		opj_image_destroy(image);
		return;
	}

	if (!opj_set_decode_area(l_codec, image, (OPJ_INT32)parameters.DA_x0,
		(OPJ_INT32)parameters.DA_y0, (OPJ_INT32)parameters.DA_x1,
		(OPJ_INT32)parameters.DA_y1)) {
		fprintf(stderr, "ERROR -> opj_decompress: failed to set the decoded area\n");
		opj_stream_destroy(l_stream);
		opj_destroy_codec(l_codec);
		opj_image_destroy(image);
		return;
	}

	/* Get the decoded image */
	if (!(opj_decode(l_codec, l_stream, image) &&
		opj_end_decompress(l_codec, l_stream))) {
		fprintf(stderr, "ERROR -> opj_decompress: failed to decode image!\n");
		opj_destroy_codec(l_codec);
		opj_stream_destroy(l_stream);
		opj_image_destroy(image);
		return;
	}

	/* FIXME? Shouldn't that situation be considered as an error of */
	/* opj_decode() / opj_get_decoded_tile() ? */
	if (image->comps[0].data == NULL) {
		fprintf(stderr, "ERROR -> opj_decompress: no image data!\n");
		opj_destroy_codec(l_codec);
		opj_stream_destroy(l_stream);
		opj_image_destroy(image);
		return;
	}

	/* Close the byte stream */
	opj_stream_destroy(l_stream);

#pragma warning( push )
#pragma warning( disable : 26812 )
	if (image->color_space != OPJ_CLRSPC_SYCC
		&& image->numcomps == 3 && image->comps[0].dx == image->comps[0].dy
		&& image->comps[1].dx != 1) {
		image->color_space = OPJ_CLRSPC_SYCC;
	}
	else if (image->numcomps <= 2) {
		image->color_space = OPJ_CLRSPC_GRAY;
	}
#pragma warning( pop )

	if (image->icc_profile_buf) {
#if defined(OPJ_HAVE_LIBLCMS1) || defined(OPJ_HAVE_LIBLCMS2)
		if (image->icc_profile_len) {
			color_apply_icc_profile(image);
		}
		else {
			color_cielab_to_rgb(image);
		}
#endif
		free(image->icc_profile_buf);
		image->icc_profile_buf = NULL;
		image->icc_profile_len = 0;
	}

	int w = (int)image->comps[0].w, h = (int)image->comps[0].h;

	if (image->numcomps == 1) {
		int adjustR = image->comps[0].prec - 8;

		for (int i = 0; i < w * h; i++) {
			int r;

			r = image->comps[0].data[i];
			r += (image->comps[0].sgnd ? 1 << (image->comps[0].prec - 1) : 0);
			if (adjustR > 0) {
				r = ((r >> adjustR) + ((r >> (adjustR - 1)) % 2));
			}
			if (r > 255) {
				r = 255;
			}
			else if (r < 0) {
				r = 0;
			}

			output[i * 4 + 0] = r;
			output[i * 4 + 1] = r;
			output[i * 4 + 2] = r;
			output[i * 4 + 3] = 255;
		}

	}
	else if (image->numcomps == 3) {
		int adjustR = image->comps[0].prec - 8,
			adjustG = image->comps[1].prec - 8,
			adjustB = image->comps[2].prec - 8;
		for (int i = 0; i < w * h; i++) {
			OPJ_UINT8 rc, gc, bc;
			int r, g, b;

			r = image->comps[0].data[i];
			r += (image->comps[0].sgnd ? 1 << (image->comps[0].prec - 1) : 0);
			if (adjustR > 0) {
				r = ((r >> adjustR) + ((r >> (adjustR - 1)) % 2));
			}
			if (r > 255) {
				r = 255;
			}
			else if (r < 0) {
				r = 0;
			}
			rc = (OPJ_UINT8)r;

			g = image->comps[1].data[i];
			g += (image->comps[1].sgnd ? 1 << (image->comps[1].prec - 1) : 0);
			if (adjustG > 0) {
				g = ((g >> adjustG) + ((g >> (adjustG - 1)) % 2));
			}
			if (g > 255) {
				g = 255;
			}
			else if (g < 0) {
				g = 0;
			}
			gc = (OPJ_UINT8)g;

			b = image->comps[2].data[i];
			b += (image->comps[2].sgnd ? 1 << (image->comps[2].prec - 1) : 0);
			if (adjustB > 0) {
				b = ((b >> adjustB) + ((b >> (adjustB - 1)) % 2));
			}
			if (b > 255) {
				b = 255;
			}
			else if (b < 0) {
				b = 0;
			}
			bc = (OPJ_UINT8)b;

			output[i * 4 + 0] = rc;
			output[i * 4 + 1] = gc;
			output[i * 4 + 2] = bc;
			output[i * 4 + 3] = 255;

		}

	}

	/* free remaining structures */
	if (l_codec) {
		opj_destroy_codec(l_codec);
	}


	/* free image data structure */
	opj_image_destroy(image);

	/* destroy the codestream index */
	opj_destroy_cstr_index(&cstr_index);

}