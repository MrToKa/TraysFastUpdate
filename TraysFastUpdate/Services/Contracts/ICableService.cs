﻿using Microsoft.AspNetCore.Components.Forms;
using TraysFastUpdate.Models;

namespace TraysFastUpdate.Services.Contracts
{
    public interface ICableService
    {
        Task<Cable> GetCableAsync(int cableId);

        Task<List<Cable>> GetCablesAsync();

        Task CreateCableAsync(Cable cable);

        Task UpdateCableAsync(Cable cable);

        Task DeleteCableAsync(int cableId);

        Task UploadFromFileAsync(IBrowserFile file);
    }
}