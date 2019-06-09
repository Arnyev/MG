using System;

namespace MG
{
    internal class MatrixCalculationHelper
    {
        public static void InverseMatrix(float[][] matrix, out float[][] inversed)
        {
            int length = matrix.Length;
            inversed = new float[length][];
            float[][] numArray = CopyMatrix(matrix);
            LuExpansion(length, ref numArray, ref numArray, out int[] rc);
            int max = length - 1;
            float[] a = new float[length];
            for (int i = 0; i < length; ++i)
            {
                for (int index = 0; index <= max; ++index)
                    a[index] = 0.0f;
                a[i] = 1.0f;

                LuToArray(length, numArray, rc, ref a);
                BackPassage(length, numArray, a, out inversed[i]);
            }
            SelfTranspose(max, ref inversed);
        }

        private static int[] Numbers(int len)
        {
            int[] numArray = new int[len];
            for (int index = 0; index < len; ++index)
                numArray[index] = index;
            return numArray;
        }

        private static void LuExpansion(int len, ref float[][] m, ref float[][] lu, out int[] rc)
        {
            rc = Numbers(len);
            int num1 = len - 1;
            for (int index1 = 1; index1 <= num1; ++index1)
            {
                int row = index1 - 1;
                if (m[row][row] == 0.0)
                {
                    int index2 = -1;
                    for (int index3 = row; index3 <= num1; ++index3)
                    {
                        if (m[index3][row] != 0.0)
                        {
                            index2 = index3;
                            break;
                        }
                    }
                    if (index2 < 0)
                        throw new ArgumentException();
                    if (index2 != row)
                    {
                        float[] numArray = m[row];
                        m[row] = m[index2];
                        m[index2] = numArray;
                        rc[row] = index2;
                    }
                }
                for (int index2 = index1; index2 <= num1; ++index2)
                {
                    float num2 = m[index2][row] / m[row][row];
                    for (int index3 = row; index3 <= num1; ++index3)
                        m[index2][index3] = m[index2][index3] - m[row][index3] * num2;
                    lu[index2][row] = num2;
                }
            }
        }

        private static void BackPassage(int len, float[][] m, float[] a, out float[] x)
        {
            x = new float[len];
            int index1 = len - 1;
            x[index1] = m[index1][index1] != 0.0f ? a[index1] / m[index1][index1] : 0.0f;
            for (int index2 = index1 - 1; index2 >= 0; --index2)
            {
                float num = 0.0f;
                for (int index3 = index2 + 1; index3 <= index1; ++index3)
                    num += m[index2][index3] * x[index3];
                x[index2] = (a[index2] - num) / m[index2][index2];
            }
        }

        private static void SelfTranspose(int max, ref float[][] m)
        {
            for (int index1 = 0; index1 <= max; ++index1)
            {
                for (int index2 = index1 + 1; index2 <= max; ++index2)
                {
                    float num = m[index1][index2];
                    m[index1][index2] = m[index2][index1];
                    m[index2][index1] = num;
                }
            }
        }

        private static void LuToArray(int len, float[][] lu, int[] rc, ref float[] a)
        {
            int num1 = len - 1;
            for (int index1 = 1; index1 <= num1; ++index1)
            {
                int index2 = index1 - 1;
                if (rc[index1] != index1)
                {
                    float num2 = a[index1];
                    a[index1] = a[rc[index1]];
                    a[rc[index1]] = num2;
                }
                for (int index3 = index1; index3 <= num1; ++index3)
                    a[index3] = a[index3] - a[index2] * lu[index3][index2];
            }
        }

        private static float[][] CopyMatrix(float[][] source)
        {
            int length = source.Length;
            var rowLength = source[0].Length;
            var numArray = new float[length][];
            for (int index = 0; index < length; ++index)
            {
                numArray[index] = new float[rowLength];
                for (int j = 0; j < rowLength; ++j)
                    numArray[index][j] = source[index][j];
            }
            return numArray;
        }
    }
}
