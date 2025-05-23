﻿using System.Data;
using APBD15_tutorial9.Models;
using Microsoft.Data.SqlClient;

namespace APBD15_tutorial9.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IConfiguration _configuration;

    public WarehouseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<(string status, decimal? idResult)> AcceptData(ProductWarehouseDto productWarehouseDto, CancellationToken cancellationToken)
    {
        if (productWarehouseDto.IdProduct < 0 || productWarehouseDto.IdWarehouse < 0 || productWarehouseDto.Amount <= 0)
        {
            return ("LessThanZero", null);
        } 
        
        await using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("ConnectionString"));
        
        await con.OpenAsync(cancellationToken);
        
        //--1 (check whether Product and WareHouse exist)
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            com.CommandText = @"SELECT COUNT(*) FROM Product WHERE IdProduct = @IdProduct;
                                     SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @IdWarehouse;";
            com.Parameters.AddWithValue("@IdProduct", productWarehouseDto.IdProduct);
            com.Parameters.AddWithValue("@IdWarehouse", productWarehouseDto.IdWarehouse);

            await using var reader = await com.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken) && (int)reader[0] == 0)
                return ("IdProduct", null);

            await reader.NextResultAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken) && (int)reader[0] == 0)
                return ("IdWarehouse", null);
        }
        
        //---2 (check whether order exists)
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            com.CommandText = @"SELECT COUNT(*) FROM [Order] 
                            WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt;";
            com.Parameters.AddWithValue("IdProduct", productWarehouseDto.IdProduct);
            com.Parameters.AddWithValue("Amount", productWarehouseDto.Amount);
            com.Parameters.AddWithValue("CreatedAt", productWarehouseDto.CreatedAt);
        
            var orderExists = (int)await com.ExecuteScalarAsync(cancellationToken);
            if (orderExists == 0)
                return ("OrderDoesNotExist", null);
        }

        //--3 (check whether order is already fulfilled)
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            com.CommandText = @"SELECT COUNT(*) 
                            FROM Product_Warehouse 
                            WHERE IdOrder = (
                                SELECT IdOrder 
                                FROM [Order] 
                                WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt
                            );";
            com.Parameters.AddWithValue("IdProduct", productWarehouseDto.IdProduct);
            com.Parameters.AddWithValue("Amount", productWarehouseDto.Amount);
            com.Parameters.AddWithValue("CreatedAt", productWarehouseDto.CreatedAt);

            var orderFulfilled = (int)await com.ExecuteScalarAsync(cancellationToken);
            if (orderFulfilled > 0)
                return ("OrderAlreadyFulfilled", null);
        }
        
        //--4 (Update order FulfilledAt column)
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            com.CommandText = @"UPDATE [Order]
                            SET FulfilledAt = @TimeNow 
                            WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt;";
            com.Parameters.AddWithValue("TimeNow", DateTime.Now);
            com.Parameters.AddWithValue("IdProduct", productWarehouseDto.IdProduct);
            com.Parameters.AddWithValue("Amount", productWarehouseDto.Amount);
            com.Parameters.AddWithValue("CreatedAt", productWarehouseDto.CreatedAt);

            await com.ExecuteNonQueryAsync(cancellationToken);
        }
        
        //--5,6 (get product price and calculate total)
        decimal productPrice;
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            com.CommandText = @"SELECT Price FROM Product WHERE IdProduct = @IdProduct;";
            com.Parameters.AddWithValue("IdProduct", productWarehouseDto.IdProduct);
            
            productPrice = (decimal) await com.ExecuteScalarAsync(cancellationToken);
        }
        var totalPriceOfTheProduct = productPrice * productWarehouseDto.Amount;

        //getting order id for inserting
        var idOrderResult = 0;
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            com.CommandText = @"SELECT IdOrder FROM [Order] 
                                WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt;";
            com.Parameters.AddWithValue("IdProduct", productWarehouseDto.IdProduct);
            com.Parameters.AddWithValue("Amount", productWarehouseDto.Amount);
            com.Parameters.AddWithValue("CreatedAt", productWarehouseDto.CreatedAt);

            idOrderResult = (int) await com.ExecuteScalarAsync(cancellationToken);
        }

        decimal idInsertedInDataBase;
        //inserting
        await using (SqlCommand com = new SqlCommand())
        {
            com.Connection = con;
            com.CommandText = @"INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                            VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
                            SELECT SCOPE_IDENTITY();";
            
            com.Parameters.AddWithValue("IdWarehouse", productWarehouseDto.IdWarehouse);
            com.Parameters.AddWithValue("IdProduct", productWarehouseDto.IdProduct);
            com.Parameters.AddWithValue("IdOrder", idOrderResult);
            com.Parameters.AddWithValue("Amount", productWarehouseDto.Amount);
            com.Parameters.AddWithValue("Price", totalPriceOfTheProduct);
            com.Parameters.AddWithValue("CreatedAt", DateTime.Now);
            
            idInsertedInDataBase = (decimal) await com.ExecuteScalarAsync(cancellationToken);
        }
        
        return ("Success", idInsertedInDataBase);
    }

    public async Task<(string status, decimal? idResult)> AcceptDataWithProcedure(ProductWarehouseDto dto, CancellationToken cancellationToken)
    {
        if (dto.IdProduct < 0 || dto.IdWarehouse < 0 || dto.Amount <= 0)
            return ("LessThanZero", null);

        try
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("ConnectionString"));
            await using (SqlCommand com = new SqlCommand())
            {
                com.Connection = connection;
                com.CommandText = "AddProductToWarehouse";
                com.CommandType = CommandType.StoredProcedure;

                com.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
                com.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
                com.Parameters.AddWithValue("@Amount", dto.Amount);
                com.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);

                await connection.OpenAsync(cancellationToken);
                var result = await com.ExecuteScalarAsync(cancellationToken);

                if (result is decimal newId)
                    return ("Success", newId);
                
                return ("Unknown", null);
            }
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("IdProduct does not exist", StringComparison.OrdinalIgnoreCase))
                return ("IdProduct", null);
            if (e.Message.Contains("IdWarehouse does not exist", StringComparison.OrdinalIgnoreCase))
                return ("IdWarehouse", null);
            if (e.Message.Contains("no order", StringComparison.OrdinalIgnoreCase))
                return ("OrderDoesNotExist", null);
            if (e.Message.Contains("already fulfilled", StringComparison.OrdinalIgnoreCase))
                return ("OrderAlreadyFulfilled", null);
        }  
        catch (Exception)
        {
            return ("Unknown", null);
        }
        
        return ("Unknown", null);
    }
}