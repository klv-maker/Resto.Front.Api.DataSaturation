using System;

namespace Resto.Front.Api.DataSaturation.Domain.Interfaces.ViewModels
{
    public interface IClosebaleViewModel : IViewModel
    {
        Action CloseAction { get; set; }
    }
}
