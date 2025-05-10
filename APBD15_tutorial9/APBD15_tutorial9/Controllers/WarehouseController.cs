using System.ComponentModel.DataAnnotations;
using APBD15_tutorial9.Models;
using APBD15_tutorial9.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD15_tutorial9.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _service;
    
    public WarehouseController(IWarehouseService service)
    {
        _service = service;
    }
    
    [HttpPost]
    public async Task<IActionResult> AcceptData([FromBody][Required] ProductWarehouseDto productWarehouseDto, CancellationToken cancellationToken)
    {
        var (status, idResult) = await _service.AcceptData(productWarehouseDto, cancellationToken);

        return status switch
        {
            "LessThanZero" => BadRequest("IdProduct or IdWarehouse less than zero. Or amount less or equal to zero"),
            "IdProduct" => NotFound("Product with provided ID does not exist!"),
            "IdWarehouse" => NotFound("Warehouse with provided ID does not exist!"),
            "OrderDoesNotExist" => NotFound("No record found in Order table with IdProduct and Amount from request and CreatedAt provided."),
            "OrderAlreadyFulfilled" => Conflict("Order is already fulfilled!"),
            "Success" => CreatedAtAction(nameof(AcceptData), new {id = idResult}),
            _ => StatusCode(500, "An unknown error occurred while processing the request.")
        };
    }
    
    [HttpPost("procedure")]
    public async Task<IActionResult> AcceptDataWithProcedure([FromBody][Required] ProductWarehouseDto productWarehouseDto, CancellationToken cancellationToken)
    {
        var (status, idResult) = await _service.AcceptDataWithProcedure(productWarehouseDto, cancellationToken);
        return status switch
        {
            "LessThanZero" => BadRequest("IdProduct or IdWarehouse less than zero. Or amount less or equal to zero"),
            "IdProduct" => NotFound("Product with provided ID does not exist!"),
            "IdWarehouse" => NotFound("Warehouse with provided ID does not exist!"),
            "OrderDoesNotExist" => NotFound("No record found in Order table with IdProduct and Amount from request and CreatedAt provided."),
            "OrderAlreadyFulfilled" => Conflict("Order is already fulfilled!"),
            "Success" => CreatedAtAction(nameof(AcceptDataWithProcedure), new {id = idResult}),
            _ => StatusCode(500, "An unknown error occurred while processing the request.")
        };
    }
}