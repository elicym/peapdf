/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    partial class ICCProfile
    {
        enum ProfileClass
        {
            Input = 0x73636E72, //'scnr'
            Display = 0x6D6E7472, //'mntr'
            Output = 0x70727472, //'prtr'
            DeviceLink = 0x6C696E6B, //'link'
            ColorSpace = 0x73706163, //'spac'
            Abstract = 0x61627374, //'abst'
            NamedColor = 0x6E6D636C, //'nmcl'
        }
        enum ColorSpace
        {
            nCIEXYZ = 0x58595A20, //'XYZ '
            CIELAB = 0x4C616220, //'Lab '
            CIELUV = 0x4C757620, //'Luv '
            YCbCr = 0x59436272, //'YCbr'
            CIEYxy = 0x59787920, //'Yxy '
            RGB = 0x52474220, //'RGB '
            Gray = 0x47524159, //'GRAY'
            HSV = 0x48535620, //'HSV '
            HLS = 0x484C5320, //'HLS '
            CMYK = 0x434D594B, //'CMYK'
            CMY = 0x434D5920, //'CMY '
            _2Col = 0x32434C52, //'2CLR'
            _3Col = 0x33434C52, //'3CLR'
            _4Col = 0x34434C52, //'4CLR'
            _5Col = 0x35434C52, //'5CLR'
            _6Col = 0x36434C52, //'6CLR'
            _7Col = 0x37434C52, //'7CLR'
            _8Col = 0x38434C52, //'8CLR'
            _9Col = 0x39434C52, //'9CLR'
            _10Col = 0x41434C52, //'ACLR'
            _11Col = 0x42434C52, //'BCLR'
            _12Col = 0x43434C52, //'CCLR'
            _13Col = 0x44434C52, //'DCLR'
            _14Col = 0x45434C52, //'ECLR'
            _15Col = 0x46434C52, //'FCLR'
        }
        enum Platform
        {
            Apple = 0x4150504C, //'APPL'
            Microsoft = 0x4D534654, //'MSFT'
            Silicon = 0x53474920, //'SGI '
            Sun = 0x53554E57, //'SUNW'
        }
        enum RenderingIntent { Perceptual = 0, MediaRelativeColorimetric, Saturation, ICCAbsoluteColorimetric }
        enum TagSignature
        {
            AToB0 = 0x41324230, //'A2B0'
            AToB1 = 0x41324231, //'A2B1'
            AToB2 = 0x41324232, //'A2B2'
            blueMatrixColumn = 0x6258595A, //'bXYZ'
            blueTRC = 0x62545243, //'bTRC'
            BToA0 = 0x42324130, //'B2A0'
            BToA1 = 0x42324131, //'B2A1'
            BToA2 = 0x42324132, //'B2A2'
            BToD0 = 0x42324430, //'B2D0'
            BToD1 = 0x42324431, //'B2D1'
            BToD2 = 0x42324432, //'B2D2'
            BToD3 = 0x42324433, //'B2D3'
            calibrationDateTime = 0x63616C74, //'calt'
            charTarget = 0x74617267, //'targ'
            chromaticAdaptation = 0x63686164, //'chad'
            chromaticity = 0x6368726D, //'chrm'
            colorantOrder = 0x636C726F, //'clro'
            colorantTable = 0x636C7274, //'clrt'
            colorantTableOut = 0x636C6F74, //'clot'
            colorimetricIntentImageState = 0x63696973, //'ciis'
            copyright = 0x63707274, //'cprt'
            deviceMfgDesc = 0x646D6E64, //'dmnd'
            deviceModelDesc = 0x646D6464, //'dmdd'
            DToB0 = 0x44324230, //'D2B0'
            DToB1 = 0x44324231, //'D2B1'
            DToB2 = 0x44324232, //'D2B2'
            DToB3 = 0x44324233, //'D2B3'
            gamut = 0x67616D74, //'gamt'
            grayTRC = 0x6B545243, //'kTRC'
            greenMatrixColumn = 0x6758595A, //'gXYZ'
            greenTRC = 0x67545243, //'gTRC'
            luminance = 0x6C756D69, //'lumi'
            measurement = 0x6D656173, //'meas'
            mediaWhitePoint = 0x77747074, //'wtpt'
            namedColor2 = 0x6E636C32, //'ncl2'
            outputResponse = 0x72657370, //'resp'
            perceptualRenderingIntentGamut = 0x72696730, //'rig0'
            preview0 = 0x70726530, //'pre0'
            preview1 = 0x70726531, //'pre1'
            preview2 = 0x70726532, //'pre2'
            profileDescription = 0x64657363, //'desc'
            profileSequenceDesc = 0x70736571, //'pseq'
            profileSequenceIdentifier = 0x70736964, //'psid'
            redMatrixColumn = 0x7258595A, //'rXYZ'
            redTRC = 0x72545243, //'rTRC'
            saturationRenderingIntentGamut = 0x72696732, //'rig2'
            technology = 0x74656368, //'tech'
            viewingCondDesc = 0x76756564, //'vued'
            viewingConditions = 0x76696577 //'view'
        }
        enum PhosphorColorant { Unknown, ITU_R_BT709_2, SMPTE_RP145, EBU_Tech3213E, P22 }

        struct XYZ
        {
            public float X, Y, Z;

            public XYZ(float x, float y, float z) => (X, Y, Z) = (x, y, z);
        }

    }
}
