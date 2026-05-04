namespace RinhaApi.Services
{
    public class Matrix : IMatrix
    {

        private readonly Random RANDOM = new(42); // fixed seed = reproducible

        public float[,] GetRandomMatrix(int inputDim = 14, int outputDim = 6)
        {
            var matrix = new float[inputDim, outputDim];

            for (int i = 0; i < inputDim; i++)
            {
                for (int j = 0; j < outputDim; j++)
                {
                    // random float between -1 and 1
                    matrix[i, j] = (float)(RANDOM.NextDouble() * 2.0 - 1.0);
                }
            }

            return matrix;
        }
    }
}