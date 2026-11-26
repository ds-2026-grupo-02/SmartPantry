using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace SmartPantry.Perfil;

[Authorize]
public class PerfilAppService : SmartPantryAppService, IPerfilAppService
{
    private const string FotoPerfilKey = "FotoPerfil";
    private readonly IdentityUserManager _userManager;

    public PerfilAppService(IdentityUserManager userManager)
    {
        _userManager = userManager;
    }

    public async Task<PerfilDto> GetAsync()
    {
        var userId = CurrentUser.GetId();
        var user = await _userManager.GetByIdAsync(userId);

        return new PerfilDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Name = user.Name ?? string.Empty,
            Surname = user.Surname ?? string.Empty,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FotoPerfil = user.GetProperty<string>(FotoPerfilKey)
        };
    }

    public async Task<PerfilDto> ActualizarPerfilAsync(ActualizarPerfilDto input)
    {
        var userId = CurrentUser.GetId();
        var user = await _userManager.GetByIdAsync(userId);

        user.Name = input.Name;
        user.Surname = input.Surname;
        
        if (user.Email != input.Email)
        {
            var setEmailResult = await _userManager.SetEmailAsync(user, input.Email);
            if (!setEmailResult.Succeeded)
            {
                throw new UserFriendlyException("No se pudo actualizar el correo electrónico.");
            }
        }

        if (user.PhoneNumber != input.PhoneNumber)
        {
            var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, input.PhoneNumber);
            if (!setPhoneResult.Succeeded)
            {
                throw new UserFriendlyException("No se pudo actualizar el número de teléfono.");
            }
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new UserFriendlyException("No se pudieron guardar los cambios del perfil.");
        }

        return new PerfilDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Name = user.Name ?? string.Empty,
            Surname = user.Surname ?? string.Empty,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FotoPerfil = user.GetProperty<string>(FotoPerfilKey)
        };
    }

    public async Task<string?> ActualizarFotoAsync(ActualizarFotoDto input)
    {
        if (string.IsNullOrWhiteSpace(input.FotoBase64))
        {
            throw new UserFriendlyException("La foto enviada no es válida.");
        }

        var userId = CurrentUser.GetId();
        var user = await _userManager.GetByIdAsync(userId);

        user.SetProperty(FotoPerfilKey, input.FotoBase64);
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new UserFriendlyException("No se pudo actualizar la foto de perfil.");
        }

        return input.FotoBase64;
    }

    public async Task EliminarFotoAsync()
    {
        var userId = CurrentUser.GetId();
        var user = await _userManager.GetByIdAsync(userId);

        user.RemoveProperty(FotoPerfilKey);
        await _userManager.UpdateAsync(user);
    }
}
