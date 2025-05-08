using System.ComponentModel.DataAnnotations;
using APBD15_tutorial9.Models;
using Microsoft.AspNetCore.Mvc;

namespace APBD15_tutorial9.Services;

public interface IWarehouseService
{
    public Task<string> AcceptData([FromBody][Required] ProductWarehouseDto productWarehouseDto, CancellationToken cancellationToken);
}