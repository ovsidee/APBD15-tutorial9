using APBD15_tutorial9.Models;

namespace APBD15_tutorial9.Services;

public interface IWarehouseService
{
    public Task<(string status, decimal? idResult)> AcceptData(ProductWarehouseDto productWarehouseDto, CancellationToken cancellationToken);
    public Task<(string status, decimal? idResult)> AcceptDataWithProcedure(ProductWarehouseDto productWarehouseDto, CancellationToken cancellationToken);
}