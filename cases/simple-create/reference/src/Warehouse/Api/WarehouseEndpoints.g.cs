#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Acme.Warehouse.Contracts;
using Acme.Warehouse.Infrastructure;
namespace Acme.Warehouse.Api;
public static partial class WarehouseEndpoints
{
    public static IEndpointRouteBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        // Object [AG][R]
        MapSupplierEndpoints(app.MapGroup("/suppliers").WithTags("Suppliers"));

        MapProductEndpoints(app.MapGroup("/products").WithTags("Products"));

        return app;
    }
    // a unique index or a foreign key violated by a concurrent change
    private static async Task<bool> TrySaveAsync(WarehouseDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
    // Object [AG][R]
    private static void MapSupplierEndpoints(RouteGroupBuilder group)
    {

        group.MapGet("/", async (WarehouseDbContext db, CancellationToken cancellationToken) =>
        {
            var entities = await db.Suppliers.AsNoTracking().ToListAsync(cancellationToken);
            return TypedResults.Ok(entities.Select(SupplierDto.From).ToList());
        });

        group.MapGet("/{id}", async Task<Results<Ok<SupplierDto>, NotFound>> (Guid id, WarehouseDbContext db, CancellationToken cancellationToken) =>
        {
            var entity = await db.Suppliers.AsNoTracking()
                // Property [M][LNA][AG]
                .Include(x => x.Products)

                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            return entity is null ? TypedResults.NotFound() : TypedResults.Ok(SupplierDto.From(entity));
        });

        group.MapPost("/", async Task<Results<Created<SupplierDto>, ValidationProblem, Conflict>> (SupplierInput input, WarehouseDbContext db, CancellationToken cancellationToken) =>
        {
            var errors = await ValidateSupplierAsync(input, db, cancellationToken);
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }
            var entity = new Domain.Supplier();
            input.ApplyTo(entity);
            db.Suppliers.Add(entity);
            if (!await TrySaveAsync(db, cancellationToken))
            {
                return TypedResults.Conflict();
            }
            return TypedResults.Created($"/suppliers/{entity.Id}", SupplierDto.From(entity));
        });

        group.MapPut("/{id}", async Task<Results<Ok<SupplierDto>, NotFound, ValidationProblem, Conflict>> (Guid id, SupplierInput input, WarehouseDbContext db, CancellationToken cancellationToken) =>
        {
            var entity = await db.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null)
            {
                return TypedResults.NotFound();
            }
            var errors = await ValidateSupplierAsync(input, db, cancellationToken);
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }
            input.ApplyTo(entity);
            if (!await TrySaveAsync(db, cancellationToken))
            {
                return TypedResults.Conflict();
            }
            return TypedResults.Ok(SupplierDto.From(entity));
        });

        group.MapDelete("/{id}", async Task<Results<NoContent, NotFound, Conflict>> (Guid id, WarehouseDbContext db, CancellationToken cancellationToken) =>
        {
            var entity = await db.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null)
            {
                return TypedResults.NotFound();
            }
            db.Suppliers.Remove(entity);
            if (!await TrySaveAsync(db, cancellationToken))
            {
                return TypedResults.Conflict();
            }
            return TypedResults.NoContent();
        });

    }
    private static async Task<Dictionary<string, string[]>> ValidateSupplierAsync(SupplierInput input, WarehouseDbContext db, CancellationToken cancellationToken)
    {
        var errors = input.Validate();

        return errors;
    }

    private static void MapProductEndpoints(RouteGroupBuilder group)
    {

        group.MapGet("/", async (WarehouseDbContext db, CancellationToken cancellationToken) =>
        {
            var entities = await db.Products.AsNoTracking().ToListAsync(cancellationToken);
            return TypedResults.Ok(entities.Select(ProductDto.From).ToList());
        });

        group.MapGet("/{id}", async Task<Results<Ok<ProductDto>, NotFound>> (Guid id, WarehouseDbContext db, CancellationToken cancellationToken) =>
        {
            var entity = await db.Products.AsNoTracking()

                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            return entity is null ? TypedResults.NotFound() : TypedResults.Ok(ProductDto.From(entity));
        });

        group.MapPost("/", async Task<Results<Created<ProductDto>, ValidationProblem, Conflict>> (ProductInput input, WarehouseDbContext db, CancellationToken cancellationToken) =>
        {
            var errors = await ValidateProductAsync(input, db, cancellationToken);
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }
            var entity = new Domain.Product();
            input.ApplyTo(entity);
            db.Products.Add(entity);
            if (!await TrySaveAsync(db, cancellationToken))
            {
                return TypedResults.Conflict();
            }
            return TypedResults.Created($"/products/{entity.Id}", ProductDto.From(entity));
        });

        group.MapPut("/{id}", async Task<Results<Ok<ProductDto>, NotFound, ValidationProblem, Conflict>> (Guid id, ProductInput input, WarehouseDbContext db, CancellationToken cancellationToken) =>
        {
            var entity = await db.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null)
            {
                return TypedResults.NotFound();
            }
            var errors = await ValidateProductAsync(input, db, cancellationToken);
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }
            input.ApplyTo(entity);
            if (!await TrySaveAsync(db, cancellationToken))
            {
                return TypedResults.Conflict();
            }
            return TypedResults.Ok(ProductDto.From(entity));
        });

        group.MapDelete("/{id}", async Task<Results<NoContent, NotFound, Conflict>> (Guid id, WarehouseDbContext db, CancellationToken cancellationToken) =>
        {
            var entity = await db.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null)
            {
                return TypedResults.NotFound();
            }
            db.Products.Remove(entity);
            if (!await TrySaveAsync(db, cancellationToken))
            {
                return TypedResults.Conflict();
            }
            return TypedResults.NoContent();
        });

    }
    private static async Task<Dictionary<string, string[]>> ValidateProductAsync(ProductInput input, WarehouseDbContext db, CancellationToken cancellationToken)
    {
        var errors = input.Validate();
        // Property [M][NO][NAR][AG][PUO][NRO][NN]
        if (!await db.Suppliers.AnyAsync(x => x.Id == input.SupplierId, cancellationToken))
        {
            errors["SupplierId"] = ["Supplier not found"];
        }

        return errors;
    }

}