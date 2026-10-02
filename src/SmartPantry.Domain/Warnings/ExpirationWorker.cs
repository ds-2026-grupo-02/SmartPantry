using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace SmartPantry.Warnings;

public class ExpirationWorker : AsyncPeriodicBackgroundWorkerBase
{
    public ExpirationWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        // Frecuencia de ejecución: 1 vez al día (86400000 milisegundos)
        Timer.Period = 86400000;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Logger.LogInformation("Iniciando verificación periódica de vencimientos en despensa...");

        // Resolver el servicio de dominio desde el alcance de la ejecución (Scope)
        var warningManager = workerContext.ServiceProvider.GetRequiredService<ExpirationWarningManager>();

        await warningManager.ProcessExpirationsAsync(DateTime.UtcNow);

        Logger.LogInformation("Procesamiento de vencimientos finalizado exitosamente.");
    }
}