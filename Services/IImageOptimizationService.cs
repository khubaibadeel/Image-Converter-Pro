using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    public interface IImageOptimizationService
    {
        OptimizationPlan CreatePlan(ConversionSettings settings);
        Task<QualityComparison> EstimateAsync(string sourcePath, ConversionSettings settings, CancellationToken cancellationToken = default);
    }
}
