using Microsoft.AspNetCore.Components.Forms;
using TraysFastUpdate.Models;

namespace TraysFastUpdate.Services.Contracts
{
    public interface ICableTypeService
    {
        Task<CableType> GetCableTypeAsync(int cableTypeId);

        Task<List<CableType>> GetCablesTypesAsync();

        Task CreateCableTypeAsync(CableType cableType);

        Task UpdateCableTypeAsync(CableType cableType);

        Task DeleteCableTypeAsync(int cableTypeId);

        Task UploadFromFileAsync(IBrowserFile file);
    }
}
