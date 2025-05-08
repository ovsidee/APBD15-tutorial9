using System.ComponentModel.DataAnnotations;
using APBD15_tutorial9.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD15_tutorial9.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IConfiguration _configuration;

    public WarehouseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> AcceptData([FromBody][Required] ProductWarehouseDto productWarehouseDto, CancellationToken cancellationToken)
    {
        if (productWarehouseDto.IdProduct < 0 || productWarehouseDto.IdWarehouse < 0 || productWarehouseDto.Amount <= 0)
        {
            return "LessThanZero";
        } 
        
        await using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("ConnectionString"));
        await using SqlCommand com = new SqlCommand();
        com.Connection = con;
        
        //--------------------------------------------1
        com.CommandText = @"SELECT COUNT(*) FROM Product WHERE IdProduct = @IdProduct;
                            SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @IdWarehouse;";
        com.Parameters.AddWithValue("IdProduct", productWarehouseDto.IdProduct);
        com.Parameters.AddWithValue("IdWarehouse", productWarehouseDto.IdWarehouse);
        
        await con.OpenAsync(cancellationToken);
        await using var reader = await com.ExecuteReaderAsync(cancellationToken);

        //check idProduct
        reader.ReadAsync(cancellationToken);
        if ((int)reader[0] == 0)
            return "IdProduct";

        //check IdWarehouse
        reader.NextResultAsync(cancellationToken);
        reader.ReadAsync(cancellationToken);
        if ((int)reader[0] == 0)
            return "IdWarehouse";

        //--------------------------------------------2
        com.CommandText = @"SELECT COUNT(*)
                            FROM Order 
                            WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt;";
        com.Parameters.Clear();
        com.Parameters.AddWithValue("IdProduct", productWarehouseDto.IdProduct);
        com.Parameters.AddWithValue("Amount", productWarehouseDto.Amount);
        com.Parameters.AddWithValue("CreatedAt", productWarehouseDto.CreatedAt);
        
        await reader.ReadAsync(cancellationToken);
        if ((int)reader[0] == 0)
            return "OrderDoesNotExist";
        
        //--------------------------------------------3
        com.CommandText = @"SELECT COUNT(*) 
                            FROM Product_Warehouse 
                            WHERE IdOrder = (
                                SELECT TOP 1 IdOrder FROM Order 
                                WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt
                            );
                            ";
        com.Parameters.Clear();
        com.Parameters.AddWithValue("IdProduct", productWarehouseDto.IdProduct);
        com.Parameters.AddWithValue("Amount", productWarehouseDto.Amount);
        com.Parameters.AddWithValue("CreatedAt", productWarehouseDto.CreatedAt);
        
        await reader.ReadAsync(cancellationToken);
        if ((int)reader[0] > 0)
            return "OrderAlreadyFulfilled";
        
        //--------------------------------------------4
        com.CommandText = @"UPDATE Order 
                            SET FullfilledAt = @TimeNow 
                            WHERE IdProduct = @IdProduct;";
        com.Parameters.Clear();
        com.Parameters.AddWithValue("TimeNow", DateTime.Now);
        com.Parameters.AddWithValue("IdProduct", productWarehouseDto.IdProduct);
        
        await com.ExecuteNonQueryAsync(cancellationToken);
        
        //--------------------------------------------5
        com.CommandText = @"SELECT Price FROM Product WHERE IdProduct = @IdProduct;";
        com.Parameters.Clear();
        com.Parameters.AddWithValue("IdProduct", productWarehouseDto.IdProduct);
        
        var priceResult = await com.ExecuteScalarAsync(cancellationToken);
        decimal unitPrice = (decimal)priceResult;
        decimal totalPrice = unitPrice * productWarehouseDto.Amount;
        //TODO 5-th (insert to Product_Warehouse) and 6-th step (lecture)
        
        return "Success";
    }
}