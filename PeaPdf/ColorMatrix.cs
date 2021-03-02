/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    //For CalRGB. To be improved, but is used rarely, and from what I've soon, for logos.
    class ColorMatrix
    {

        float[,] numbers;

        public ColorMatrix(float[,] numbers)
        {
            this.numbers = numbers;
        }

        public ColorMatrix(float[] numbers, int width, int height)
        {
            this.numbers = new float[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    this.numbers[i, j] = numbers[i * width + j];
                }
            }
        }

        public float[] MultipleVectorWith(float[] vector)
        {
            var res = new float[3];
            for (int j = 0; j < 3; j++)
            {
                float d = 0;
                for (int e = 0; e < 3; e++)
                {
                    d += vector[e] * numbers[e, j];
                }
                res[j] = d;
            }
            return res;
        }

        public float[] MultipleWithVector(float[] vector)
        {
            var res = new float[3];
            for (int j = 0; j < 3; j++)
            {
                float d = 0;
                for (int e = 0; e < 3; e++)
                {
                    d += vector[e] * numbers[j, e];
                }
                res[j] = d;
            }
            return res;
        }

        public static ColorMatrix sRGB = new ColorMatrix(new float[,] { { 3.2404542f, -1.5371385f, -0.4985314f }, { -0.9692660f, 1.8760108f, 0.0415560f }, { 0.0556434f, -0.2040259f, 1.0572252f } });

    }


}
