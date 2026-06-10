using System.Security.Claims;
using server.application.pet;
using server.domain.pet;

namespace server.endpoints;

internal static class PetEndpoints
{
    internal static WebApplication MapPetEndpoints(this WebApplication app)
    {
        var petGroup = app.MapGroup("/pets").RequireAuthorization();

        petGroup.MapGet("/", async (ClaimsPrincipal user, PetService petService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            var pets = await petService.GetPetsAsync(playerId.Value);
            return Results.Ok(MapPets(pets));
        });

        petGroup.MapPost("/{petId:guid}/train", async (Guid petId, ClaimsPrincipal user, PetService petService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var pet = await petService.TrainAsync(playerId.Value, new PlayerPetId(petId));
                return Results.Ok(MapPet(pet));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        });

        petGroup.MapPost("/{petId:guid}/standby", async (Guid petId, ClaimsPrincipal user, PetService petService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var pets = await petService.StandbyAsync(playerId.Value, new PlayerPetId(petId));
                return Results.Ok(MapPets(pets));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        });

        petGroup.MapPost("/{petId:guid}/clear-standby", async (Guid petId, ClaimsPrincipal user, PetService petService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var pets = await petService.ClearStandbyAsync(playerId.Value, new PlayerPetId(petId));
                return Results.Ok(MapPets(pets));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        });

        petGroup.MapDelete("/{petId:guid}", async (Guid petId, ClaimsPrincipal user, PetService petService) =>
        {
            var playerId = EndpointHelpers.TryGetPlayerId(user);
            if (playerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                await petService.ReleaseAsync(playerId.Value, new PlayerPetId(petId));
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        });

        return app;
    }

    private static object MapPets(IReadOnlyList<PlayerPetView> pets)
    {
        return new
        {
            maxPetCount = PetConstants.MaxPetCount,
            trainingCostGold = PetConstants.TrainingCostGold,
            pets = pets.Select(MapPet).ToArray()
        };
    }

    private static object MapPet(PlayerPetView pet)
    {
        return new
        {
            petId = pet.PetId,
            enemyDefinitionId = pet.EnemyDefinitionId,
            name = pet.Name,
            imagePath = pet.ImagePath,
            level = pet.Level,
            isStandby = pet.IsStandby,
            capturedAt = pet.CapturedAt,
            bonusStatus = new
            {
                maxHp = pet.BonusStatus.MaxHp,
                maxMp = pet.BonusStatus.MaxMp,
                strength = pet.BonusStatus.Strength,
                defense = pet.BonusStatus.Defense,
                intelligence = pet.BonusStatus.Intelligence,
                luck = pet.BonusStatus.Luck,
                speed = pet.BonusStatus.Speed
            },
            totalStatus = new
            {
                maxHp = pet.TotalStatus.MaxHp,
                maxMp = pet.TotalStatus.MaxMp,
                strength = pet.TotalStatus.Strength,
                defense = pet.TotalStatus.Defense,
                intelligence = pet.TotalStatus.Intelligence,
                luck = pet.TotalStatus.Luck,
                speed = pet.TotalStatus.Speed
            }
        };
    }
}
