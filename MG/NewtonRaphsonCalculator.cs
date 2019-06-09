namespace MG
{
    internal class NewtonRaphsonEquationSolver
    {
        private static void Iteration(EquationSystem system, float[] x0, ref float[] x1)
        {
            var dimension = x0.Length;
            var jacobian = new float[dimension][];
            for (var index = 0; index < dimension; ++index)
                jacobian[index] = new float[dimension];

            var functionValues = new float[dimension];

            for (int i = 0; i < dimension; i++)
                x1[i] = x0[i];

            system.Jacobian(x0, jacobian);
            system.Calculate(x0, functionValues);
            float[][] inversed;

            MatrixCalculationHelper.InverseMatrix(jacobian, out inversed);

            for (int i = 0; i < dimension; i++)
            {
                x1[i] = x0[i];
                for (int j = 0; j < dimension; j++)
                    x1[i] -= inversed[i][j] * functionValues[j];
            }
        }

        public static bool Solve(EquationSystem system, float[] x0, out float[] x, int iterations, float precision, out int iterationsUsed)
        {
            var dimension = x0.Length;
            var x01 = new float[dimension];
            var y = new float[dimension];
            x = new float[dimension];
            x0.CopyTo(x01, 0);
            iterationsUsed = iterations;

            for (int index = 1; index <= iterations; ++index)
            {
                Iteration(system, x01, ref x);
                system.Calculate(x,  y);

                float num = 0;
                for (int i = 0; i < y.Length; i++)
                    num += y[i] * y[i];

                if (num <= precision)
                {
                    iterationsUsed = index - 1;
                    return true;
                }

                x.CopyTo(x01, 0);
            }
            return false;
        }
    }
}
