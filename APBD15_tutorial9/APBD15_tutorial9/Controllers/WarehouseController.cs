using System.ComponentModel.DataAnnotations;
using APBD15_tutorial9.Models;
using APBD15_tutorial9.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD15_tutorial9.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly WarehouseService _service;
    
    public WarehouseController(WarehouseService service)
    {
        _service = service;
    }
    
    [HttpPost]
    public async Task<IActionResult> AcceptData([FromBody][Required] ProductWarehouseDto productWarehouseDto, CancellationToken cancellationToken)
    {
        var result = await _service.AcceptData(productWarehouseDto, cancellationToken);

        return result switch
        {
            "LessThanZero" => BadRequest("IdProduct or IdWarehouse less than zero. Or amount less or equal to zero"),
            "IdProduct" => BadRequest("Product with provided ID does not exist!"),
            "IdWarehouse" => BadRequest("Warehouse with provided ID does not exist!"),
            "OrderDoesNotExist" => BadRequest("No record found in Order table with IdProduct and Amount from request and CreatedAt provided."),
            "OrderAlreadyFulfilled" => BadRequest("Order is already fulfilled!"),
            "Success" => Ok("success!"),
            _ => StatusCode(500, "An error occurred while registering the client.")
        };
    }
}