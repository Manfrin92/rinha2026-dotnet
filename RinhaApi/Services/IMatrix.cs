namespace RinhaApi.Services
{
    public interface IMatrix
    {
        float[,] GetRandomMatrix(int inputDim = 14, int outputDim = 6);
    }
}